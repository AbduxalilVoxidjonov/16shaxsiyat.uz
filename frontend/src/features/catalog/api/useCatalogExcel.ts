import { useMutation } from '@tanstack/react-query';
import { env } from '@/shared/config/env';
import { getAdminAccessToken } from '@/shared/api/adminClient';
import { AppError } from '@/shared/api/AppError';
import { excelParseResponseSchema, problemDetailsSchema } from '../model/importSchema';

/** Blob javobini brauzerda faylga yuklab beradi (native `<a download>`, kutubxonasiz). */
function triggerBlobDownload(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

/**
 * `.xlsx` yuklab olish — `adminRequest` JSON javob kutadi, shu sabab bu yerda xom `fetch`
 * ishlatiladi (`Authorization` va cookie qo'lda qo'shiladi).
 */
async function downloadExcel(path: string, fallbackFileName: string): Promise<void> {
  const token = getAdminAccessToken();

  let response: Response;
  try {
    response = await fetch(`${env.apiBaseUrl}${path}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      credentials: 'include',
    });
  } catch (cause) {
    throw AppError.networkError(cause);
  }

  if (!response.ok) {
    throw new AppError({
      code: 'EXCEL_DOWNLOAD_FAILED',
      message: `Faylni yuklab bo'lmadi (${String(response.status)})`,
      status: response.status,
    });
  }

  triggerBlobDownload(await response.blob(), fileNameFrom(response) ?? fallbackFileName);
}

/** `Content-Disposition` dagi fayl nomi (bo'lmasa `null`) — server bergan nom afzal. */
function fileNameFrom(response: Response): string | null {
  const header = response.headers.get('content-disposition');
  if (!header) return null;
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(header);
  const raw = match?.[1];
  if (!raw) return null;
  try {
    return decodeURIComponent(raw);
  } catch {
    return raw;
  }
}

/**
 * `GET /api/admin/catalog/import-template.xlsx` — bo'sh shablon (ko'rsatma varag'i va bitta
 * namunaviy to'ldirilgan qator bilan).
 */
export function useDownloadImportTemplate() {
  return useMutation({
    mutationFn: () =>
      downloadExcel('/api/admin/catalog/import-template.xlsx', 'shaxsiyat-anketa-shablon.xlsx'),
  });
}

/**
 * `GET /api/admin/catalog/tests/{id}/export.xlsx` — mavjud anketani Excel'ga chiqaradi.
 * Bo'sh shablondan yaxshiroq namuna: savollar, shkalalar va og'irliklar bilan to'ldirilgan
 * HAQIQIY fayl chiqadi. Tizim metodikasi uchun ham ishlaydi (o'qish amali, BR-8 buzilmaydi).
 */
export function useDownloadTestExcel() {
  return useMutation({
    mutationFn: ({ id, code }: { id: string; code: string }) =>
      downloadExcel(`/api/admin/catalog/tests/${id}/export.xlsx`, `anketa-${code}.xlsx`),
  });
}

/**
 * `POST /api/admin/catalog/import/parse-excel` — faylni serverga yuboradi, u ClosedXML bilan
 * o'qib MAVJUD JSON import sxemasidagi obyektni qaytaradi. Hech narsa saqlanmaydi: natija
 * `validateTestImportObject` orqali o'sha oldindan ko'rish/validatsiya yo'liga tushadi.
 */
export function useParseExcelMutation() {
  return useMutation({
    mutationFn: async (file: File) => {
      const form = new FormData();
      form.append('file', file);

      const token = getAdminAccessToken();

      let response: Response;
      try {
        response = await fetch(`${env.apiBaseUrl}/api/admin/catalog/import/parse-excel`, {
          method: 'POST',
          headers: token ? { Authorization: `Bearer ${token}` } : {},
          credentials: 'include',
          body: form,
        });
      } catch (cause) {
        throw AppError.networkError(cause);
      }

      // Xom `fetch` — `apiRequest` (shared) JSON tanani o'zi serializatsiya qiladi va
      // `FormData` ni qo'llab-quvvatlamaydi; `multipart/form-data` chegarasini brauzer
      // o'zi qo'yishi kerak, shu sabab `Content-Type` ATAYLAB qo'yilmaydi.
      const body: unknown = await response.json().catch(() => null);

      if (!response.ok) {
        const problem = problemDetailsSchema.safeParse(body);
        throw new AppError({
          code: problem.success ? (problem.data.code ?? 'EXCEL_PARSE_FAILED') : 'EXCEL_PARSE_FAILED',
          message: problem.success
            ? (problem.data.detail ?? problem.data.title ?? "Faylni o'qib bo'lmadi.")
            : "Faylni o'qib bo'lmadi.",
          status: response.status,
        });
      }

      if (!excelParseResponseSchema.safeParse(body).success) {
        throw new AppError({
          code: 'EXCEL_PARSE_UNEXPECTED',
          message: 'Server javobi kutilgan shaklda emas.',
          status: response.status,
        });
      }

      return body;
    },
  });
}
