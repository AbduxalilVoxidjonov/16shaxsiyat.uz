import { issueAdminToken } from './support/adminToken';
import { startStack } from './support/stack';

/**
 * Butun to'plamdan oldin bir marta: E2E steki (`docker-compose.e2e.yml`) ko'tariladi —
 * alohida loyiha nomi, alohida bo'sh baza, bir martalik sirlar. Jonli stek va
 * `16shaxsiyat.uz` ma'lumotlariga TEGILMAYDI.
 *
 * Shu yerda superadmin tokeni ham bir marta olinadi — worker'lar uni fayldan o'qiydi
 * (`adminToken.ts` izohi: login cheklovi va konkurentlik ziddiyati).
 */
export default async function globalSetup(): Promise<void> {
  await startStack();
  await issueAdminToken('10.199.0.1');
}
