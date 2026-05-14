// Embedded host bridge. Sets up window.PKMDS.host with a tiny surface area
// that an external host (WKWebView in iOS, any other JS-capable container)
// uses to drive PKMDS programmatically instead of via the file picker.
//
// In standalone web mode this script just creates the global; nothing calls
// into it. In embedded mode the host calls loadSave / requestExport from its
// own code, and PKMDS posts ready / saveExport messages back via the WebKit
// message handler (or the console fallback in a regular browser tab).

(function () {
    'use strict';

    window.PKMDS = window.PKMDS || {};

    // Outbound: post a message to the host. Uses WKWebView's
    // window.webkit.messageHandlers.pkmds when present, otherwise logs to the
    // console so the bridge stays observable in a regular browser tab.
    //
    // Payload arrives as a JSON string from the C# side so Blazor's IJS marshaler
    // stays out of the trim hazard path (see EmbeddedHostBridge.SaveExportPayload).
    // Tolerate the legacy object form too in case anything else ever calls in.
    function postMessage(kind, payload) {
        let payloadObj;
        if (payload == null) {
            payloadObj = {};
        } else if (typeof payload === 'string') {
            try {
                payloadObj = JSON.parse(payload);
            } catch (e) {
                console.warn('[PKMDS.host] Failed to parse payload JSON:', e);
                payloadObj = {};
            }
        } else {
            payloadObj = payload;
        }
        const message = Object.assign({ kind: kind }, payloadObj);
        try {
            const handler = window.webkit
                && window.webkit.messageHandlers
                && window.webkit.messageHandlers.pkmds;
            if (handler && typeof handler.postMessage === 'function') {
                handler.postMessage(message);
                return;
            }
        } catch (e) {
            console.warn('[PKMDS.host] postMessage handler failed:', e);
        }
        console.log('[PKMDS.host] →', kind, payloadObj);
    }

    window.PKMDS.host = {
        // Inbound: load a save into PKMDS. The host calls this once embedded
        // mode is initialized (after the 'ready' message has been observed).
        // bytesBase64 — base64-encoded raw save file bytes (or Manic EMU ZIP).
        //   IMPORTANT: this is base64-encoded BYTES, not a file path or
        //   filename. Use loadSaveFromUrl below if you want to load from a URL
        //   in browser-based testing.
        // fileName — display filename; used for Manic EMU detection, error
        //   messages, and the eventual export filename. Pass null if unknown.
        loadSave: async function (bytesBase64, fileName) {
            return await DotNet.invokeMethodAsync(
                'Pkmds.Rcl', 'LoadSaveFromHost', bytesBase64, fileName || null);
        },

        // Convenience helper for browser-based testing: fetch a URL, base64-
        // encode the bytes, and forward to loadSave(). Useful for smoke-testing
        // the bridge from DevTools without manually base64-encoding a file.
        // Real WKWebView hosts call loadSave() directly with bytes they already
        // have in Swift Data form.
        loadSaveFromUrl: async function (url, fileName) {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error('[PKMDS.host] Fetch failed for ' + url + ': ' + response.status);
            }
            const buffer = await response.arrayBuffer();
            const bytes = new Uint8Array(buffer);
            // Chunked encoding to avoid call-stack overflow on large saves
            // (Gen 9 saves can exceed 4 MB; String.fromCharCode.apply has a
            // ~64K argument limit on most engines).
            let binary = '';
            const chunkSize = 0x8000;
            for (let i = 0; i < bytes.length; i += chunkSize) {
                binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunkSize));
            }
            const base64 = btoa(binary);
            const inferredName = fileName || url.split('/').pop() || 'save.sav';
            return await window.PKMDS.host.loadSave(base64, inferredName);
        },

        // Inbound: request the current save bytes. PKMDS responds asynchronously
        // by posting a 'saveExport' message with { data: '<base64>', fileName }.
        // Typically called when the host's "Done" button is tapped.
        requestExport: async function () {
            return await DotNet.invokeMethodAsync(
                'Pkmds.Rcl', 'RequestExportFromHost');
        },

        // Internal: outbound message helper, called from the C# side.
        // Exposed via PKMDS.host to keep the postMessage / fallback logic
        // colocated with the rest of the bridge.
        _sendMessage: postMessage,
    };
})();
