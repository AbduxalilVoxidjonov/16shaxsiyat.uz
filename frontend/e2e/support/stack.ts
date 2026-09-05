import { randomBytes } from 'node:crypto';
import { spawn } from 'node:child_process';
import {
  API_BASE_URL,
  COMPOSE_FILE,
  COMPOSE_PROJECT,
  KEEP_STACK,
  REPO_ROOT,
  REUSE_STACK,
  WEB_BASE_URL,
  readAdminCredentials,
} from './config';

/**
 * Har run uchun yangi tasodifiy sirlar. Repoda hech qanday parol/kalit saqlanmaydi
 * (CLAUDE.md 4-qoida), baza esa har run boshida `down -v` bilan tozalanadi — shu sabab
 * kalit o'zgargani hech narsani buzmaydi.
 */
function generateStackSecrets(): Record<string, string> {
  return {
    E2E_DB_PASSWORD: randomBytes(18).toString('hex'),
    E2E_JWT_KEY: randomBytes(48).toString('base64'),
    E2E_ENCRYPTION_KEY: randomBytes(32).toString('base64'),
    E2E_IP_HASH_SALT: randomBytes(16).toString('hex'),
  };
}

function composeEnv(secrets: Record<string, string>): NodeJS.ProcessEnv {
  const admin = readAdminCredentials();
  return {
    ...process.env,
    ...secrets,
    E2E_ADMIN_USERNAME: admin.username,
    E2E_ADMIN_PASSWORD: admin.password,
    E2E_FRONTEND_URL: WEB_BASE_URL,
    E2E_API_PORT: String(new URL(API_BASE_URL).port),
  };
}

function runCompose(args: string[], env: NodeJS.ProcessEnv): Promise<void> {
  return new Promise((resolve, reject) => {
    const child = spawn(
      'docker',
      ['compose', '-f', COMPOSE_FILE, '-p', COMPOSE_PROJECT, ...args],
      { cwd: REPO_ROOT, env, stdio: 'inherit' },
    );
    child.on('error', reject);
    child.on('close', (code) => {
      if (code === 0) {
        resolve();
        return;
      }
      reject(new Error(`\`docker compose ${args.join(' ')}\` ${String(code)} kodi bilan tugadi.`));
    });
  });
}

/**
 * `/health` javob berguncha kutadi. Qat'iy `sleep` emas — holatga kutish: migratsiya va
 * seed (190 savol, katalog, superadmin) tugamaguncha `api` konteyneri umuman
 * ishga tushmaydi (`depends_on: service_completed_successfully`).
 */
async function waitForApi(timeoutMs: number): Promise<void> {
  const deadline = Date.now() + timeoutMs;
  let lastError = 'javob yo\'q';

  while (Date.now() < deadline) {
    try {
      const response = await fetch(`${API_BASE_URL}/health`);
      if (response.ok) return;
      lastError = `HTTP ${String(response.status)}`;
    } catch (error) {
      lastError = error instanceof Error ? error.message : String(error);
    }
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }

  throw new Error(
    `E2E API (${API_BASE_URL}/health) ${String(timeoutMs / 1000)} soniyada ko'tarilmadi: ${lastError}. ` +
      `Loglar: docker compose -f ${COMPOSE_FILE} -p ${COMPOSE_PROJECT} logs`,
  );
}

export async function startStack(): Promise<void> {
  if (REUSE_STACK) {
    await waitForApi(60_000);
    return;
  }

  const env = composeEnv(generateStackSecrets());
  // `down -v` — oldingi run qoldiqlari va bazasi butunlay o'chiriladi. Har test o'z
  // maktabini yaratsa ham, toza baza takrorlanuvchanlikni kafolatlaydi.
  await runCompose(['down', '-v', '--remove-orphans'], env);
  // `--build` MAJBURIY: `shaxsiyat-e2e-api:latest` tegi bir marta yig'ilgach, `up -d`
  // uni QAYTA yig'maydi va E2E jimgina ESKI backendga qarshi ishlaydi. Aynan shu tufayli
  // `GET /api/admin/schools/link-health` (backendda bor) E2E stekida `404` qaytarayotgan
  // edi — boshqaruv paneli konsolida xato, sabab esa ko'rinmasdi. Docker qatlam keshi
  // tufayli manba o'zgarmagan bo'lsa qayta yig'ish deyarli bepul.
  await runCompose(['up', '-d', '--build'], env);
  await waitForApi(240_000);
}

export async function stopStack(): Promise<void> {
  if (REUSE_STACK || KEEP_STACK) return;
  await runCompose(['down', '-v', '--remove-orphans'], composeEnv(generateStackSecrets()));
}
