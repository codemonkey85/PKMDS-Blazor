// Submit a PokePaste create form in a new tab.
// The /create endpoint on pokepast.es does not set CORS headers, so a direct XHR/fetch
// POST would be blocked by the browser. Submitting a hidden form with target="_blank"
// bypasses CORS entirely because it is a standard browser navigation, not an XHR.
window.submitPokePasteForm = function (paste, title, author, notes) {
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = 'https://pokepast.es/create';
    form.target = '_blank';
    form.rel = 'noopener noreferrer';

    const addField = (name, value) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value ?? '';
        form.appendChild(input);
    };

    addField('paste', paste);
    addField('title', title);
    addField('author', author);
    addField('notes', notes);

    document.body.appendChild(form);
    form.submit();
    document.body.removeChild(form);
};

// Track the last drag event for external file drag
window.lastDragEvent = null;
window.droppedFiles = null;

// Prevent browser from opening files - always preventDefault on dragover and drop
document.addEventListener('dragover', function (e) {
    e.preventDefault();
}, false);

// Use capture phase (true) so this fires before element handlers and is unaffected by
// stopPropagation on slot drop handlers. Storing the FileList here so readDroppedFile
// can access it after the async yield in the Blazor drop handler.
document.addEventListener('drop', function (e) {
    // EXCEPTION: when the drop target is a <input type="file">, let the native file-input
    // drop handling run. Calling preventDefault here would cancel the input's automatic
    // assignment of `e.dataTransfer.files` to its own `files` property and the change
    // event that follows, which is how MudFileUpload (and plain InputFile) receive
    // dropped files. We still rely on this listener for non-file-input drops (slot
    // drag-drop reads window.droppedFiles after it lands).
    if (e.target && e.target.tagName === 'INPUT' && e.target.type === 'file') {
        return;
    }

    // Always prevent default to stop browser from opening files
    e.preventDefault();

    // Store files if dropped
    if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files.length > 0) {
        window.droppedFiles = e.dataTransfer.files;
    }
}, true);

// Capture dragstart events globally
document.addEventListener('dragstart', function (e) {
    window.lastDragEvent = e;
}, true);

// Set drag-out data on the current dragstart event so the PKM file can be dragged to the OS desktop.
// Uses the DownloadURL convention supported by Chrome/Edge (silently ignored by Firefox/Safari).
window.setDragDownloadData = function (filename, base64data) {
    if (!window.lastDragEvent) return false;
    try {
        const dataUrl = 'data:application/octet-stream;base64,' + base64data;
        const downloadUrl = 'application/octet-stream:' + filename + ':' + dataUrl;
        window.lastDragEvent.dataTransfer.setData('DownloadURL', downloadUrl);
        return true;
    } catch (e) {
        console.warn('[setDragDownloadData] Failed:', e);
        return false;
    }
};

// Haptic feedback via the Vibration API. Android Chrome / Samsung / Firefox honour this;
// iOS Safari doesn't implement navigator.vibrate at all, so this is a silent no-op there
// (iOS still gets the system "lift" haptic from native HTML5 drag for free — see #770).
// Caller passes either a duration in ms or an array pattern (e.g. [10, 30, 10]).
window.pkmdsHaptic = function (pattern) {
    if (!navigator || typeof navigator.vibrate !== 'function') return false;
    try {
        return navigator.vibrate(pattern);
    } catch (e) {
        return false;
    }
};

// iOS Safari cancels a drag whose dragstart completes with an empty DataTransfer.
// Set a minimal text/plain payload so the drag survives through to dragover/drop
// on touch devices. Desktop browsers are unaffected — they ignore unused payloads.
window.setSlotDragMarker = function (value) {
    if (!window.lastDragEvent) return false;
    try {
        window.lastDragEvent.dataTransfer.setData('text/plain', value || 'pkmds-slot');
        return true;
    } catch (e) {
        console.warn('[setSlotDragMarker] Failed:', e);
        return false;
    }
};

