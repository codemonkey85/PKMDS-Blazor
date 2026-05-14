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

export async function addBackup(bytesBase64, metaJson, source) {
    // C# passes meta as a JSON string so it can serialize via source-gen and avoid
    // Blazor's reflection-based JsonSerializer on the IJS boundary (trim-safe).
    const meta = JSON.parse(metaJson);
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

// Returns all backup records WITHOUT bytesBase64 — lightweight for list display.
// Uses a cursor so that full blob data is never loaded into memory at once.
// Returns a JSON string rather than the raw array so the .NET side can deserialize
// via source-gen instead of Blazor's reflection-based JsonSerializer (trim-safe;
// see #894).
export async function getBackupMetadata() {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readonly");
        const store = tx.objectStore(STORE);
        const results = [];
        const request = store.openCursor();

        request.onsuccess = (event) => {
            const cursor = event.target.result;
            if (!cursor) {
                resolve(JSON.stringify(results));
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

// Returns a single full backup record by ID (including bytesBase64) for restore/export.
// Returns a JSON string (or null) — see getBackupMetadata for rationale.
export async function getBackup(id) {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, "readonly");
        const store = tx.objectStore(STORE);
        const request = store.get(id);
        request.onsuccess = (event) => {
            const value = event.target.result;
            resolve(value ? JSON.stringify(value) : null);
        };
        request.onerror = (event) => reject(event.target.error);
    });
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
