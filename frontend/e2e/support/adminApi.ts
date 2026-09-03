import { API_BASE_URL, readAdminCredentials } from './config';

export interface E2ESchool {
  id: string;
  name: string;
  slug: string;
  /** To'liq ommaviy havola (`http://127.0.0.1:5199/t/<slug>?k=<token>`). */
  publicUrl: string;
  /** Faqat yo'l + `?k=` — `page.goto()` `baseURL` bilan ishlatadi. */
  publicPath: string;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

async function callApi<T>(
  method: string,
  routePath: string,
  options: { token?: string; body?: unknown; clientIp: string },
): Promise<T> {
  const headers: Record<string, string> = {
    // Har chaqiruv o'z "mijoz IP"si bilan ketadi — tezlik cheklovi bo'laklari
    // testlar orasida aralashmaydi (`docker-compose.e2e.yml` dagi `App__KnownProxies` izohi).
    'X-Forwarded-For': options.clientIp,
  };
  if (options.token) headers.Authorization = `Bearer ${options.token}`;
  if (options.body !== undefined) headers['Content-Type'] = 'application/json';

  const response = await fetch(`${API_BASE_URL}${routePath}`, {
    method,
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (!response.ok) {
    throw new Error(`${method} ${routePath} → ${String(response.status)}: ${await response.text()}`);
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

/**
 * TOPILMA (P30-1): bir vaqtda ikkita `POST /api/auth/login` yuborilsa backend
 * `409 CONCURRENCY_CONFLICT` qaytaradi (bitta superadmin yozuvi optimistik
 * konkurentlik bilan yangilanadi). Shu sabab bu yerda qayta urinish bor — va
 * butun to'plam uchun token `globalSetup` da BIR MARTA olinadi (`admin-token.json`),
 * ya'ni testlar bu holatga umuman kirmaydi.
 */
export async function loginAsAdminApi(clientIp: string, attempts = 5): Promise<string> {
  const { username, password } = readAdminCredentials();

  for (let attempt = 1; ; attempt += 1) {
    try {
      const result = await callApi<{ accessToken: string }>('POST', '/api/auth/login', {
        body: { username, password },
        clientIp,
      });
      return result.accessToken;
    } catch (error) {
      const isConflict = error instanceof Error && error.message.includes('409');
      if (!isConflict || attempt >= attempts) throw error;
      await new Promise((resolve) => setTimeout(resolve, 200 * attempt));
    }
  }
}

/**
 * Test uchun alohida maktab. Nom unikal — `slug` nom+tumandan hosil bo'ladi, shu sabab
 * har test o'z havolasiga ega bo'ladi va testlar bir-biriga xalaqit bermaydi.
 */
export async function createSchool(
  token: string,
  clientIp: string,
  namePrefix: string,
): Promise<E2ESchool> {
  const unique = `${String(Date.now()).slice(-8)}${Math.random().toString(36).slice(2, 6)}`;
  const name = `E2E ${namePrefix} ${unique}`;

  const created = await callApi<{ id: string; name: string; slug: string; publicUrl: string }>(
    'POST',
    '/api/admin/schools',
    {
      token,
      clientIp,
      body: {
        name,
        region: "Farg'ona",
        district: "Qo'qon",
        schoolNumber: null,
        contactPerson: null,
        contactPhone: null,
        accessCode: null,
        // Ataylab katta — bir necha test bir maktabda emas, lekin kunlik limit
        // qoldiq ma'lumot sababli testni to'xtatib qo'ymasligi uchun.
        dailyRegistrationLimit: 500,
        notes: 'P30 E2E — avtomatik yaratilgan, test oxirida o\'chiriladi.',
      },
    },
  );

  const url = new URL(created.publicUrl);
  return {
    id: created.id,
    name: created.name,
    slug: created.slug,
    publicUrl: created.publicUrl,
    publicPath: `${url.pathname}${url.search}`,
  };
}

export async function setSchoolActive(
  token: string,
  clientIp: string,
  schoolId: string,
  active: boolean,
): Promise<void> {
  const detail = await callApi<{ isActive: boolean }>('GET', `/api/admin/schools/${schoolId}`, {
    token,
    clientIp,
  });
  if (detail.isActive === active) return;
  await callApi('POST', `/api/admin/schools/${schoolId}/toggle-active`, { token, clientIp });
}

/**
 * Test o'z ma'lumotini o'zi tozalaydi: avval o'quvchilar (aks holda maktabni o'chirishga
 * `409` keladi — `docs/07` 3.1), keyin maktabning o'zi.
 */
export async function deleteSchoolCascade(
  token: string,
  clientIp: string,
  schoolId: string,
): Promise<void> {
  const students = await callApi<PagedResult<{ id: string }>>(
    'GET',
    `/api/admin/students?schoolId=${schoolId}&page=1&pageSize=100`,
    { token, clientIp },
  );

  for (const student of students.items) {
    await callApi('DELETE', `/api/admin/students/${student.id}?hard=true`, { token, clientIp });
  }

  await callApi('DELETE', `/api/admin/schools/${schoolId}`, { token, clientIp });
}

/**
 * Nomi bo'yicha topilgan maktablarni (odatda UI orqali yaratilganlarini) o'chiradi —
 * test yarmida yiqilsa ham E2E bazasida qoldiq qolmasligi uchun.
 */
export async function deleteSchoolsBySearch(
  token: string,
  clientIp: string,
  search: string,
): Promise<void> {
  const found = await callApi<PagedResult<{ id: string }>>(
    'GET',
    `/api/admin/schools?search=${encodeURIComponent(search)}&page=1&pageSize=50`,
    { token, clientIp },
  );

  for (const item of found.items) {
    await deleteSchoolCascade(token, clientIp, item.id);
  }
}
