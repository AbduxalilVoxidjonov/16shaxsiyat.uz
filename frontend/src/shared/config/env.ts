/**
 * Muhit o'zgaruvchilari — docs/10-frontend-arxitektura.md, 8-bo'lim.
 * Barcha `import.meta.env` murojaatlari shu faylda markazlashtiriladi.
 */
function readEnv(key: string, fallback = ''): string {
  const value = import.meta.env[key];
  return typeof value === 'string' && value.length > 0 ? value : fallback;
}

export const env = {
  apiBaseUrl: readEnv('VITE_API_BASE_URL', 'http://localhost:5000'),
  appName: readEnv('VITE_APP_NAME', 'Salohiyat'),
  sentryDsn: readEnv('VITE_SENTRY_DSN'),
} as const;
