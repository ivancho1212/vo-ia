(function () {
  const scriptTag = document.currentScript;
  const botId = scriptTag.getAttribute("data-bot");
  const token = scriptTag.getAttribute("data-token");
  const clientSecret = scriptTag.getAttribute("data-client-secret") || '';
  const allowedDomain = scriptTag.getAttribute('data-allowed-domain') || scriptTag.getAttribute('data-allowedDomain') || '';

  if (!botId) {
    console.error("❌ El atributo data-bot es requerido para el widget.");
    return;
  }

  // If allowedDomain is present, validate host; if absent, refuse to load for safety
  if (!allowedDomain) {
    console.warn('⚠️ Widget not loaded: missing data-allowed-domain attribute. Regenerate the integration snippet including the allowed domain.');
    return;
  }

  try {
    const allowedHost = (new URL(allowedDomain)).host;
    if (window.location.host !== allowedHost) {
      // Not authorized on this host
      // Silent return to avoid breaking host page
      return;
    }
  } catch (e) {
    console.warn('⚠️ Invalid data-allowed-domain value for widget.');
    return;
  }

  // Create iframe that will host the widget
  const iframe = document.createElement("iframe");
  let baseUrl = `http://localhost:3000/widget-frame?bot=${botId}`;
  if (token) {
    baseUrl += `&token=${token}`;
  }
  if (clientSecret) {
    baseUrl += `&secret=${encodeURIComponent(clientSecret)}`;
  }
  iframe.src = baseUrl;

  // Minimal, non-invasive defaults. Let the host page control positioning if desired.
  // Iframe default styles — visible and positioned in the corner by default
  iframe.style.cssText = `
    display: block;
    width: 420px; /* initial fallback larger so child can measure its preferred layout */
    height: 720px; /* initial fallback larger so child can measure its preferred layout */
    border: none;
    background: transparent;
    z-index: 9999;
    pointer-events: auto; /* allow interactions by default */
    /* Default position: make widget visible by default without forcing full-screen */
    position: fixed;
    bottom: 20px;
    right: 20px;
  `;
  iframe.setAttribute('allowtransparency', 'true');

  // Handshake: validate messages only from the iframe's origin
  const childOrigin = (() => {
    try { return new URL(iframe.src).origin; } catch (e) { return null; }
  })();

  function handleMessage(event) {
    try {
      if (!event.data || typeof event.data !== 'object') return;
      if (childOrigin && event.origin !== childOrigin) return; // strict origin check

      const data = event.data;
      // Child announces readiness
      if (data.type === 'widget-ready') {
        // Parent can optionally request preferred size, but child will proactively send it.
        // Send a confirmation so child knows parent heard it.
        // If the child provided styling/position preferences, apply them to the iframe
        try {
          const cfg = data.config || {};
          const pos = (cfg.styles && cfg.styles.position) || cfg.position || null;
          if (pos) {
            // Reset positioning edges first
            iframe.style.top = '';
            iframe.style.bottom = '';
            iframe.style.left = '';
            iframe.style.right = '';
            iframe.style.transform = '';
            iframe.style.position = 'fixed';
            const margin = 20; // px
            // Try to detect a header/navbar element on the host page to avoid overlapping it
            const headerSelectorCandidates = ['header', '.MuiAppBar-root', '.navbar', '#navbar', '.topbar', '.app-header'];
            let navbarHeight = 0;
            for (const sel of headerSelectorCandidates) {
              const el = document.querySelector(sel);
              if (el) {
                const r = el.getBoundingClientRect();
                if (r && r.height > navbarHeight) navbarHeight = r.height;
              }
            }
            // If navbarHeight was found, add a small spacing
            const topOffset = navbarHeight ? navbarHeight + margin : margin;
            switch (String(pos)) {
              case 'bottom-left':
                iframe.style.bottom = `${margin}px`; iframe.style.left = `${margin}px`; break;
              case 'top-left':
                iframe.style.top = `${topOffset}px`; iframe.style.left = `${margin}px`; break;
              case 'top-right':
                iframe.style.top = `${topOffset}px`; iframe.style.right = `${margin}px`; break;
              case 'center-left':
                // center vertically relative to viewport; add small topOffset to nudge below header if present
                iframe.style.top = navbarHeight ? `calc(50% + ${navbarHeight / 2}px)` : '50%'; iframe.style.left = `${margin}px`; iframe.style.transform = 'translateY(-50%)'; break;
              case 'center-right':
                iframe.style.top = navbarHeight ? `calc(50% + ${navbarHeight / 2}px)` : '50%'; iframe.style.right = `${margin}px`; iframe.style.transform = 'translateY(-50%)'; break;
              case 'bottom-right':
              default:
                iframe.style.bottom = `${margin}px`; iframe.style.right = `${margin}px`; break;
            }
          }
        } catch (e) {
          console.warn('[parent] error applying child position config', e);
        }

        event.source.postMessage({ type: 'parent-received-ready' }, event.origin);
        return;
      }

      // Child provides preferred size
      if (data.type === 'preferred-size' && data.width && data.height) {
  // Apply size in pixels to the iframe
  iframe.style.width = `${Math.max(0, Number(data.width))}px`;
  iframe.style.height = `${Math.max(0, Number(data.height))}px`;

        // Inform child that parent applied the size
        const payload = { type: 'parent-applied-size', width: Number(data.width), height: Number(data.height) };
        try {
          iframe.contentWindow.postMessage(payload, event.origin);
        } catch (err) {
          console.warn('[parent] could not postMessage to iframe.contentWindow', err);
        }
        return;
      }

      // Child ack after applying container size
      if (data.type === 'child-ack') {
        return;
      }
    } catch (e) {
      console.error('[parent] message handler error', e);
    }
  }

  window.addEventListener('message', handleMessage);

  // Clean up on unload
  const cleanup = () => {
    window.removeEventListener('message', handleMessage);
  };
  window.addEventListener('beforeunload', cleanup);

  // append after wiring handlers
  document.body.appendChild(iframe);
  try {
    const rect = iframe.getBoundingClientRect();
  } catch (e) {
  }
})();
