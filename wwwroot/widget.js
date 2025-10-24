// widget.js - Script embebido que carga directamente el ChatWidget
(function () {
  const scriptTag = document.currentScript;
  const botId = scriptTag.getAttribute("data-bot");
  const token = scriptTag.getAttribute("data-token");
  const allowedDomain = scriptTag.getAttribute('data-allowed-domain') || scriptTag.getAttribute('data-allowedDomain') || '';

  if (!botId) {
    console.error("❌ El atributo data-bot es requerido para el widget.");
    return;
  }

  if (!token) {
    console.error("❌ El atributo data-token es requerido para el widget.");
    return;
  }

  // Security: require allowedDomain and validate current host
  if (!allowedDomain) {
    console.warn('⚠️ Widget not loaded: missing data-allowed-domain attribute. Regenerate the integration snippet including the allowed domain.');
    return;
  }

  try {
    const allowedHost = (new URL(allowedDomain)).host;
    if (window.location.host !== allowedHost) {
      return; // not authorized here
    }
  } catch (e) {
    console.warn('⚠️ Invalid data-allowed-domain value for widget.');
    return;
  }

  function initWidget() {
    // Crear iframe que carga directamente el ChatWidget (que ya tiene su propio ícono flotante)
    const iframe = document.createElement("iframe");
      iframe.src = `http://localhost:3000/widget-frame?bot=${botId}&token=${token}`;
    // Limit iframe to the visible widget area so it doesn't block the whole page

    iframe.setAttribute('allowtransparency', 'true');

    document.body.appendChild(iframe);
  }

  // Esperar a que el DOM esté listo
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initWidget);
  } else {
    // DOM ya está listo
    initWidget();
  }
})();