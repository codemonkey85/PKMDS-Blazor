// bank.js — IndexedDB-backed Pokemon Bank for PKMDS
// PKM bytes are stored as base64 strings to avoid binary limitations on some browsers.

const DB_NAME = "pkmds-bank";
const DB_VERSION = 1;
const STORE = "pokemon";

// Shared connection promise — reused across operations to avoid opening a new
// connection for every CRUD call and to ensure versionchange events can close
// the connection cleanly so future schema upgrades are not blocked.
let _dbPromise = null;

function openDb() {
    if (_dbPromise) return _dbPromise;

    _dbPromise = new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            const oldVersion = event.oldVersion;

            // v1: initial schema
            if (oldVersion < 1) {
                const store = db.createObjectStore(STORE, {keyPath: "id", autoIncrement: true});
                store.createIndex("species", "meta.species", {unique: false});
                store.createIndex("isShiny", "meta.isShiny", {unique: false});
                store.createIndex("tag", "meta.tag", {unique: false});
            }

            // Future versions: add migration steps here, e.g.:
            // if (oldVersion < 2) { ... }
        };

        request.onsuccess = (event) => {
            const db = event.target.result;

            // Reset when the DB is closed externally (e.g., browser clears storage).
            db.onclose = () => {
                _dbPromise = null;
            };

            // Allow other tabs to upgrade the schema without being blocked by
            // this open connection.
            db.onversionchange = () => {
                db.close();
                _dbPromise = null;
            };

            resolve(db);
        };

        request.onerror = (event) => {
            _dbPromise = null;
            reject(event.target.error);
        };
    });

    return _dbPromise;
}

export async function addPokemon(bytesBase64, metaJson) {
    // C# passes meta as a JSON string so it can serialize via source-gen and avoid
    // Blazor's reflection-based JsonSerializer on the IJS boundary (trim-safe).
    const meta = JSON.parse(metaJson);
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readwrite");
        const store = tx.objectStore(STORE);
        const addedAt = new Date().toISOString();
        const request = store.add({bytesBase64, meta, addedAt});
        let newId;
        // Capture the auto-assigned ID from the request, but resolve only after
        // the transaction commits so callers observe durable state.
        request.onsuccess = (event) => {
            newId = event.target.result;
        };
        tx.oncomplete = () => resolve(newId);
        tx.onerror = (event) => reject(event.target.error);
        tx.onabort = (event) => reject(event.target.error ?? new Error("Transaction aborted"));
    });
}

// Bulk-insert all entries in a single readwrite transaction — much faster than
// calling addPokemon() in a loop when importing an entire save file.
export async function addRange(entriesJson) {
    // See addPokemon() — C# pre-serializes the payload for trim-safe interop.
    const entries = JSON.parse(entriesJson);
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readwrite");
        const store = tx.objectStore(STORE);
        const addedAt = new Date().toISOString();
        for (const {bytesBase64, meta} of entries) {
            store.add({bytesBase64, meta, addedAt});
        }
        tx.oncomplete = () => resolve(entries.length);
        tx.onerror = (event) => reject(event.target.error);
        tx.onabort = (event) => reject(event.target.error ?? new Error("Transaction aborted"));
    });
}

// Internal helper — returns the raw entries array. Used by both the exported
// getAllPokemon (which stringifies for IJS) and exportAll (which builds a binary
// file payload directly from the array).
async function getAllEntries() {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readonly");
        const store = tx.objectStore(STORE);
        const request = store.getAll();
        request.onsuccess = (event) => resolve(event.target.result);
        request.onerror = (event) => reject(event.target.error);
    });
}

// Exported for C# — returns a JSON string rather than the raw array so the .NET
// side can deserialize via source-gen instead of Blazor's reflection-based
// JsonSerializer (which breaks under TrimMode=full — see #896).
export async function getAllPokemon() {
    const entries = await getAllEntries();
    return JSON.stringify(entries);
}

export async function deletePokemon(id) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readwrite");
        const store = tx.objectStore(STORE);
        store.delete(id);
        tx.oncomplete = () => resolve();
        tx.onerror = (event) => reject(event.target.error);
        tx.onabort = (event) => reject(event.target.error ?? new Error("Transaction aborted"));
    });
}

export async function clearAll() {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readwrite");
        const store = tx.objectStore(STORE);
        store.clear();
        tx.oncomplete = () => resolve();
        tx.onerror = (event) => reject(event.target.error);
        tx.onabort = (event) => reject(event.target.error ?? new Error("Transaction aborted"));
    });
}

export async function exportAll() {
    const entries = await getAllEntries();
    const json = JSON.stringify(entries);
    const encoder = new TextEncoder();
    // Return Uint8Array directly — .NET marshals this to byte[] without an extra
    // Array.from() copy, which halves the memory needed for large exports.
    return encoder.encode(json);
}

function isValidEntry(entry) {
    return entry !== null &&
        typeof entry === "object" &&
        !Array.isArray(entry) &&
        typeof entry.bytesBase64 === "string" && entry.bytesBase64.trim() !== "" &&
        entry.meta !== null && typeof entry.meta === "object" &&
        typeof entry.meta.ext === "string" && entry.meta.ext.trim() !== "";
}

export async function importAll(jsonBytes) {
    const decoder = new TextDecoder();
    // Avoid an unnecessary copy when the interop payload is already a Uint8Array.
    const bytes = jsonBytes instanceof Uint8Array ? jsonBytes : new Uint8Array(jsonBytes);

    let entries;
    try {
        entries = JSON.parse(decoder.decode(bytes));
    } catch {
        throw new Error("Invalid bank import file: malformed JSON.");
    }

    if (!Array.isArray(entries)) {
        throw new Error("Invalid bank import file: expected an array of entries.");
    }

    // Filter out entries that are missing required fields (e.g. from a future or
    // corrupt export) so they don't create records that can't be rehydrated later.
    const valid = entries.filter(isValidEntry);

    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readwrite");
        const store = tx.objectStore(STORE);

        for (const entry of valid) {
            // Strip the id so IndexedDB auto-assigns a new one (avoids conflicts).
            // Normalize addedAt to an ISO string — a non-string value (e.g. a number
            // from an older or hand-edited export) would survive isValidEntry but
            // then fail RawEntry.AddedAt deserialization in C#, preventing bank load.
            const {id: _id, addedAt: rawAddedAt, ...rest} = entry;
            const addedAt = (typeof rawAddedAt === "string" && rawAddedAt.trim() !== "")
                ? rawAddedAt
                : new Date().toISOString();
            store.add({...rest, addedAt});
        }

        tx.oncomplete = () => resolve(valid.length);
        tx.onerror = (event) => reject(event.target.error);
        tx.onabort = (event) => reject(event.target.error ?? new Error("Transaction aborted"));
    });
}
