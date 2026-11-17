// Capability checks, not user-agent version checks. See docs/BROWSER_SUPPORT.md.
(function () {
    // (module (func (local exnref) (try_table)))
    // Uses final exception handling; a legacy try/catch probe would miss the
    // .NET 11 Mono requirement. No imports, exports, or application data.
    var exceptions = [0, 97, 115, 109, 1, 0, 0, 0, 1, 4, 1, 96, 0, 0, 3, 2, 1, 0,
        10, 10, 1, 8, 1, 1, 105, 31, 64, 0, 11, 11];
    // (module (func (result v128) (v128.const i32x4 0 0 0 0)))
    var simd = [0, 97, 115, 109, 1, 0, 0, 0, 1, 5, 1, 96, 0, 1, 123, 3, 2, 1, 0,
        10, 22, 1, 20, 0, 253, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 11];

    // null means the checked capabilities are available, not that every app
    // feature is certified on this browser. Never start without build metadata.
    window.pkmdsCheckBrowser = function (requirements) {
        if (!requirements || typeof requirements.exceptionHandling !== 'boolean'
            || typeof requirements.simd !== 'boolean') {
            return 'configuration';
        }
        try {
            if (typeof WebAssembly !== 'object' || typeof WebAssembly.validate !== 'function'
                || typeof WebAssembly.instantiate !== 'function') {
                return 'webassembly';
            }
            new WebAssembly.Instance(new WebAssembly.Module(
                new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0])));
        } catch (e) {
            return 'webassembly';
        }

        function validates(bytes) {
            try {
                return WebAssembly.validate(new Uint8Array(bytes));
            } catch (e) {
                return false;
            }
        }

        if (requirements.exceptionHandling && !validates(exceptions)) {
            return 'exceptions';
        }
        if (requirements.simd && !validates(simd)) {
            return 'simd';
        }
        return null;
    };
})();
