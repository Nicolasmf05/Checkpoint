// Persistencia de biblioteca y preferencias en IndexedDB.
// El número de revisión detecta cambios de otra pestaña antes de sobrescribir la colección.

export class BrowserStore {
  async open() {
    this.db = await new Promise((resolve, reject) => {
      const r = indexedDB.open('checkpoint-web', 1);
      r.onupgradeneeded = () => r.result.createObjectStore('values');
      r.onsuccess = () => resolve(r.result);
      r.onerror = () => reject(r.error);
    });
    return this;
  }
  async get(key) {
    return new Promise((resolve, reject) => {
      const t = this.db.transaction('values'),
        r = t.objectStore('values').get(key);
      r.onsuccess = () => resolve(r.result);
      r.onerror = () => reject(r.error);
    });
  }
  async put(key, value) {
    return new Promise((resolve, reject) => {
      const t = this.db.transaction('values', 'readwrite');
      t.objectStore('values').put(value, key);
      t.oncomplete = resolve;
      t.onerror = () => reject(t.error);
      t.onabort = () => reject(t.error);
    });
  }
  // Comprueba y actualiza la revisión en una sola transacción IndexedDB para detectar otras pestañas.
  async save(value, expected) {
    return new Promise((resolve, reject) => {
      const t = this.db.transaction('values', 'readwrite'),
        s = t.objectStore('values'),
        r = s.get('library');
      let conflict = false;
      r.onsuccess = () => {
        if ((r.result?.revision || 0) !== expected) {
          conflict = true;
          t.abort();
          return;
        }
        s.put({ ...value, revision: expected + 1 }, 'library');
      };
      t.oncomplete = () => resolve(expected + 1);
      t.onabort = () => reject(new Error(conflict ? 'local-conflict' : 'storage'));
      t.onerror = () => reject(new Error('storage'));
    });
  }
}
