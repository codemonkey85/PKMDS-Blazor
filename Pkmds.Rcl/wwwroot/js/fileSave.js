// Track the last drag event for external file drag
window.lastDragEvent = null;
window.droppedFiles = null;
window.storedPokemonData = null;
window.storedPokemonFileName = null;

// Capture dragstart events globally
document.addEventListener('dragstart', function(e) {
    window.lastDragEvent = e;
}, true);

// Capture drop events to store files
document.addEventListener('drop', function(e) {
    if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files.length > 0) {
        window.droppedFiles = e.dataTransfer.files;
    }
}, true);

// Store Pokemon data for potential export
window.storePokemonForExport = function(fileName, byteArray) {
    window.storedPokemonFileName = fileName;
    window.storedPokemonData = byteArray;
};

// Download the stored Pokemon
window.downloadStoredPokemon = function() {
    if (!window.storedPokemonData || !window.storedPokemonFileName) {
        return;
    }
    
    const uint8 = window.storedPokemonData instanceof Uint8Array 
        ? window.storedPokemonData 
        : new Uint8Array(window.storedPokemonData);
    
    const blob = new Blob([uint8], {type: 'application/octet-stream'});
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = window.storedPokemonFileName;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => {
        URL.revokeObjectURL(a.href);
        a.remove();
        // Clear stored data
        window.storedPokemonData = null;
        window.storedPokemonFileName = null;
    }, 0);
};

// Function to read a dropped file and return as base64
window.readDroppedFile = async function(index) {
    if (!window.droppedFiles || index >= window.droppedFiles.length) {
        return null;
    }
    
    const file = window.droppedFiles[index];
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = function(e) {
            // Convert ArrayBuffer to base64
            const bytes = new Uint8Array(e.target.result);
            let binary = '';
            for (let i = 0; i < bytes.length; i++) {
                binary += String.fromCharCode(bytes[i]);
            }
            const base64 = btoa(binary);
            resolve(base64);
        };
        reader.onerror = function(e) {
            reject(e);
        };
        reader.readAsArrayBuffer(file);
    });
};

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

        // Chrome Android may have partial / flaky support for File System Access API.
        const supportsFS = !!window.showSaveFilePicker;
        if (!supportsFS || /Android/i.test(navigator.userAgent)) {
            console.warn('[showFilePickerAndWrite] Falling back to anchor download for this platform.');

            const uint8 = byteArray instanceof Uint8Array ? byteArray : new Uint8Array(byteArray);

            // Use a more "specific" looking type instead of generic octet-stream.
            const blob = new Blob([uint8], {type: 'application/x-pokemon-savedata'});

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

        // Build options for File System Access API
        const opts = {
            suggestedName: (ext && fileName.toLowerCase().endsWith(ext.toLowerCase()))
                ? fileName
                : fileName + ext
        };

        // Only add types if we have an extension
        if (ext) {
            opts.types = [{
                description: description || 'File',
                accept: {
                    'application/x-pokemon-savedata': [ext]
                }
            }];
        }

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
