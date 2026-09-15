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

function pkmdsIsIOS() {
    return /iPad|iPhone|iPod/.test(navigator.userAgent) ||
        (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
}

function pkmdsNeedsUserTapDownload() {
    // Mobile Chromium can expose the download attribute while still suppressing a
    // programmatic anchor click after Blazor's async export pipeline has consumed the
    // original user gesture. A real tap also keeps the established iOS workaround intact.
    return pkmdsIsIOS() || /Android/i.test(navigator.userAgent);
}

function pkmdsIsAbortError(error) {
    if (!error) return false;
    const message = error.message || String(error);
    return error.name === 'AbortError' || /AbortError|aborted a request/i.test(message);
}

// Monotonic counter so each download dialog gets unique element ids for its
// aria-labelledby / aria-describedby wiring (avoids id collisions if one is ever
// shown before a previous one is torn down).
let pkmdsDialogSeq = 0;

// Some mobile browsers only start a download when the anchor click happens inside a live
// user gesture. Our export pipeline reaches the download after several awaits (unsaved-changes
// dialog, IsSupportedAsync interop, the download interop call), by which point transient
// activation can be gone — so a script-triggered a.click() is silently dropped and
// "Export Save File" appears to do nothing (issues #1044-#1060, #1297).
//
// Fix: instead of clicking for the user, present a real control they tap themselves.
// The tap IS the gesture WebKit requires, so the download/share is always honored,
// regardless of how much async ran before this point. Returns a Promise that resolves
// only after the user starts a download/share and rejects with AbortError when dismissed.
// Used on iOS and Android, and as a last-resort fallback when a reported File System
// Access implementation fails.
function pkmdsPresentDownload(fileName, blob) {
    return new Promise((resolve, reject) => {
        const url = URL.createObjectURL(blob);
        const previouslyFocused = document.activeElement;
        const uid = 'pkmds-dl-' + (++pkmdsDialogSeq);
        let settled = false;
        const finish = (completed) => {
            if (settled) return;
            settled = true;
            document.removeEventListener('keydown', onKey);
            root.remove();
            // Restore focus to wherever the user was before the dialog opened.
            try { if (previouslyFocused && previouslyFocused.focus) previouslyFocused.focus(); } catch (e) { /* ignore */ }
            // Keep the object URL alive long enough for the download/share to read it.
            setTimeout(() => { try { URL.revokeObjectURL(url); } catch (e) { /* ignore */ } }, 60000);
            if (completed) {
                resolve();
            } else {
                reject(new DOMException('The user aborted a request.', 'AbortError'));
            }
        };
        // Escape closes the dialog; Tab is trapped so keyboard focus can't leave the modal.
        const onKey = (e) => {
            if (e.key === 'Escape') { finish(false); return; }
            if (e.key !== 'Tab') return;
            const focusables = Array.prototype.slice.call(root.querySelectorAll('a[href],button'));
            if (focusables.length === 0) return;
            const first = focusables[0];
            const last = focusables[focusables.length - 1];
            const active = document.activeElement;
            if (e.shiftKey && (active === first || !root.contains(active))) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && (active === last || !root.contains(active))) {
                e.preventDefault();
                first.focus();
            }
        };

        const root = document.createElement('div');
        root.setAttribute('role', 'dialog');
        root.setAttribute('aria-modal', 'true');
        root.setAttribute('aria-labelledby', uid + '-title');
        root.setAttribute('aria-describedby', uid + '-sub');
        root.style.cssText = 'position:fixed;inset:0;z-index:200000;display:flex;align-items:center;justify-content:center;padding:24px;box-sizing:border-box;background:rgba(0,0,0,.62);-webkit-backdrop-filter:blur(3px);backdrop-filter:blur(3px);font-family:system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;';

        const card = document.createElement('div');
        card.style.cssText = 'background:#1e1e28;color:#fff;border-radius:16px;padding:24px 22px;width:min(420px,92vw);box-sizing:border-box;box-shadow:0 12px 40px rgba(0,0,0,.5);display:flex;flex-direction:column;gap:12px;text-align:center;';

        const title = document.createElement('div');
        title.id = uid + '-title';
        title.textContent = 'Your save file is ready';
        title.style.cssText = 'font-size:1.15rem;font-weight:700;';

        const sub = document.createElement('div');
        sub.id = uid + '-sub';
        sub.textContent = 'Tap below to save it to your device.';
        sub.style.cssText = 'font-size:.92rem;opacity:.8;margin-bottom:4px;';

        const btnStyle = 'display:block;width:100%;box-sizing:border-box;padding:14px 18px;border-radius:9999px;font-weight:600;font-size:1rem;text-decoration:none;border:0;cursor:pointer;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;';

        card.appendChild(title);
        card.appendChild(sub);

        // Primary: native share sheet ("Save to Files") when the platform can share the file.
        // Guard both the File ctor and canShare: navigator.canShare({ files }) can throw on some
        // WebKit builds (Web Share present but file-sharing unsupported / payload rejected). An
        // unguarded throw here would reject pkmdsPresentDownload before the overlay is appended,
        // aborting export on iOS with no download link at all — the very platform this fixes. On
        // any failure we simply skip the share button and fall through to the direct download link.
        let file = null;
        try { file = new File([blob], fileName, { type: blob.type || 'application/octet-stream' }); } catch (e) { /* File ctor unsupported */ }
        let canShareFile = false;
        try { canShareFile = !!(file && navigator.canShare && navigator.canShare({ files: [file] })); } catch (e) { canShareFile = false; }
        if (canShareFile) {
            const shareBtn = document.createElement('button');
            shareBtn.type = 'button';
            shareBtn.textContent = 'Save to Files…';
            shareBtn.style.cssText = btnStyle + 'background:#7c4dff;color:#fff;margin-bottom:2px;';
            shareBtn.addEventListener('click', async () => {
                try {
                    // Share ONLY the file — no title/text. On iOS, when a share includes both a
                    // file and a title/text and the user picks "Save to Files", the title is
                    // written out as a separate .txt alongside the save (e.g. a 12-byte "text"
                    // file containing "TR ADDED.dsv"). Files-only avoids that stray download.
                    await navigator.share({ files: [file] });
                    finish(true);
                } catch (e) {
                    // User cancelled the share sheet, or share failed — leave the dialog open
                    // so they can still use the direct download link below.
                }
            });
            card.appendChild(shareBtn);
        }

        // Always offer a direct download link the user taps (real anchor = real gesture).
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.textContent = 'Download ' + fileName;
        link.style.cssText = btnStyle + 'background:#fff;color:#111;';
        link.addEventListener('click', () => { setTimeout(() => finish(true), 400); });
        card.appendChild(link);

        const cancel = document.createElement('button');
        cancel.type = 'button';
        cancel.textContent = 'Cancel';
        cancel.style.cssText = btnStyle + 'background:transparent;color:#fff;border:1px solid rgba(255,255,255,.35);font-weight:500;margin-top:2px;';
        cancel.addEventListener('click', () => finish(false));
        card.appendChild(cancel);

        root.appendChild(card);
        root.addEventListener('click', (e) => { if (e.target === root) finish(false); });
        document.addEventListener('keydown', onKey);
        document.body.appendChild(root);

        // Move focus into the dialog so keyboard / assistive-tech users aren't stranded
        // behind the modal. Prefer the primary action (share button, else the download link).
        const firstFocusable = root.querySelector('button,a[href]');
        if (firstFocusable) {
            try { firstFocusable.focus(); } catch (e) { /* ignore */ }
        }
    });
}

