window.showFilePickerAndWrite = async function (fileName, byteArray, extension, description) {
    // byteArray is expected to be a JS array of numbers coming from a Blazor byte[]
    try {
        if (!byteArray) throw new Error('byteArray is null/undefined');
        const length = byteArray.length || 0;
        console.log('[showFilePickerAndWrite] Incoming length:', length, 'type:', Object.prototype.toString.call(byteArray));

        if (length === 0) {
            console.warn('[showFilePickerAndWrite] Received empty array. Aborting write.');
            return;
        }

        // Chrome Android may have partial / flaky support for File System Access API.
        const supportsFS = !!window.showSaveFilePicker;
        if (!supportsFS || /Android/i.test(navigator.userAgent)) {
            console.warn('[showFilePickerAndWrite] Falling back to anchor download for this platform.');
            const uint8 = byteArray instanceof Uint8Array ? byteArray : new Uint8Array(byteArray);
            const blob = new Blob([uint8], { type: 'application/octet-stream' });
            const a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = fileName.endsWith(extension) ? fileName : fileName + extension;
            document.body.appendChild(a);
            a.click();
            setTimeout(() => {
                URL.revokeObjectURL(a.href);
                a.remove();
            }, 0);
            return;
        }

        const opts = {
            suggestedName: fileName,
            types: [{
                description: description || 'File',
                // Ensure extension has leading dot
                accept: { 'application/octet-stream': [extension.startsWith('.') ? extension : '.' + extension] }
            }]
        };

        // Must be called during a user gesture on some platforms.
        const handle = await window.showSaveFilePicker(opts);
        const writable = await handle.createWritable();

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

window.encryptAes = function (key, data, modeString) {
    const keyHex = CryptoJS.enc.Hex.parse(key);
    const encrypted = CryptoJS.AES.encrypt(CryptoJS.enc.Hex.parse(data), keyHex, {
        mode: getMode(modeString),
        padding: CryptoJS.pad.NoPadding
    });
    return encrypted.ciphertext.toString(CryptoJS.enc.Hex);
};

window.decryptAes = function (key, data, modeString) {
    var keyHex = CryptoJS.enc.Hex.parse(key);
    var encryptedHexStr = CryptoJS.enc.Hex.parse(data);
    var encryptedBase64Str = CryptoJS.enc.Base64.stringify(encryptedHexStr);
    var decrypted = CryptoJS.AES.decrypt(encryptedBase64Str, keyHex, {
        mode: getMode(modeString),
        padding: CryptoJS.pad.NoPadding
    });
    return decrypted.toString(CryptoJS.enc.Hex);
};

window.md5Hash = function (data) {
    const parsedHexString = CryptoJS.enc.Hex.parse(data);
    var hash = CryptoJS.MD5(parsedHexString);
    return hash.toString(CryptoJS.enc.Hex);
}

function getMode(modeString) {
    if (modeString === 'ecb') return CryptoJS.mode.ECB;
    if (modeString === 'cbc') return CryptoJS.mode.CBC;
    if (modeString === 'cfb') return CryptoJS.mode.CFB;
    if (modeString === 'ctr') return CryptoJS.mode.CTR;
    if (modeString === 'ofb') return CryptoJS.mode.OFB;

    throw new Error(`AES mode ${modeString} not supported.`);
}