// Function to read a dropped file and return as base64
window.readDroppedFile = async function (index) {
    if (!window.droppedFiles || index >= window.droppedFiles.length) {
        return null;
    }

    const file = window.droppedFiles[index];
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = function (e) {
            // Convert ArrayBuffer to base64 efficiently
            const bytes = new Uint8Array(e.target.result);
            const binary = Array.from(bytes, byte => String.fromCharCode(byte)).join('');
            const base64 = btoa(binary);
            resolve(base64);
        };
        reader.onerror = function (e) {
            reject(e);
        };
        reader.readAsArrayBuffer(file);
    });
};

// Returns true if the page is running inside a known in-app browser (e.g. Google Search App,
// Facebook, Instagram) whose WebView may block file downloads or the File System Access API.
// Namespaced under `pkmds` so we don't collide with the non-function `window.isInAppBrowser`
// boolean that some in-app browsers / extensions inject into the page (see issue #732).
window.pkmdsIsInAppBrowser = function () {
    const ua = navigator.userAgent || '';
    return (
        /GSA\//.test(ua) ||                    // Google Search App (iOS)
        /FBAN\/|FBAV\//.test(ua) ||            // Facebook
        /Instagram/.test(ua) ||                 // Instagram
        /Twitter\//.test(ua) ||                 // Twitter / X
        /Line\//.test(ua) ||                    // Line messenger
        /LinkedInApp/.test(ua) ||               // LinkedIn
        /Snapchat/.test(ua) ||                  // Snapchat
        /WhatsApp/.test(ua) ||                  // WhatsApp
        /Telegram(Bot)?/.test(ua) ||             // Telegram
        /MicroMessenger/.test(ua) ||            // WeChat
        /Discord\//.test(ua) ||                 // Discord
        /Pinterest\//.test(ua) ||               // Pinterest
        /BytedanceWebview|musical_ly/.test(ua) || // TikTok
        /Reddit\//.test(ua) ||                  // Reddit
        !!window.TelegramWebviewProxy           // Telegram (fallback — UA not always branded)
    );
};

// Map an extension to a sensible Content-Type when the caller hasn't passed one explicitly.
// Covers compound extensions like ".3ds.sav" (leaf ".sav") and non-save outputs (.zip, .json)
// so anchor-download fallbacks on iOS don't end up tagged application/x-pokemon-savedata for
// obviously-not-a-save payloads (e.g. bank exports, bulk PKM archives).
function pkmdsInferMimeType(ext) {
    if (!ext) return 'application/octet-stream';
    const normalized = ext.toLowerCase();
    const leaf = normalized.lastIndexOf('.') > 0 ? normalized.slice(normalized.lastIndexOf('.')) : normalized;
    if (leaf === '.zip') return 'application/zip';
    if (leaf === '.json') return 'application/json';
    if (leaf === '.sav' || leaf === '.dsv' || /^\.(pk|ek|bk)[0-9]$/.test(leaf) || leaf === '.pb7' || leaf === '.pb8') {
        return 'application/x-pokemon-savedata';
    }
    return 'application/octet-stream';
}

