import { useMutation } from '@tanstack/react-query';
import { env } from '@/shared/config/env';
import { getAdminAccessToken } from '@/shared/api/adminClient';
import { AppError } from '@/shared/api/AppError';
import type { StudentsListQuery } from '../model/types';
import { buildStudentsQueryString } from './useStudentsQuery';

/** `AppError.code` — eksport endpointi hali backend'da yo'q (`404`) yoki qasddan o'chirilgan (`501`). */
export const EXPORT_NOT_AVAILABLE_CODE = 'EXPORT_NOT_AVAILABLE';

function buildExportFilename(): string {
  const today = new Date().toISOString().slice(0, 10);
  return `oquvchilar-eksport-${today}.xlsx`;
}

/** Blob javobini brauzerda faylga yuklab beradi (native `<a download>`, portal/kutubxonasiz). */
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

// TODO(P27): eksport endpointi hali yo'q — `GET /api/admin/students/export` P27'da qo'shiladi
// (docs/07 3.2). Hozircha `404`/`501` kelishi kutiladi; shu holatlar pastda alohida
// `EXPORT_NOT_AVAILABLE_CODE` bilan tushunarli xabarga aylantiriladi, ilova yiqilmaydi.
async function exportStudents(query: StudentsListQuery): Promise<void> {
  const url = `${env.apiBaseUrl}/api/admin/students/export?${buildStudentsQueryString(query)}`;
  const token = getAdminAccessToken();

  let response: Response;
  try {
    response = await fetch(url, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      credentials: 'include',
    });
  } catch (cause) {
    throw AppError.networkError(cause);
  }

  if (!response.ok) {
    if (response.status === 404 || response.status === 501) {
      throw new AppError({
        code: EXPORT_NOT_AVAILABLE_CODE,
        message: 'Eksport hali mavjud emas',
        status: response.status,
      });
    }
    throw new AppError({
      code: 'EXPORT_FAILED',
      message: `Eksport muvaffaqiyatsiz (${String(response.status)})`,
      status: response.status,
    });
  }

  const blob = await response.blob();
  triggerBlobDownload(blob, buildExportFilename());
}

/**
 * Joriy filtr bilan `.xlsx` eksport — docs/07 3.2, docs/11 A-4 ("Yuqori o'ngda:
 * 'Excel'ga eksport'"). `useMutation` — `useDeleteSchool`/`useToggleSchoolActive` (P23)
 * bilan bir xil naqsh: chaqiruvchi (`StudentsPage.tsx`) `mutateAsync` + `try/catch` orqali
 * xatoni `toast`ga aylantiradi, `isPending` bilan yuklanish indikatorini ko'rsatadi.
 */
export function useExportStudentsMutation() {
  return useMutation({
    mutationFn: exportStudents,
  });
}
