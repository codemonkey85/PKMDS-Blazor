// PkmdsPreviewShim — the native half of the PowerToys-style preview-handler split.
//
// This DLL is the COM IPreviewHandler that Explorer loads into prevhost.exe. Being native, it
// runs fine in the Low-integrity preview sandbox (no CoreCLR to host — which is exactly what a
// .NET comhost cannot do there; see ../README.md "Findings"). On DoPreview it ShellExecuteEx's
// the .NET worker (PkmdsPreviewWorker.exe, sitting next to this DLL), passing the file path,
// the parent HWND, and the bounds on the command line; the worker reparents its WebView2 window
// into that HWND and renders via the shared HtmlRenderer. Unload terminates the worker.
//
// Structure adapted from PowerToys src/modules/previewpane/MarkdownPreviewHandlerCpp (MIT).

#include <windows.h>
#include <shlobj.h>      // IInitializeWithFile, IPreviewHandler, IObjectWithSite, IPreviewHandlerFrame
#include <shlwapi.h>     // QISearch / QITAB, PathFileExists
#include <shellapi.h>    // ShellExecuteEx
#include <thumbcache.h>  // IThumbnailProvider, WTS_ALPHATYPE
#include <wincodec.h>    // WIC — decode the worker's PNG into an HBITMAP
#include <new>
#include <string>
#include <sstream>

#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "gdi32.lib")
#pragma comment(lib, "windowscodecs.lib")

// {e528b90b-bba4-4870-92fe-d8ee781d86c5} — preview handler; matches register.cs.
static const GUID CLSID_PkmdsPreviewHandler =
    { 0xe528b90b, 0xbba4, 0x4870, { 0x92, 0xfe, 0xd8, 0xee, 0x78, 0x1d, 0x86, 0xc5 } };

// {b98dbf6e-6efb-43c0-acb3-cc807131359a} — thumbnail provider; matches register.cs.
static const GUID CLSID_PkmdsThumbnailProvider =
    { 0xb98dbf6e, 0x6efb, 0x43c0, { 0xac, 0xb3, 0xcc, 0x80, 0x71, 0x31, 0x35, 0x9a } };

static HINSTANCE g_hInst = nullptr;
static long g_cDllRef = 0;
static long g_instanceCounter = 0;

// Full path to the worker exe, which is deployed next to this DLL.
static std::wstring WorkerExePath()
{
    wchar_t path[MAX_PATH]{};
    GetModuleFileNameW(g_hInst, path, MAX_PATH);
    std::wstring p(path);
    const auto slash = p.find_last_of(L"\\/");
    if (slash != std::wstring::npos)
        p.resize(slash + 1);
    p += L"PkmdsPreviewWorker.exe";
    return p;
}

