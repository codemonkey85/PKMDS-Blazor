globalThis.encryptAes = function (key, data, modeString, iv) {
    requireCbcIv(modeString, iv);
    const keyHex = CryptoJS.enc.Hex.parse(key);
    const encrypted = CryptoJS.AES.encrypt(CryptoJS.enc.Hex.parse(data), keyHex, {
        mode: getMode(modeString),
        padding: CryptoJS.pad.NoPadding,
        ...(iv ? { iv: CryptoJS.enc.Hex.parse(iv) } : {})
    });
    return encrypted.ciphertext.toString(CryptoJS.enc.Hex);
};

globalThis.decryptAes = function (key, data, modeString, iv) {
    requireCbcIv(modeString, iv);
    var keyHex = CryptoJS.enc.Hex.parse(key);
    var encryptedHexStr = CryptoJS.enc.Hex.parse(data);
    var encryptedBase64Str = CryptoJS.enc.Base64.stringify(encryptedHexStr);
    var decrypted = CryptoJS.AES.decrypt(encryptedBase64Str, keyHex, {
        mode: getMode(modeString),
        padding: CryptoJS.pad.NoPadding,
        ...(iv ? { iv: CryptoJS.enc.Hex.parse(iv) } : {})
    });
    return decrypted.toString(CryptoJS.enc.Hex);
};

globalThis.md5Hash = function (data) {
    const parsedHexString = CryptoJS.enc.Hex.parse(data);
    var hash = CryptoJS.MD5(parsedHexString);
    return hash.toString(CryptoJS.enc.Hex);
};

function requireCbcIv(mode, iv) {
    if (mode === 'cbc' && (typeof iv !== 'string' || !/^[0-9a-f]{32}$/i.test(iv))) {
        throw new Error('AES-CBC requires a 16-byte initialization vector.');
    }
}

function getMode(modeString) {
    if (modeString === 'ecb') return CryptoJS.mode.ECB;
    if (modeString === 'cbc') return CryptoJS.mode.CBC;
    if (modeString === 'cfb') return CryptoJS.mode.CFB;
    if (modeString === 'ctr') return CryptoJS.mode.CTR;
    if (modeString === 'ofb') return CryptoJS.mode.OFB;

    throw new Error(`AES mode ${modeString} not supported.`);
}
