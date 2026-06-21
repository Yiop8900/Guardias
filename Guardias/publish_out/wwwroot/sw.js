const CACHE_VER   = 'operapp-v6';
const ASSET_CACHE = CACHE_VER + '-assets';
const PAGE_CACHE  = CACHE_VER + '-pages';
const API_CACHE   = CACHE_VER + '-api';

// Assets estáticos — se precachean al instalar (sin autenticación)
const PRECACHE_ASSETS = [
  '/offline-shell.html',
  '/css/mobile.css',
  '/img/LogoOperapp.jpeg',
  '/img/LogoOperapp.png',
  '/js/offline.js',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/jquery/dist/jquery.min.js'
];

// Páginas móviles que se precachean cuando el usuario está online
const MOBILE_PAGES = [
  '/Tarea',
  '/Tarea/Index',
  '/Ronda/Historial',
  '/Ronda/Nueva',
  '/Incidencia',
  '/Pendientes',
  '/offline'
];

// Endpoints de API que se cachean
const API_ENDPOINTS = [
  '/api/mobile/tareas',
  '/api/mobile/historial',
  '/api/mobile/areas',
  '/api/mobile/ronda-activa'
];

const CDN_ICONS = 'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css';

// ── INSTALL ──────────────────────────────────────────────────────────────────
self.addEventListener('install', e => {
  e.waitUntil((async () => {
    const cache = await caches.open(ASSET_CACHE);
    // Activos locales — críticos
    await cache.addAll(PRECACHE_ASSETS.map(u => new Request(u, { cache: 'reload' }))).catch(() => {});
    // Bootstrap Icons CDN — best-effort
    await fetch(CDN_ICONS).then(r => { if (r.ok) cache.put(CDN_ICONS, r); }).catch(() => {});
    await self.skipWaiting();
  })());
});

