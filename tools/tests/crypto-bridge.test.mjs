import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const cryptoJs = readFileSync(new URL('../../Pkmds.Web/wwwroot/js/lib/crypto-js/crypto-js.min.js', import.meta.url), 'utf8');
const bridge = readFileSync(new URL('../../Pkmds.Rcl/wwwroot/js/crypto.js', import.meta.url), 'utf8');
// No window global: also verifies the production functions work in a worker.
const runtime = vm.createContext({});
vm.runInContext(cryptoJs, runtime);
vm.runInContext(bridge, runtime);
const key = '2b7e151628aed2a6abf7158809cf4f3c';
const iv = '000102030405060708090a0b0c0d0e0f';
const plaintext = '6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e51' +
    '30c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710';
// NIST SP 800-38A F.1/F.2. Independent known ciphertexts catch matching bugs.
for (const [mode, ciphertext] of [
    ['ecb', '3ad77bb40d7a3660a89ecaf32466ef97f5d3d58503b9699de785895a96fdbaaf' +
        '43b1cd7f598ece23881b00e3ed0306887b0c785e27e8ad3f8223207104725dd4'],
    ['cbc', '7649abac8119b246cee98e9b12e9197d5086cb9b507219ee95db113a917678b2' +
        '73bed6b8e3c1743b7116e69e222295163ff1caa1681fac09120eca307586e1a7'],
]) {
    test(`AES ${mode} matches four-block known answers in both directions`, () => {
        assert.equal(runtime.encryptAes(key, plaintext, mode, mode === 'cbc' ? iv : null), ciphertext);
        assert.equal(runtime.decryptAes(key, ciphertext, mode, mode === 'cbc' ? iv : null), plaintext);
    });
}

test('CBC rejects missing, malformed and wrong-length IVs', () => {
    for (const badIv of [undefined, null, '', '00', 'x'.repeat(32)]) {
        assert.throws(() => runtime.encryptAes(key, plaintext, 'cbc', badIv), /initialization vector/);
        assert.throws(() => runtime.decryptAes(key, plaintext, 'cbc', badIv), /initialization vector/);
    }
});

test('changing the CBC IV changes the ciphertext', () => {
    assert.notEqual(runtime.encryptAes(key, plaintext, 'cbc', iv),
        runtime.encryptAes(key, plaintext, 'cbc', '00'.repeat(16)));
});

test('MD5 matches RFC 1321 empty, short and multi-block messages', () => {
    for (const [text, expected] of [
        ['', 'd41d8cd98f00b204e9800998ecf8427e'],
        ['abc', '900150983cd24fb0d6963f7d28e17f72'],
        ['1234567890'.repeat(8), '57edf4a22be3c955ac49da2e2107b67a'],
    ]) assert.equal(runtime.md5Hash(Buffer.from(text).toString('hex')), expected);
});
