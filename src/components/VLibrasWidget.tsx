import { useEffect, useState } from 'react';

const scriptId = 'santos-conecta-vlibras-script';
const scriptUrl = 'https://vlibras.gov.br/app/vlibras-plugin.js';

type VlibrasGlobal = {
  Widget?: new (options?: { rootPath?: string; avatar?: string; position?: 'R' | 'L' }) => unknown;
  initBtn?: HTMLButtonElement;
};

declare global {
  interface Window {
    VLibras?: VlibrasGlobal;
    VLibrasWidget?: { initBtn?: HTMLButtonElement };
  }
}

let loadPromise: Promise<void> | undefined;

function loadVlibras(): Promise<void> {
  if (document.getElementById('vlibras-access-wrapper') || window.VLibrasWidget?.initBtn) {
    return Promise.resolve();
  }
  if (loadPromise) return loadPromise;

  loadPromise = new Promise<void>((resolve, reject) => {
    const initialize = () => {
      try {
        // The official script also auto-initializes after 50 ms. Calling the
        // constructor in onload applies the documented position before that timer.
        if (!document.getElementById('vlibras-access-wrapper') && !window.VLibrasWidget?.initBtn) {
          if (window.VLibras?.Widget) {
            new window.VLibras.Widget({ rootPath: 'https://vlibras.gov.br/app', position: 'R' });
          }
        }
        resolve();
      } catch {
        reject(new Error('Não foi possível inicializar o VLibras.'));
      }
    };

    const existing = document.getElementById(scriptId) as HTMLScriptElement | null;
    if (existing) {
      if (window.VLibras?.Widget) {
        initialize();
        return;
      }
      existing.addEventListener('load', initialize, { once: true });
      existing.addEventListener('error', () => reject(new Error('Não foi possível carregar o VLibras.')), { once: true });
      return;
    }

    const script = document.createElement('script');
    script.id = scriptId;
    script.src = scriptUrl;
    script.async = true;
    script.onload = initialize;
    script.onerror = () => reject(new Error('Não foi possível carregar o VLibras.'));
    document.head.appendChild(script);
  }).catch((error: unknown) => {
    loadPromise = undefined;
    throw error;
  });

  return loadPromise;
}

/** Loads the official Government VLibras widget once for the whole SPA. */
export function VLibrasWidget() {
  const [unavailable, setUnavailable] = useState(false);

  useEffect(() => {
    let active = true;
    const start = () => {
      void loadVlibras().catch(() => {
        if (active) setUnavailable(true);
      });
    };
    // Load only after the app's own document is ready so an unavailable
    // third-party service cannot hold up the initial page load.
    if (document.readyState === 'complete') start();
    else window.addEventListener('load', start, { once: true });
    return () => {
      active = false;
      window.removeEventListener('load', start);
    };
  }, []);

  return unavailable ? (
    <span className="vlibras-status" role="status">
      O recurso VLibras não está disponível no momento.
    </span>
  ) : null;
}