// ── ACTIVATE ─────────────────────────────────────────────────────────────────
self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(k => !k.startsWith(CACHE_VER)).map(k => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

// ── FETCH ────────────────────────────────────────────────────────────────────
self.addEventListener('fetch', e => {
  const req = e.request;
  const url = new URL(req.url);

  if (req.method !== 'GET') return;

  // CDN externos → cache first (Bootstrap Icons, SweetAlert2)
  if (url.origin !== self.location.origin) {
    if (req.url.includes('bootstrap-icons') || req.url.includes('sweetalert2')
        || req.url.includes('jsdelivr') || req.url.includes('cdnjs')) {
      e.respondWith(cacheFirst(req, ASSET_CACHE));
    }
    return;
  }

  // Activos estáticos → cache first, actualiza en background
  if (isStaticAsset(url.pathname)) {
    e.respondWith(cacheFirst(req, ASSET_CACHE));
    return;
  }

  // API móvil → stale-while-revalidate (sirve cache, actualiza en bg)
  if (url.pathname.startsWith('/api/mobile/')) {
    e.respondWith(staleWhileRevalidate(req, API_CACHE));
    return;
  }

  // Uploads cacheados
  if (url.pathname.startsWith('/uploads/')) {
    e.respondWith(cacheFirst(req, ASSET_CACHE));
    return;
  }

  // Páginas → network first, cache fallback, luego /offline
  e.respondWith(networkFirstPage(req));
});

function isStaticAsset(p) {
  return p.startsWith('/css/') || p.startsWith('/js/') || p.startsWith('/lib/')
    || p.startsWith('/img/') || p.endsWith('.ico') || p.endsWith('.woff2')
    || p.endsWith('.woff') || p.endsWith('.png') || p.endsWith('.jpeg')
    || p.endsWith('.jpg');
}

async function cacheFirst(req, cacheName) {
  const cached = await caches.match(req);
  if (cached) {
    fetch(req).then(r => {
      if (r.ok) caches.open(cacheName).then(c => c.put(req, r));
    }).catch(() => {});
    return cached;
  }
  try {
    const res = await fetch(req);
    if (res.ok) caches.open(cacheName).then(c => c.put(req, res.clone()));
    return res;
  } catch {
    return new Response('', { status: 503 });
  }
}

async function staleWhileRevalidate(req, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(req);

  const networkPromise = fetch(req).then(res => {
    if (res.ok) cache.put(req, res.clone());
    return res;
  }).catch(() => null);

  return cached || await networkPromise || new Response(
    JSON.stringify({ ok: false, offline: true }),
    { headers: { 'Content-Type': 'application/json' } }
  );
}

async function networkFirstPage(req) {
  try {
    const res = await fetch(req);
    if (res.ok || res.redirected) {
      caches.open(PAGE_CACHE).then(c => c.put(req, res.clone()));
    }
    return res;
  } catch {
    // 1. Buscar en PAGE_CACHE (páginas previamente visitadas)
    const cached = await caches.match(req, { ignoreSearch: true })
      || await caches.match(new URL(req.url).pathname, { ignoreSearch: true });
    if (cached) return cached;

    // 2. Fallback: offline shell (siempre disponible desde install)
    //    Se sirve con el URL original para que el JS del shell lea location.pathname
    const shell = await caches.match('/offline-shell.html');
    if (shell) return shell;

    // 3. Último recurso
    return new Response(
      '<html><body style="font-family:sans-serif;text-align:center;padding:40px">'
      + '<h2>Sin conexión</h2><p>Abre la app online primero.</p>'
      + '<a href="/Tarea">Reintentar</a></body></html>',
      { headers: { 'Content-Type': 'text/html' } }
    );
  }
}

// ── PRECACHE PAGES (mensaje desde cliente) ────────────────────────────────────
self.addEventListener('message', async e => {
  if (e.data?.type === 'PRECACHE_PAGES') {
    const urls = [...MOBILE_PAGES, ...(e.data.extraUrls || [])];
    const pageCache = await caches.open(PAGE_CACHE);
    const apiCache  = await caches.open(API_CACHE);

    for (const url of urls) {
      try {
        const res = await fetch(url, { credentials: 'include' });
        if (res.ok) await pageCache.put(url, res.clone());
      } catch {}
    }
    for (const url of API_ENDPOINTS) {
      try {
        const res = await fetch(url, { credentials: 'include' });
        if (res.ok) await apiCache.put(url, res.clone());
      } catch {}
    }
    e.source?.postMessage({ type: 'PRECACHE_DONE' });
  }
});

// ── BACKGROUND SYNC ───────────────────────────────────────────────────────────
self.addEventListener('sync', e => {
  if (e.tag === 'sync-operapp') e.waitUntil(processSyncQueue());
});

async function processSyncQueue() {
  const db = await openIDB();
  const completions = await getAllRecords(db, 'completions');
  for (const item of completions) {
    try {
      const res = await fetch('/Sync/CompletarTarea', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Sync-Key': item.syncKey },
        body: JSON.stringify({ tareaId: item.tareaId, fecha: item.fecha })
      });
      if (res.ok) await deleteRecord(db, 'completions', item.id);
    } catch {}
  }
  const uploads = await getAllRecords(db, 'uploads');
  for (const item of uploads) {
    try {
      const blob = new Blob([item.data], { type: item.mime });
      const fd = new FormData();
      fd.append('archivos', blob, item.nombre);
      const res = await fetch('/Sync/SubirArchivo/' + item.tareaId, { method: 'POST', body: fd });
      if (res.ok) await deleteRecord(db, 'uploads', item.id);
    } catch {}
  }
  // Sincronizar áreas de ronda
  const rondareas = await getAllRecords(db, 'rondareas');
  for (const item of rondareas) {
    try {
      const fd = new FormData();
      fd.append('rondaId', item.rondaId);
      fd.append('areaRondaId', item.areaRondaId);
      if (item.notas) fd.append('notas', item.notas);
      if (item.incDescripcion) fd.append('incDescripcion', item.incDescripcion);
      fd.append('incSeveridad', item.incSeveridad || 0);
      (item.fotos || []).forEach(function(f) {
        fd.append('fotos', new Blob([f.data], { type: f.mime }), f.nombre);
      });
      const res = await fetch('/Sync/CheckArea', { method: 'POST', body: fd });
      if (res.ok) await deleteRecord(db, 'rondareas', item.id);
    } catch {}
  }

  // Refrescar cache de API tras sync
  const apiCache = await caches.open(API_CACHE);
  for (const url of API_ENDPOINTS) {
    try {
      const res = await fetch(url, { credentials: 'include' });
      if (res.ok) await apiCache.put(url, res);
    } catch {}
  }
}

function openIDB() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open('operapp-offline', 2);
    req.onupgradeneeded = e => {
      const db = e.target.result;
      if (!db.objectStoreNames.contains('completions'))
        db.createObjectStore('completions', { keyPath: 'id', autoIncrement: true });
      if (!db.objectStoreNames.contains('uploads'))
        db.createObjectStore('uploads', { keyPath: 'id', autoIncrement: true });
      if (!db.objectStoreNames.contains('rondareas'))
        db.createObjectStore('rondareas', { keyPath: 'id', autoIncrement: true });
    };
    req.onsuccess = e => resolve(e.target.result);
    req.onerror = e => reject(e.target.error);
  });
}
function getAllRecords(db, store) {
  return new Promise((res, rej) => {
    const r = db.transaction(store, 'readonly').objectStore(store).getAll();
    r.onsuccess = () => res(r.result);
    r.onerror = () => rej(r.error);
  });
}
function deleteRecord(db, store, id) {
  return new Promise((res, rej) => {
    const r = db.transaction(store, 'readwrite').objectStore(store).delete(id);
    r.onsuccess = () => res();
    r.onerror = () => rej(r.error);
  });
}
