import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
// Variable-shriftlar: `@font-face` qoidalari va `.woff2` fayllari paketning o'zidan keladi
// (Google Fonts'ga tashqi so'rov yo'q). `index.css` dan OLDIN — `--font-sans`/`--font-display`
// shu oilalarga tayanadi.
import '@fontsource-variable/inter';
import '@fontsource-variable/plus-jakarta-sans';
import './index.css';
import App from './App';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error("#root elementi topilmadi — index.html buzilgan bo'lishi mumkin.");
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