window.showFilePickerAndWrite = async function (fileName, byteArray, extension, description, mimeType) {
    // byteArray is expected to be a JS array of numbers coming from a Blazor byte[]
    try {
        if (!byteArray) throw new Error('byteArray is null/undefined');
        const length = byteArray.length || 0;
        console.log('[showFilePickerAndWrite] Incoming length:', length, 'type:', Object.prototype.toString.call(byteArray));

        if (length === 0) {
            console.warn('[showFilePickerAndWrite] Received empty array. Aborting write.');
            return;
        }

        // Normalize the extension - allow empty/null for no extension
        let ext = (extension || '').trim();

        // If extension is just a dot, treat it as no extension
        if (ext === '.') {
            ext = '';
        }

        // Add leading dot if we have an extension that doesn't start with one
        if (ext && !ext.startsWith('.')) {
            ext = '.' + ext;
        }

        // Caller can override the MIME — important for ZIP archives (Manic EMU .3ds.sav)
        // where the default is wrong and iOS Safari is known to rewrite the extension to
        // match the declared type. When no override is passed we infer from the extension:
        // callers that dispatch .zip / .json (bulk exports, bank exports) shouldn't have to
        // thread a MIME just to avoid the generic Pokémon-savedata tag on anchor fallbacks.
        const blobType = mimeType || pkmdsInferMimeType(ext);

        // Chrome Android may have partial / flaky support for File System Access API.
        // iOS (all browsers) uses WebKit, which may expose showSaveFilePicker but has
        // incomplete support for createWritable() — always use the anchor fallback on iOS.
        const supportsFS = !!window.showSaveFilePicker;
        const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) ||
            (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
        if (!supportsFS || /Android/i.test(navigator.userAgent) || isIOS) {
            console.warn('[showFilePickerAndWrite] Falling back to anchor download for this platform.');

            const uint8 = byteArray instanceof Uint8Array ? byteArray : new Uint8Array(byteArray);
            const blob = new Blob([uint8], {type: blobType});

            const hasExt = ext && fileName.toLowerCase().endsWith(ext.toLowerCase());
            const finalName = (ext && !hasExt) ? fileName + ext : fileName;

            const a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = finalName;
            document.body.appendChild(a);
            a.click();
            setTimeout(() => {
                URL.revokeObjectURL(a.href);
                a.remove();
            }, 0);

            return;
        }

        // Chrome's showSaveFilePicker accepts single extensions like ".sav" and compound
        // extensions like ".3ds.sav". Disallow spaces or non-alphanumeric segments — those
        // cause Chrome to blank the filename field — but keep compound suffixes intact so
        // Manic EMU archives don't lose their ".3ds.sav" on export.
        const isValidExtForPicker = !!ext && /^(?:\.[a-zA-Z0-9]+){1,2}$/.test(ext);

        // Build options for File System Access API
        const opts = {
            suggestedName: isValidExtForPicker
                ? ((fileName.toLowerCase().endsWith(ext.toLowerCase())) ? fileName : fileName + ext)
                : (ext && fileName.toLowerCase().endsWith(ext.toLowerCase()) ? fileName.slice(0, -ext.length) : fileName)
        };

        if (isValidExtForPicker) {
            // The `accept` field only takes simple extensions per the File System Access spec,
            // so pass the leaf extension (".sav" from ".3ds.sav") rather than the compound.
            const leafExt = ext.lastIndexOf('.') > 0 ? ext.slice(ext.lastIndexOf('.')) : ext;
            opts.types = [{
                description: description || 'File',
                accept: {
                    [blobType]: [leafExt]
                }
            }];
        }

        // Must be called during a user gesture on some platforms.
        const handle = await window.showSaveFilePicker(opts);
        const writable = await handle.createWritable({ keepExistingData: false });

        const uint8 = byteArray instanceof Uint8Array ? byteArray : new Uint8Array(byteArray);

        // Prefer direct BufferSource write (avoid Blob in some mobile implementations).
        await writable.write(uint8);

        // Optionally flush (object form)
        // await writable.write({ type: 'write', data: uint8 });

        await writable.close();
        console.log('[showFilePickerAndWrite] Write complete. Bytes written:', uint8.length);
    } catch (ex) {
        console.error('[showFilePickerAndWrite] Error:', ex);
        throw ex;
    }
};

// Anchor-based blob download. Used as a fallback when the File System Access API isn't
// available (or the user dismissed it). Avoids the ~33% base64 inflation of a data: URI
// and works around URL-length limits on older engines. The caller should pass an explicit
// mimeType — application/zip for Manic EMU archives, application/x-pokemon-savedata for
// raw saves, application/octet-stream for anything else.
window.downloadBlob = function (fileName, byteArray, mimeType) {
    if (!byteArray) return;
    const uint8 = byteArray instanceof Uint8Array ? byteArray : new Uint8Array(byteArray);
    const inferredExt = fileName ? fileName.slice(fileName.lastIndexOf('.')) : '';
    const blob = new Blob([uint8], { type: mimeType || pkmdsInferMimeType(inferredExt) });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => {
        URL.revokeObjectURL(url);
        a.remove();
    }, 0);
};
