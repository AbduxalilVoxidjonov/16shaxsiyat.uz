import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const dirname = path.dirname(fileURLToPath(import.meta.url));

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(dirname, './src'),
    },
  },
  // Faqat DEV serveri uchun — ishlab chiqarish build'iga ta'sir qilmaydi.
  // Namoyish/sinov uchun ilova bitta origin ostida ochiladi (Cloudflare tunnel),
  // shuning uchun `/api` so'rovlari lokal backendga proksilanadi va tashqi
  // host nomiga ruxsat beriladi (Vite standart holatda notanish `Host` ni rad etadi).
  server: {
    host: true,
    allowedHosts: true,
    proxy: {
      '/api': {
        target: process.env.DEV_API_PROXY_TARGET ?? 'http://localhost:5080',
        changeOrigin: true,
      },
      '/swagger': {
        target: process.env.DEV_API_PROXY_TARGET ?? 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    // Tailwind v4 build-vaqtidagi CSS transformatsiyasi sourcemap generatsiya qilmaydi
    // (@tailwindcss/vite bilan ma'lum muammo — rolldown-vite ostida SOURCEMAP_BROKEN
    // ogohlantirishi beradi). Skelet bosqichida sourcemap shart emas.
    sourcemap: false,
  },
});
