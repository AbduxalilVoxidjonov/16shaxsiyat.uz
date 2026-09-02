import { useMutation } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AiProviderKind, TestAiConnectionResult } from '../model/types';

/**
 * `POST /api/admin/ai/providers/{provider}/test` — docs/07, 3.5-bo'lim: "Aloqa tekshiruvi →
 * `{ ok, latencyMs, message }`". Muvaffaqiyatsiz tekshiruv HTTP xatosi EMAS (200 bilan
 * `ok: false` qaytadi) — chaqiruvchi `result.ok` ni tekshiradi, `message` xato TURINI
 * bildiradi (`prompts/28` MAXSUS DIQQAT #3).
 */
export function useTestAiProviderConnection() {
  return useMutation({
    mutationFn: (provider: AiProviderKind) =>
      adminRequest<TestAiConnectionResult>(`/api/admin/ai/providers/${provider}/test`, {
        method: 'POST',
      }),
  });
}