class PreviewHandler :
    public IInitializeWithFile,
    public IPreviewHandler,
    public IPreviewHandlerVisuals,
    public IOleWindow,
    public IObjectWithSite
{
public:
    PreviewHandler() :
        m_cRef(1), m_hwndParent(nullptr), m_rcParent{}, m_punkSite(nullptr), m_process(nullptr),
        m_resizeEvent(nullptr)
    {
        // Per-instance auto-reset event the worker waits on to learn the pane was resized.
        // Unique name (PID + counter) so concurrent preview panes don't cross-signal.
        m_eventName = L"PkmdsPreviewResize_" + std::to_wstring(GetCurrentProcessId()) +
                      L"_" + std::to_wstring(InterlockedIncrement(&g_instanceCounter));
        m_resizeEvent = CreateEventW(nullptr, FALSE, FALSE, m_eventName.c_str());
        InterlockedIncrement(&g_cDllRef);
    }

    // IUnknown
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv)
    {
        static const QITAB qit[] = {
            QITABENT(PreviewHandler, IPreviewHandler),
            QITABENT(PreviewHandler, IInitializeWithFile),
            QITABENT(PreviewHandler, IPreviewHandlerVisuals),
            QITABENT(PreviewHandler, IOleWindow),
            QITABENT(PreviewHandler, IObjectWithSite),
            { 0 },
        };
        return QISearch(this, qit, riid, ppv);
    }
    IFACEMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&m_cRef); }
    IFACEMETHODIMP_(ULONG) Release()
    {
        const ULONG r = InterlockedDecrement(&m_cRef);
        if (r == 0)
            delete this;
        return r;
    }

    // IInitializeWithFile
    IFACEMETHODIMP Initialize(LPCWSTR pszFilePath, DWORD) { m_filePath = pszFilePath; return S_OK; }

    // IPreviewHandler
    IFACEMETHODIMP SetWindow(HWND hwnd, const RECT* prc)
    {
        if (hwnd && prc) { m_hwndParent = hwnd; m_rcParent = *prc; }
        return S_OK;
    }
    IFACEMETHODIMP SetRect(const RECT* prc)
    {
        if (!prc)
            return E_INVALIDARG;
        const bool wasEmpty = !m_rcParent.left && !m_rcParent.top && !m_rcParent.right && !m_rcParent.bottom;
        const bool nowSet = prc->left || prc->top || prc->right || prc->bottom;
        const bool changed = !EqualRect(&m_rcParent, prc);
        m_rcParent = *prc;
        if (wasEmpty && nowSet)
            DoPreview();                  // first real rect after an empty SetWindow — render now
        else if (changed && nowSet && m_resizeEvent)
            SetEvent(m_resizeEvent);      // pane resized — tell the worker to re-fit to the parent
        return S_OK;
    }
    IFACEMETHODIMP DoPreview()
    {
        if (!m_hwndParent)
            return S_OK;
        if (!m_rcParent.left && !m_rcParent.top && !m_rcParent.right && !m_rcParent.bottom)
            return S_OK;   // position not known yet; SetRect will trigger the render
        if (m_process)
            return S_OK;   // worker already launched for this preview — Explorer calls DoPreview
                           // again after the SetRect-triggered first render (and on minimize/restore)

        std::wostringstream cmd;
        cmd << L"\"" << m_filePath << L"\" "
            << std::hex << reinterpret_cast<size_t>(m_hwndParent) << std::dec << L" "
            << m_rcParent.left << L" " << m_rcParent.right << L" "
            << m_rcParent.top << L" " << m_rcParent.bottom << L" "
            << m_eventName;   // worker waits on this named event to learn about resizes
        const std::wstring params = cmd.str();
        const std::wstring app = WorkerExePath();

        SHELLEXECUTEINFOW sei{ sizeof(sei) };
        sei.fMask = SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAG_NO_UI;
        sei.lpFile = app.c_str();
        sei.lpParameters = params.c_str();
        sei.nShow = SW_SHOWDEFAULT;
        if (ShellExecuteExW(&sei))
            m_process = sei.hProcess;   // Unload terminates it; the guard above prevents relaunch
        return S_OK;
    }
    IFACEMETHODIMP SetFocus() { return S_FALSE; }
    IFACEMETHODIMP QueryFocus(HWND* phwnd)
    {
        if (!phwnd)
            return E_INVALIDARG;
        *phwnd = ::GetFocus();
        return *phwnd ? S_OK : HRESULT_FROM_WIN32(GetLastError());
    }
    IFACEMETHODIMP TranslateAccelerator(MSG* pmsg)
    {
        // Forward unhandled keys to the host frame so Explorer accelerators keep working.
        IPreviewHandlerFrame* frame = nullptr;
        HRESULT hr = S_FALSE;
        if (m_punkSite && SUCCEEDED(m_punkSite->QueryInterface(IID_PPV_ARGS(&frame))))
        {
            hr = frame->TranslateAccelerator(pmsg);
            frame->Release();
        }
        return hr;
    }
    IFACEMETHODIMP Unload()
    {
        if (m_process)
        {
            TerminateProcess(m_process, 0);
            CloseHandle(m_process);
            m_process = nullptr;
        }
        m_filePath.clear();
        return S_OK;
    }

    // IPreviewHandlerVisuals — no-op; the worker's HTML follows the system theme via CSS color-scheme.
    IFACEMETHODIMP SetBackgroundColor(COLORREF) { return S_OK; }
    IFACEMETHODIMP SetFont(const LOGFONTW*) { return S_OK; }
    IFACEMETHODIMP SetTextColor(COLORREF) { return S_OK; }

    // IOleWindow
    IFACEMETHODIMP GetWindow(HWND* phwnd) { if (!phwnd) return E_INVALIDARG; *phwnd = m_hwndParent; return S_OK; }
    IFACEMETHODIMP ContextSensitiveHelp(BOOL) { return E_NOTIMPL; }

    // IObjectWithSite
    IFACEMETHODIMP SetSite(IUnknown* punkSite)
    {
        if (m_punkSite)
            m_punkSite->Release();
        m_punkSite = punkSite;
        if (m_punkSite)
            m_punkSite->AddRef();
        return S_OK;
    }
    IFACEMETHODIMP GetSite(REFIID riid, void** ppv)
    {
        if (m_punkSite)
            return m_punkSite->QueryInterface(riid, ppv);
        *ppv = nullptr;
        return E_FAIL;
    }

