// backup.js — IndexedDB-backed save file backup manager for PKMDS
// Save file bytes are stored as base64 strings to avoid binary limitations on some browsers.

const DB_NAME = "pkmds-backups";
const DB_VERSION = 1;
const STORE = "backups";

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
                store.createIndex("createdAt", "createdAt", {unique: false});
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

// Tolerate either a JSON string (new trim-safe path) or an already-parsed object
// (legacy callers). Service-worker rollouts can briefly serve mismatched JS / DLL
// pairs across cache boundaries, so the JS side defends against either shape.
function parseMeta(value) {
    if (value == null) return {};
    return typeof value === 'string' ? JSON.parse(value) : value;
}

export async function addBackup(bytesBase64, meta, source) {
    // C# passes meta as a JSON string so it can serialize via source-gen and avoid
    // Blazor's reflection-based JsonSerializer on the IJS boundary (trim-safe).
    // An older DLL may still pass an object; parseMeta handles both.
    meta = parseMeta(meta);
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readwrite");
        const store = tx.objectStore(STORE);
        const createdAt = new Date().toISOString();
        const request = store.add({bytesBase64, meta, createdAt, source});
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

// Internal helper — returns the metadata array (records without bytesBase64).
// Shared between the legacy array-returning export and the new JSON-string export.
async function getBackupMetadataInternal() {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readonly");
        const store = tx.objectStore(STORE);
        const results = [];
        const request = store.openCursor();

        request.onsuccess = (event) => {
            const cursor = event.target.result;
            if (!cursor) {
                resolve(results);
                return;
            }

            const {bytesBase64: _bytes, ...rest} = cursor.value;
            results.push(rest);
            cursor.continue();
        };

        request.onerror = (event) => reject(event.target.error);
        tx.onerror = (event) => reject(event.target.error);
        tx.onabort = (event) => reject(event.target.error ?? new Error("Transaction aborted"));
    });
}

// Legacy export — returns the raw metadata array. Kept around so an older cached
// DLL that hasn't been updated yet still works during a service-worker rollout.
// Uses a cursor so full blob data is never loaded into memory at once.
export async function getBackupMetadata() {
    return await getBackupMetadataInternal();
}

// Returns metadata serialized as a JSON string for source-gen deserialization
// on the .NET side (trim-safe; see #894).
export async function getBackupMetadataJson() {
    return JSON.stringify(await getBackupMetadataInternal());
}

// Internal helper — returns the full record (including bytesBase64) by ID, or null.
async function getBackupInternal(id) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readonly");
        const store = tx.objectStore(STORE);
        const request = store.get(id);
        request.onsuccess = (event) => resolve(event.target.result ?? null);
        request.onerror = (event) => reject(event.target.error);
    });
}

// Legacy export — returns the full backup record (or null). Used by restore/export
// flows in older cached DLLs.
export async function getBackup(id) {
    return await getBackupInternal(id);
}

// Returns the full backup record serialized as a JSON string (or null) — see
// getBackupMetadataJson for rationale.
export async function getBackupJson(id) {
    const value = await getBackupInternal(id);
    return value ? JSON.stringify(value) : null;
}

export async function deleteBackup(id) {
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

export async function getCount() {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readonly");
        const store = tx.objectStore(STORE);
        const request = store.count();
        request.onsuccess = (event) => resolve(event.target.result);
        request.onerror = (event) => reject(event.target.error);
    });
}

// Deletes multiple backups by ID in a single readwrite transaction.
// More efficient than repeated single-delete calls from C# interop.
export async function deleteMultiple(ids) {
    if (!ids || ids.length === 0) return;
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readwrite");
        const store = tx.objectStore(STORE);
        for (const id of ids) {
            store.delete(id);
        }
        tx.oncomplete = () => resolve();
        tx.onerror = (event) => reject(event.target.error);
        tx.onabort = (event) => reject(event.target.error ?? new Error("Transaction aborted"));
    });
}

// Returns the IDs of the N oldest backups ordered by createdAt ascending.
// Used by the retention policy to prune excess backups.
export async function getOldestIds(count) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readonly");
        const store = tx.objectStore(STORE);
        const index = store.index("createdAt");
        const ids = [];
        const request = index.openCursor();
        request.onsuccess = (event) => {
            const cursor = event.target.result;
            if (cursor && ids.length < count) {
                ids.push(cursor.value.id);
                cursor.continue();
            } else {
                resolve(ids);
            }
        };
        request.onerror = (event) => reject(event.target.error);
    });
}
