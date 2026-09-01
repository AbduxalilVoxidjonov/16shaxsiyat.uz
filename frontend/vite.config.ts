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
  build: {
    outDir: 'dist',
    // Tailwind v4 build-vaqtidagi CSS transformatsiyasi sourcemap generatsiya qilmaydi
    // (@tailwindcss/vite bilan ma'lum muammo — rolldown-vite ostida SOURCEMAP_BROKEN
    // ogohlantirishi beradi). Skelet bosqichida sourcemap shart emas.
    sourcemap: false,
  },
});