private:
    ~PreviewHandler()
    {
        if (m_process)
            CloseHandle(m_process);
        if (m_resizeEvent)
            CloseHandle(m_resizeEvent);
        if (m_punkSite)
            m_punkSite->Release();
        InterlockedDecrement(&g_cDllRef);
    }

    long m_cRef;
    std::wstring m_filePath;
    HWND m_hwndParent;
    RECT m_rcParent;
    IUnknown* m_punkSite;
    HANDLE m_process;
    HANDLE m_resizeEvent;
    std::wstring m_eventName;
};

// Decode a PNG file into a 32-bpp premultiplied-BGRA top-down HBITMAP (preserves transparency).
static HRESULT LoadPngAsHBitmap(const std::wstring& path, HBITMAP* phbmp)
{
    *phbmp = nullptr;
    IWICImagingFactory* factory = nullptr;
    HRESULT hr = CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&factory));
    if (FAILED(hr))
        return hr;

    IWICBitmapDecoder* decoder = nullptr;
    IWICBitmapFrameDecode* frame = nullptr;
    IWICFormatConverter* converter = nullptr;

    hr = factory->CreateDecoderFromFilename(path.c_str(), nullptr, GENERIC_READ, WICDecodeMetadataCacheOnLoad, &decoder);
    if (SUCCEEDED(hr)) hr = decoder->GetFrame(0, &frame);
    if (SUCCEEDED(hr)) hr = factory->CreateFormatConverter(&converter);
    if (SUCCEEDED(hr)) hr = converter->Initialize(frame, GUID_WICPixelFormat32bppPBGRA, WICBitmapDitherTypeNone, nullptr, 0.0, WICBitmapPaletteTypeCustom);

    UINT w = 0, h = 0;
    if (SUCCEEDED(hr)) hr = converter->GetSize(&w, &h);
    if (SUCCEEDED(hr) && w > 0 && h > 0)
    {
        BITMAPINFO bmi{};
        bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        bmi.bmiHeader.biWidth = static_cast<LONG>(w);
        bmi.bmiHeader.biHeight = -static_cast<LONG>(h);   // top-down
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = BI_RGB;

        void* bits = nullptr;
        HBITMAP hbmp = CreateDIBSection(nullptr, &bmi, DIB_RGB_COLORS, &bits, nullptr, 0);
        if (hbmp && bits)
        {
            hr = converter->CopyPixels(nullptr, w * 4, w * h * 4, static_cast<BYTE*>(bits));
            if (SUCCEEDED(hr))
                *phbmp = hbmp;
            else
                DeleteObject(hbmp);
        }
        else
        {
            hr = E_FAIL;
        }
    }

    if (converter) converter->Release();
    if (frame) frame->Release();
    if (decoder) decoder->Release();
    if (factory) factory->Release();
    return hr;
}

