const CACHE_VER = 'operapp-v3';
const ASSET_CACHE = CACHE_VER + '-assets';
const PAGE_CACHE  = CACHE_VER + '-pages';

// Activos que se pre-cachean al instalar el SW
const PRECACHE_ASSETS = [
  '/css/mobile.css',
  '/img/LogoOperapp.jpeg',
  '/img/LogoOperapp.png',
  '/offline'
];

// Rutas de páginas móviles que se cachean dinámicamente
const MOBILE_PAGES = [
  '/Ronda/Historial',
  '/Tarea',
  '/Incidencia',
  '/Ronda/Nueva'
];

// ── INSTALL ──────────────────────────────────────────────────────────────────
self.addEventListener('install', e => {
  e.waitUntil(
    caches.open(ASSET_CACHE)
      .then(cache => cache.addAll(PRECACHE_ASSETS).catch(() => {}))
      .then(() => self.skipWaiting())
  );
});

// ── ACTIVATE ─────────────────────────────────────────────────────────────────
self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys().then(keys =>
      Promise.all(
        keys.filter(k => !k.startsWith(CACHE_VER)).map(k => caches.delete(k))
      )
    ).then(() => self.clients.claim())
  );
});

// ── FETCH ────────────────────────────────────────────────────────────────────
self.addEventListener('fetch', e => {
  const req = e.request;
  const url = new URL(req.url);

  // Solo interceptamos GET del mismo origen
  if (req.method !== 'GET' || url.origin !== self.location.origin) return;

  // Activos estáticos → cache-first, actualiza en background
  if (isStaticAsset(url.pathname)) {
    e.respondWith(cacheFirst(req, ASSET_CACHE));
    return;
  }

  // Páginas → network-first, fallback a caché, luego /offline
  e.respondWith(networkFirstPage(req));
});

function isStaticAsset(pathname) {
  return pathname.startsWith('/css/')
    || pathname.startsWith('/js/')
    || pathname.startsWith('/lib/')
    || pathname.startsWith('/img/')
    || pathname.startsWith('/uploads/')
    || pathname.endsWith('.ico')
    || pathname.endsWith('.woff2')
    || pathname.endsWith('.woff');
}

async function cacheFirst(req, cacheName) {
  const cached = await caches.match(req);
  if (cached) {
    // Actualiza en segundo plano
    fetch(req).then(r => {
      if (r.ok) caches.open(cacheName).then(c => c.put(req, r));
    }).catch(() => {});
    return cached;
  }
  try {
    const response = await fetch(req);
    if (response.ok) {
      const cache = await caches.open(cacheName);
      cache.put(req, response.clone());
    }
    return response;
  } catch {
    return new Response('', { status: 503 });
  }
}

async function networkFirstPage(req) {
  try {
    const response = await fetch(req);
    if (response.ok || response.redirected) {
      const cache = await caches.open(PAGE_CACHE);
      cache.put(req, response.clone());
    }
    return response;
  } catch {
    const cached = await caches.match(req);
    if (cached) return cached;
    // Fallback: offline page o respuesta mínima
    const offline = await caches.match('/offline');
    return offline || new Response(
      '<html><body style="font-family:sans-serif;text-align:center;padding:40px"><h2>Sin conexión</h2><p>Los datos guardados se mostrarán al reconectar.</p></body></html>',
      { headers: { 'Content-Type': 'text/html' } }
    );
  }
}

// ── BACKGROUND SYNC ──────────────────────────────────────────────────────────
self.addEventListener('sync', e => {
  if (e.tag === 'sync-operapp') {
    e.waitUntil(processSyncQueue());
  }
});

async function processSyncQueue() {
  const db = await openIDB();

  // 1. Sincronizar completaciones de tareas
  const completions = await getAllRecords(db, 'completions');
  for (const item of completions) {
    try {
      const res = await fetch('/Sync/CompletarTarea', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Sync-Key': item.syncKey },
        body: JSON.stringify({ tareaId: item.tareaId, fechaCompletada: item.fecha })
      });
      if (res.ok) await deleteRecord(db, 'completions', item.id);
    } catch {}
  }

  // 2. Sincronizar archivos pendientes
  const uploads = await getAllRecords(db, 'uploads');
  for (const item of uploads) {
    try {
      const blob = new Blob([item.data], { type: item.mime });
      const fd = new FormData();
      fd.append('archivos', blob, item.nombre);
      fd.append('syncKey', item.syncKey);
      const res = await fetch(`/Sync/SubirArchivo/${item.tareaId}`, {
        method: 'POST',
        body: fd
      });
      if (res.ok) await deleteRecord(db, 'uploads', item.id);
    } catch {}
  }
}

// ── IndexedDB helpers ────────────────────────────────────────────────────────
function openIDB() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open('operapp-offline', 2);
    req.onupgradeneeded = e => {
      const db = e.target.result;
      if (!db.objectStoreNames.contains('completions'))
        db.createObjectStore('completions', { keyPath: 'id', autoIncrement: true });
      if (!db.objectStoreNames.contains('uploads'))
        db.createObjectStore('uploads', { keyPath: 'id', autoIncrement: true });
    };
    req.onsuccess = e => resolve(e.target.result);
    req.onerror = e => reject(e.target.error);
  });
}

function getAllRecords(db, store) {
  return new Promise((resolve, reject) => {
    const tx = db.transaction(store, 'readonly');
    const req = tx.objectStore(store).getAll();
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

function deleteRecord(db, store, id) {
  return new Promise((resolve, reject) => {
    const tx = db.transaction(store, 'readwrite');
    const req = tx.objectStore(store).delete(id);
    req.onsuccess = () => resolve();
    req.onerror = () => reject(req.error);
  });
}
