/* OperApp Offline Manager */
(function () {
  'use strict';

  const DB_NAME = 'operapp-offline';
  const DB_VER  = 2;
  let _db = null;

  // ── IndexedDB ──────────────────────────────────────────────────────────────
  function openDB() {
    if (_db) return Promise.resolve(_db);
    return new Promise((resolve, reject) => {
      const req = indexedDB.open(DB_NAME, DB_VER);
      req.onupgradeneeded = e => {
        const db = e.target.result;
        if (!db.objectStoreNames.contains('completions'))
          db.createObjectStore('completions', { keyPath: 'id', autoIncrement: true });
        if (!db.objectStoreNames.contains('uploads'))
          db.createObjectStore('uploads', { keyPath: 'id', autoIncrement: true });
      };
      req.onsuccess = e => { _db = e.target.result; resolve(_db); };
      req.onerror = e => reject(e.target.error);
    });
  }

  function idbAdd(store, record) {
    return openDB().then(db => new Promise((resolve, reject) => {
      const tx = db.transaction(store, 'readwrite');
      const req = tx.objectStore(store).add(record);
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    }));
  }

  function idbGetAll(store) {
    return openDB().then(db => new Promise((resolve, reject) => {
      const tx = db.transaction(store, 'readonly');
      const req = tx.objectStore(store).getAll();
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    }));
  }

  function idbDelete(store, id) {
    return openDB().then(db => new Promise((resolve, reject) => {
      const tx = db.transaction(store, 'readwrite');
      const req = tx.objectStore(store).delete(id);
      req.onsuccess = () => resolve();
      req.onerror = () => reject(req.error);
    }));
  }

  // ── CSRF token ────────────────────────────────────────────────────────────
  function getSyncKey() {
    return localStorage.getItem('operapp_synckey') || '';
  }

  // Guardar el token CSRF cuando estamos online (se llama desde las páginas)
  window.OperApp = window.OperApp || {};
  window.OperApp.saveSyncKey = function (token) {
    if (token) localStorage.setItem('operapp_synckey', token);
  };

  // ── Offline indicator ─────────────────────────────────────────────────────
  function showOfflineBanner() {
    let banner = document.getElementById('offline-banner');
    if (!banner) {
      banner = document.createElement('div');
      banner.id = 'offline-banner';
      banner.innerHTML = '<i class="bi bi-wifi-off"></i> Sin conexión — los cambios se guardarán y sincronizarán al reconectar';
      banner.style.cssText = [
        'position:fixed;top:0;left:0;right:0;z-index:9999',
        'background:#0F172A;color:#fff',
        'padding:8px 16px;font-size:13px;font-weight:500',
        'display:flex;align-items:center;gap:8px',
        'animation:slideDown .3s ease'
      ].join(';');
      document.body.prepend(banner);
      document.body.style.paddingTop = (parseInt(document.body.style.paddingTop || 0) + 36) + 'px';
    }
    // Actualizar contador pendientes
    updatePendingCount();
  }

  function hideOfflineBanner() {
    const banner = document.getElementById('offline-banner');
    if (banner) {
      banner.innerHTML = '<i class="bi bi-wifi"></i> Conexión restaurada — sincronizando...';
      banner.style.background = '#16A34A';
      setTimeout(() => {
        banner.remove();
        document.body.style.paddingTop = '';
      }, 3000);
    }
  }

  async function updatePendingCount() {
    try {
      const comps = await idbGetAll('completions');
      const ups = await idbGetAll('uploads');
      const total = comps.length + ups.length;
      const banner = document.getElementById('offline-banner');
      if (banner && total > 0) {
        const badge = banner.querySelector('.pending-count') || (() => {
          const s = document.createElement('span');
          s.className = 'pending-count';
          s.style.cssText = 'background:#EF4444;border-radius:10px;padding:1px 7px;font-size:11px;margin-left:auto;';
          banner.appendChild(s);
          return s;
        })();
        badge.textContent = total + ' pendiente(s)';
      }
    } catch {}
  }

  // ── Sync ──────────────────────────────────────────────────────────────────
  async function syncAll() {
    const syncKey = getSyncKey();

    // Sincronizar completaciones
    const completions = await idbGetAll('completions');
    for (const item of completions) {
      try {
        const res = await fetch('/Sync/CompletarTarea', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-Sync-Key': syncKey
          },
          body: JSON.stringify({ tareaId: item.tareaId, fecha: item.fecha })
        });
        if (res.ok) {
          await idbDelete('completions', item.id);
          // Actualizar UI si la tarea está visible
          markTareaCompletaEnUI(item.tareaId, item.fecha);
        }
      } catch {}
    }

    // Sincronizar archivos
    const uploads = await idbGetAll('uploads');
    for (const item of uploads) {
      try {
        const blob = new Blob([item.data], { type: item.mime });
        const fd = new FormData();
        fd.append('archivos', blob, item.nombre);
        fd.append('syncKey', syncKey);
        const res = await fetch('/Sync/SubirArchivo/' + item.tareaId, {
          method: 'POST',
          body: fd
        });
        if (res.ok) {
          await idbDelete('uploads', item.id);
        }
      } catch {}
    }

    // Registrar Background Sync si disponible
    if ('serviceWorker' in navigator && 'SyncManager' in window) {
      try {
        const reg = await navigator.serviceWorker.ready;
        await reg.sync.register('sync-operapp');
      } catch {}
    }
  }

  function markTareaCompletaEnUI(tareaId, fecha) {
    // Marcar en la lista de tareas si está visible
    const card = document.querySelector(`[data-tarea-id="${tareaId}"]`);
    if (card) {
      card.style.opacity = '0.65';
      const title = card.querySelector('.task-title');
      if (title) title.style.textDecoration = 'line-through';
      const badge = card.querySelector('.badge-ios');
      if (badge) { badge.className = 'badge-ios badge-done'; badge.textContent = '✓ Hoy'; }
    }
  }

  // ── Completar tarea offline ───────────────────────────────────────────────
  window.OperApp.completarOffline = async function (tareaId, formEl) {
    const fecha = new Date().toISOString();
    await idbAdd('completions', { tareaId: parseInt(tareaId), fecha, syncKey: getSyncKey() });
    await updatePendingCount();

    // Feedback visual
    if (formEl) {
      const btn = formEl.querySelector('button[type=submit]');
      if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<i class="bi bi-clock"></i> Pendiente de sync';
        btn.style.background = '#FEF3C7';
        btn.style.color = '#92400E';
      }
    }

    // Guardar en sessionStorage para mostrar "completada" en esta sesión
    const done = JSON.parse(sessionStorage.getItem('tareas_done') || '[]');
    if (!done.includes(tareaId)) { done.push(tareaId); sessionStorage.setItem('tareas_done', JSON.stringify(done)); }
  };

  // ── Subir archivo offline ─────────────────────────────────────────────────
  window.OperApp.subirArchivoOffline = async function (tareaId, file, onQueued) {
    const buffer = await file.arrayBuffer();
    const id = await idbAdd('uploads', {
      tareaId: parseInt(tareaId),
      nombre: file.name,
      mime: file.type,
      data: buffer,
      syncKey: getSyncKey()
    });
    await updatePendingCount();
    if (typeof onQueued === 'function') onQueued(id, file.name);
  };

  // ── Pending count badge (sidebar) ─────────────────────────────────────────
  window.OperApp.getPendingCount = async function () {
    try {
      const c = await idbGetAll('completions');
      const u = await idbGetAll('uploads');
      return c.length + u.length;
    } catch { return 0; }
  };

  // ── Init ──────────────────────────────────────────────────────────────────
  document.addEventListener('DOMContentLoaded', async function () {
    // Registrar service worker
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.register('/sw.js').catch(() => {});
    }

    if (!navigator.onLine) showOfflineBanner();

    window.addEventListener('offline', () => showOfflineBanner());
    window.addEventListener('online', async () => {
      hideOfflineBanner();
      await syncAll();
    });

    // Badge de pendientes en nav
    const count = await window.OperApp.getPendingCount();
    if (count > 0 && !navigator.onLine) showOfflineBanner();

    // Si hay pendientes y estamos online, sincronizar
    if (count > 0 && navigator.onLine) {
      await syncAll();
    }
  });

  // CSS para animación
  const style = document.createElement('style');
  style.textContent = '@keyframes slideDown{from{transform:translateY(-100%)}to{transform:translateY(0)}}';
  document.head.appendChild(style);

})();
