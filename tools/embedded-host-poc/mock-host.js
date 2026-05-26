'use strict';

// Determine the PKMDS base URL.
//
// When this page is served from the same origin as the PKMDS dev server
// (the normal case — both at http://localhost:5283), a relative URL works
// perfectly. When opened directly as a local file (file://), default to the
// standard dev-server address. Override either case with ?pkmds=<url>.
var pkmdsParam = new URLSearchParams(location.search).get('pkmds');
var pkmdsOrigin = location.protocol !== 'file:' ? location.origin : 'http://localhost:5283';
if (pkmdsParam) {
    // Validate the override before it reaches the iframe src: only accept an
    // http(s) URL and use just its origin, so a crafted value like
    // ?pkmds=javascript:… can't be injected (CodeQL js/xss,
    // js/client-side-unvalidated-url-redirection).
    try {
        var overrideUrl = new URL(pkmdsParam, location.href);
        if (overrideUrl.protocol === 'http:' || overrideUrl.protocol === 'https:') {
            pkmdsOrigin = overrideUrl.origin;
        }
    } catch (e) {
        // Ignore an unparseable override and keep the default origin.
    }
}
document.getElementById('pkmds-frame').src = pkmdsOrigin + '/?host=poc';

// ── DOM references ─────────────────────────────────────────────────────────

var frame     = document.getElementById('pkmds-frame');
var btnDone   = document.getElementById('btn-done');
var btnCancel = document.getElementById('btn-cancel');
var fileBtn   = document.getElementById('file-btn');
var fileInput = document.getElementById('file-input');
var statusEl  = document.getElementById('status');
var logEl     = document.getElementById('log-entries');
var logClear  = document.getElementById('log-clear');

// ── State ──────────────────────────────────────────────────────────────────

var callIdSeq = 0;
var pendingCalls = {};       // id → { resolve, reject }
var saveLoaded = false;
var saveExportResolver = null; // resolves when saveExport arrives

// ── Utilities ──────────────────────────────────────────────────────────────

function nextId() {
    return 'c' + (++callIdSeq);
}

function timestamp() {
    var d = new Date();
    return d.toTimeString().slice(0, 8) + '.'
        + String(d.getMilliseconds()).padStart(3, '0');
}

function setStatus(text) {
    statusEl.textContent = text;
}

// Append a message to the log panel.
function log(cssKind, label, detail) {
    var empty = logEl.querySelector('.log-empty');
    if (empty) empty.remove();

    var entry = document.createElement('div');
    entry.className = 'log-entry kind-' + cssKind;

    var head = document.createElement('div');
    head.innerHTML =
        '<span class="log-time">' + timestamp() + '</span>'
        + ' <span class="log-kind">' + escapeHtml(label) + '</span>';
    entry.appendChild(head);

    if (detail) {
        var det = document.createElement('div');
        det.className = 'log-detail';
        det.textContent = detail;
        entry.appendChild(det);
    }

    logEl.insertBefore(entry, logEl.firstChild); // newest on top
}

function escapeHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

// ── Bridge: inbound (parent → iframe) ─────────────────────────────────────

// Send a method call to window.PKMDS.host inside the iframe.
// Returns a Promise that resolves with the call's return value.
// The iframe's ?host=poc polyfill (in app.js) handles these messages.
function callIframe(method, args) {
    return new Promise(function (resolve, reject) {
        var id = nextId();
        pendingCalls[id] = { resolve: resolve, reject: reject };

        // Safety net: reject if the iframe never responds
        var timer = setTimeout(function () {
            if (pendingCalls[id]) {
                delete pendingCalls[id];
                reject(new Error('Timeout waiting for response to ' + method));
            }
        }, 30000);

        pendingCalls[id].timer = timer;

        frame.contentWindow.postMessage(
            { type: 'pkmds-host-call', method: method, args: args || [], id: id },
            '*'
        );
    });
}

// ── Bridge: outbound (iframe → parent) ────────────────────────────────────

// Handle a message posted by PKMDS (via the ?host=poc webkit polyfill).
function handleOutbound(msg) {
    switch (msg.kind) {

        case 'ready':
            log('ready', 'ready', null);
            setStatus('PKMDS ready — pick a .sav file to load.');
            fileBtn.disabled = false;
            btnCancel.disabled = false;
            break;

        case 'saveExport':
            log('export', 'saveExport',
                'fileName: ' + msg.fileName + '\n'
                + 'data: ' + msg.data.slice(0, 40) + '… ('
                + msg.data.length + ' base64 chars)');
            if (saveExportResolver) {
                var resolver = saveExportResolver;
                saveExportResolver = null;
                resolver(msg);
            }
            break;

        default:
            log('call', msg.kind || '(unknown)', JSON.stringify(msg));
    }
}

// ── Unified message listener ───────────────────────────────────────────────

