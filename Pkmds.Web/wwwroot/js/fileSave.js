window.showSaveFilePickerAndWrite = async function (fileName, byteArray) {
    try {
        const opts = {
            suggestedName: fileName,
            types: [{
                description: 'Save File',
                accept: { 'application/octet-stream': ['.sav'] }
            }]
        };

        const handle = await window.showSaveFilePicker(opts);
        const writable = await handle.createWritable();
        await writable.write(new Blob([new Uint8Array(byteArray)], { type: "application/octet-stream" }));
        await writable.close();
    } catch (ex) {
        console.error(ex);
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