// Thumbnail provider loaded into dllhost.exe. Spawns the worker in --thumbnail mode to draw the
// file's representative bundled sprite to a temp PNG, then decodes it into the returned HBITMAP.
class ThumbnailHandler :
    public IInitializeWithFile,
    public IThumbnailProvider
{
public:
    ThumbnailHandler() : m_cRef(1) { InterlockedIncrement(&g_cDllRef); }

    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv)
    {
        static const QITAB qit[] = {
            QITABENT(ThumbnailHandler, IThumbnailProvider),
            QITABENT(ThumbnailHandler, IInitializeWithFile),
            { 0 },
        };
        return QISearch(this, qit, riid, ppv);
    }
    IFACEMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&m_cRef); }
    IFACEMETHODIMP_(ULONG) Release()
    {
        const ULONG r = InterlockedDecrement(&m_cRef);
        if (r == 0)
            delete this;
        return r;
    }

    IFACEMETHODIMP Initialize(LPCWSTR pszFilePath, DWORD) { m_filePath = pszFilePath; return S_OK; }

    IFACEMETHODIMP GetThumbnail(UINT cx, HBITMAP* phbmp, WTS_ALPHATYPE* pdwAlpha)
    {
        if (!phbmp || !pdwAlpha)
            return E_INVALIDARG;
        *phbmp = nullptr;
        *pdwAlpha = WTSAT_ARGB;
        if (m_filePath.empty() || cx == 0)
            return E_FAIL;

        wchar_t tempDir[MAX_PATH]{};
        GetTempPathW(MAX_PATH, tempDir);
        const std::wstring outPng = std::wstring(tempDir) + L"pkmds_thumb_" +
            std::to_wstring(GetCurrentProcessId()) + L"_" +
            std::to_wstring(InterlockedIncrement(&g_instanceCounter)) + L".png";

        std::wostringstream params;
        params << L"--thumbnail \"" << outPng << L"\" " << cx << L" \"" << m_filePath << L"\"";
        const std::wstring app = WorkerExePath();
        const std::wstring p = params.str();

        SHELLEXECUTEINFOW sei{ sizeof(sei) };
        sei.fMask = SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAG_NO_UI;
        sei.lpFile = app.c_str();
        sei.lpParameters = p.c_str();
        sei.nShow = SW_HIDE;

        HRESULT hr = E_FAIL;
        if (ShellExecuteExW(&sei) && sei.hProcess)
        {
            WaitForSingleObject(sei.hProcess, 15000);
            CloseHandle(sei.hProcess);
            if (PathFileExistsW(outPng.c_str()))
            {
                hr = LoadPngAsHBitmap(outPng, phbmp);
                DeleteFileW(outPng.c_str());
            }
        }
        return (SUCCEEDED(hr) && *phbmp) ? S_OK : E_FAIL;
    }

private:
    ~ThumbnailHandler() { InterlockedDecrement(&g_cDllRef); }
    long m_cRef;
    std::wstring m_filePath;
};

template <class THandler>
class ClassFactory : public IClassFactory
{
public:
    ClassFactory() : m_cRef(1) { InterlockedIncrement(&g_cDllRef); }

    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv)
    {
        static const QITAB qit[] = { QITABENT(ClassFactory, IClassFactory), { 0 } };
        return QISearch(this, qit, riid, ppv);
    }
    IFACEMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&m_cRef); }
    IFACEMETHODIMP_(ULONG) Release()
    {
        const ULONG r = InterlockedDecrement(&m_cRef);
        if (r == 0)
            delete this;
        return r;
    }

    IFACEMETHODIMP CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv)
    {
        if (pUnkOuter)
            return CLASS_E_NOAGGREGATION;
        auto* h = new (std::nothrow) THandler();
        if (!h)
            return E_OUTOFMEMORY;
        const HRESULT hr = h->QueryInterface(riid, ppv);
        h->Release();
        return hr;
    }
    IFACEMETHODIMP LockServer(BOOL fLock)
    {
        if (fLock)
            InterlockedIncrement(&g_cDllRef);
        else
            InterlockedDecrement(&g_cDllRef);
        return S_OK;
    }

private:
    ~ClassFactory() { InterlockedDecrement(&g_cDllRef); }
    long m_cRef;
};

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        g_hInst = hModule;
        DisableThreadLibraryCalls(hModule);
    }
    return TRUE;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void** ppv)
{
    IClassFactory* cf = nullptr;
    if (IsEqualCLSID(CLSID_PkmdsPreviewHandler, rclsid))
        cf = new (std::nothrow) ClassFactory<PreviewHandler>();
    else if (IsEqualCLSID(CLSID_PkmdsThumbnailProvider, rclsid))
        cf = new (std::nothrow) ClassFactory<ThumbnailHandler>();
    else
        return CLASS_E_CLASSNOTAVAILABLE;

    if (!cf)
        return E_OUTOFMEMORY;
    const HRESULT hr = cf->QueryInterface(riid, ppv);
    cf->Release();
    return hr;
}

STDAPI DllCanUnloadNow()
{
    return g_cDllRef > 0 ? S_FALSE : S_OK;
}