window.addEventListener('message', function (event) {
    var data = event.data;
    if (!data) return;

    // Outbound from PKMDS — has a 'kind' field
    if (typeof data.kind === 'string') {
        handleOutbound(data);
        return;
    }

    // Response to an inbound call — has type + id
    if (data.type === 'pkmds-host-result' && data.id) {
        var pending = pendingCalls[data.id];
        if (pending) {
            clearTimeout(pending.timer);
            delete pendingCalls[data.id];
            pending.resolve(data.result);
        }
    }
});

// ── File picker → loadSave ─────────────────────────────────────────────────

fileBtn.addEventListener('click', function () {
    fileInput.click();
});

fileInput.addEventListener('change', function () {
    var file = fileInput.files && fileInput.files[0];
    if (!file) return;
    fileInput.value = '';

    setStatus('Reading ' + file.name + '…');

    var reader = new FileReader();
    reader.onload = function (ev) {
        var buffer = ev.target.result;
        var bytes = new Uint8Array(buffer);

        // Chunked base64 encoding avoids call-stack overflow on large saves
        // (Gen 9 saves can exceed 4 MB; String.fromCharCode.apply has a
        // ~64 K argument limit in most engines).
        var bin = '';
        var chunk = 0x8000;
        for (var i = 0; i < bytes.length; i += chunk) {
            bin += String.fromCharCode.apply(null, bytes.subarray(i, i + chunk));
        }
        var b64 = btoa(bin);

        log('call', 'loadSave →', 'file: ' + file.name + ' (' + bytes.length + ' bytes)');
        setStatus('Loading ' + file.name + ' via bridge…');

        callIframe('loadSave', [b64, file.name])
            .then(function (result) {
                if (result) {
                    saveLoaded = true;
                    btnDone.disabled = false;
                    setStatus('Loaded ' + file.name + '. Edit and tap Done to export.');
                    log('call', '← loadSave', 'result: true');
                } else {
                    setStatus('Load failed — see browser console for details.');
                    log('error', '← loadSave', 'result: false');
                }
            })
            .catch(function (err) {
                setStatus('Load error: ' + err.message);
                log('error', 'loadSave error', String(err));
            });
    };
    reader.onerror = function () {
        setStatus('Could not read file.');
        log('error', 'FileReader error', String(reader.error));
    };
    reader.readAsArrayBuffer(file);
});

// ── Done button → requestExport ────────────────────────────────────────────

btnDone.addEventListener('click', function () {
    if (!saveLoaded) return;
    btnDone.disabled = true;
    setStatus('Requesting export…');
    log('call', 'requestExport →', null);

    // Set up a promise that resolves when the saveExport message arrives.
    // saveExport is posted by PKMDS before requestExport() resolves, but we
    // guard both orderings: the promise resolves whichever arrives second.
    var saveExportPromise = new Promise(function (resolve, reject) {
        saveExportResolver = resolve;
        setTimeout(function () {
            if (saveExportResolver) {
                saveExportResolver = null;
                reject(new Error('Timeout waiting for saveExport'));
            }
        }, 15000);
    });

    callIframe('requestExport', [])
        .then(function (result) {
            log('call', '← requestExport', 'result: ' + result);
            if (!result) {
                saveExportResolver = null;
                setStatus('Export failed — see browser console for details.');
                btnDone.disabled = !saveLoaded;
                return;
            }
            return saveExportPromise;
        })
        .then(function (exportMsg) {
            if (!exportMsg) return; // already handled the !result branch
            triggerDownload(exportMsg);
            setStatus('Export complete — download started.');
            btnDone.disabled = !saveLoaded;
        })
        .catch(function (err) {
            saveExportResolver = null;
            setStatus('Export error: ' + err.message);
            log('error', 'export error', String(err));
            btnDone.disabled = !saveLoaded;
        });
});

// ── Cancel button ──────────────────────────────────────────────────────────

btnCancel.addEventListener('click', function () {
    log('call', 'Cancel', 'Host dismissed without exporting.');
    setStatus('Cancelled. Pick another save to continue.');
    // In a real native app this would dismiss the sheet/modal.
    // In the PoC we reset the host-side state without touching the iframe.
    saveLoaded = false;
    btnDone.disabled = true;
    saveExportResolver = null;
});

// ── Log clear ─────────────────────────────────────────────────────────────

logClear.addEventListener('click', function () {
    logEl.innerHTML = '<div class="log-empty">No messages yet</div>';
});

// ── Download helper ────────────────────────────────────────────────────────

function triggerDownload(exportMsg) {
    var bin = atob(exportMsg.data);
    var bytes = new Uint8Array(bin.length);
    for (var i = 0; i < bin.length; i++) {
        bytes[i] = bin.charCodeAt(i);
    }
    var blob = new Blob([bytes], { type: 'application/octet-stream' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = exportMsg.fileName || 'save.sav';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(function () { URL.revokeObjectURL(url); }, 5000);
}
