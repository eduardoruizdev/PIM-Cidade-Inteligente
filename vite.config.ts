import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    host: '127.0.0.1',
    proxy: { '/api': { target: 'http://localhost:5080', changeOrigin: true } },
  },
  build: {
    rolldownOptions: {
      output: {
        manualChunks(id) {
          const path = id.replaceAll('\\', '/');
          if (/node_modules\/(leaflet|react-leaflet|@react-leaflet)/.test(path)) return 'maps';
          if (/node_modules\/(recharts|d3-|victory|lodash|decimal)/.test(path)) return 'charts';
          if (/node_modules\/(react|react-dom|react-router|scheduler)\//.test(path)) return 'react';
        },
      },
    },
  },
});
