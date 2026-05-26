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
#include <shlwapi.h>     // QISearch / QITAB
#include <shellapi.h>    // ShellExecuteEx
#include <new>
#include <string>
#include <sstream>

#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "user32.lib")

// {e528b90b-bba4-4870-92fe-d8ee781d86c5} — matches the registration tooling (register.cs).
static const GUID CLSID_PkmdsPreviewHandler =
    { 0xe528b90b, 0xbba4, 0x4870, { 0x92, 0xfe, 0xd8, 0xee, 0x78, 0x1d, 0x86, 0xc5 } };

static HINSTANCE g_hInst = nullptr;
static long g_cDllRef = 0;

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
        m_cRef(1), m_hwndParent(nullptr), m_rcParent{}, m_punkSite(nullptr), m_process(nullptr)
    {
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
        m_rcParent = *prc;
        if (wasEmpty && nowSet)
            DoPreview();   // first real rect arrived after an empty SetWindow — render now
        return S_OK;
    }
    IFACEMETHODIMP DoPreview()
    {
        if (!m_hwndParent)
            return S_OK;
        if (!m_rcParent.left && !m_rcParent.top && !m_rcParent.right && !m_rcParent.bottom)
            return S_OK;   // position not known yet; SetRect will trigger the render

        std::wostringstream cmd;
        cmd << L"\"" << m_filePath << L"\" "
            << std::hex << reinterpret_cast<size_t>(m_hwndParent) << std::dec << L" "
            << m_rcParent.left << L" " << m_rcParent.right << L" "
            << m_rcParent.top << L" " << m_rcParent.bottom;
        const std::wstring params = cmd.str();
        const std::wstring app = WorkerExePath();

        SHELLEXECUTEINFOW sei{ sizeof(sei) };
        sei.fMask = SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAG_NO_UI;
        sei.lpFile = app.c_str();
        sei.lpParameters = params.c_str();
        sei.nShow = SW_SHOWDEFAULT;
        if (ShellExecuteExW(&sei))
        {
            // Preview is invoked repeatedly (e.g. minimize/restore); don't leak workers.
            if (m_process)
            {
                TerminateProcess(m_process, 0);
                CloseHandle(m_process);
            }
            m_process = sei.hProcess;
        }
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
};

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
        auto* h = new (std::nothrow) PreviewHandler();
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
    if (!IsEqualCLSID(CLSID_PkmdsPreviewHandler, rclsid))
        return CLASS_E_CLASSNOTAVAILABLE;

    auto* cf = new (std::nothrow) ClassFactory();
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
