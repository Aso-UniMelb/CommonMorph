import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import path from 'path';

// https://vite.dev/config/
export default defineConfig({
  plugins: [svelte()],
  server: {
    port: 5173,
    proxy: {
      '/Home/api': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/User': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/Lang': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/Affix': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/Structure': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/InflectionClass': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/Elicit': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/Cell': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/ActiveLearning': { target: 'http://localhost:5041', changeOrigin: true, secure: false, timeout: 300000 },
      '/LLM': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/Survey': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/QTemplate': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/Morphophonology': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/Lemma': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
      '/download': { target: 'http://localhost:5041', changeOrigin: true, secure: false },
    }
  },
  build: {
    outDir: path.resolve(import.meta.dirname, '../wwwroot'),
    emptyOutDir: false,
  }
});
