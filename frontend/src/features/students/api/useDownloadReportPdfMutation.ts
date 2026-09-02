import { useMutation } from '@tanstack/react-query';
import { env } from '@/shared/config/env';
import { getAdminAccessToken } from '@/shared/api/adminClient';
import { AppError } from '@/shared/api/AppError';

/** Blob javobini brauzerda faylga yuklab beradi — `useExportStudentsMutation.ts` (P24) bilan bir xil naqsh. */
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

async function downloadReportPdf(assessmentId: string): Promise<void> {
  const url = `${env.apiBaseUrl}/api/admin/assessments/${assessmentId}/report.pdf`;
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
    throw new AppError({
      code: 'PDF_DOWNLOAD_FAILED',
      message: `PDF hisobotni yuklab bo'lmadi (${String(response.status)})`,
      status: response.status,
    });
  }

  const blob = await response.blob();
  triggerBlobDownload(blob, `hisobot-${assessmentId}.pdf`);
}

/** `GET /api/admin/assessments/{id}/report.pdf` — docs/07, 3.3-bo'lim (P25 sarlavha bloki: "PDF yuklab olish"). */
export function useDownloadReportPdfMutation() {
  return useMutation({
    mutationFn: downloadReportPdf,
  });
}