window.pkmdsSupportsSaveFilePicker = function () {
    // Export only needs showSaveFilePicker. The package-level support check also requires
    // open and directory pickers, which can incorrectly reject browsers that can save.
    // iOS is kept on the user-tap flow because createWritable support is incomplete.
    return typeof window.showSaveFilePicker === 'function' && !pkmdsIsIOS();
};

function pkmdsDownloadPreparedBlob(fileName, blob, forceUserTap) {
    if (forceUserTap || pkmdsNeedsUserTapDownload()) {
        return pkmdsPresentDownload(fileName, blob);
    }

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
}

window.showFilePickerAndWrite = async function (fileName, byteArray, extension, description, mimeType) {
    // byteArray is expected to be a JS array of numbers coming from a Blazor byte[]
    let fallbackFileName = fileName || 'download';
    let fallbackBlob = null;
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
        const uint8 = byteArray instanceof Uint8Array ? byteArray : new Uint8Array(byteArray);
        fallbackBlob = new Blob([uint8], {type: blobType});

        const hasExt = ext && fileName.toLowerCase().endsWith(ext.toLowerCase());
        fallbackFileName = (ext && !hasExt) ? fileName + ext : fileName;

        // Feature-detect the picker instead of excluding Android by user agent. Current
        // Chromium supports this API on Android and ChromeOS; if an implementation still
        // fails, the catch block below presents a user-initiated download instead.
        // iOS (all browsers) uses WebKit, which may expose showSaveFilePicker but has
        // incomplete support for createWritable(), so it always uses the tap flow.
        const supportsFS = window.pkmdsSupportsSaveFilePicker();
        if (!supportsFS) {
            console.warn('[showFilePickerAndWrite] File picker unavailable; using download fallback.');
            await pkmdsDownloadPreparedBlob(fallbackFileName, fallbackBlob);
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

        // Prefer direct BufferSource write (avoid Blob in some mobile implementations).
        await writable.write(uint8);

        // Optionally flush (object form)
        // await writable.write({ type: 'write', data: uint8 });

        await writable.close();
        console.log('[showFilePickerAndWrite] Write complete. Bytes written:', uint8.length);
    } catch (ex) {
        console.error('[showFilePickerAndWrite] Error:', ex);
        if (pkmdsIsAbortError(ex)) {
            throw ex;
        }

        if (!fallbackBlob) {
            throw ex;
        }

        try {
            // The picker can be present but unusable (or lose transient activation) on some
            // mobile/ChromeOS configurations. A visible download control supplies a fresh user
            // gesture and gives the user a second, independent way to save the generated bytes.
            console.warn('[showFilePickerAndWrite] File picker failed; presenting download fallback.');
            await pkmdsDownloadPreparedBlob(fallbackFileName, fallbackBlob, true);
        } catch (fallbackError) {
            if (pkmdsIsAbortError(fallbackError)) {
                throw fallbackError;
            }

            console.error('[showFilePickerAndWrite] Download fallback failed:', fallbackError);
            const pickerMessage = ex && ex.message ? ex.message : String(ex);
            const fallbackMessage = fallbackError && fallbackError.message
                ? fallbackError.message
                : String(fallbackError);
            throw new Error('File picker failed (' + pickerMessage + '); download fallback failed (' + fallbackMessage + ').');
        }
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
    // Only treat a trailing ".xyz" as an extension. Guard against lastIndexOf === -1, where
    // slice(-1) would return the final character (wrong MIME for extension-less names like
    // "violet_main", which EnsureExtension now preserves).
    const dot = fileName ? fileName.lastIndexOf('.') : -1;
    const inferredExt = dot > 0 ? fileName.slice(dot) : '';
    const blob = new Blob([uint8], { type: mimeType || pkmdsInferMimeType(inferredExt) });
    return pkmdsDownloadPreparedBlob(fileName, blob);
};
