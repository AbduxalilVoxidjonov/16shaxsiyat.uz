import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import type { AiPromptTemplateDto } from '../model/types';

export interface PromptViewDialogProps {
  prompt: AiPromptTemplateDto | null;
  onClose: () => void;
}

/**
 * Prompt shablonini ko'rish — `docs/11` A-7: "ko'rish (faqat o'qish MVP'da)". Matn oddiy
 * `<pre>` bilan chiqariladi (`dangerouslySetInnerHTML` TAQIQLANGAN — CLAUDE.md 12-band).
 */
export function PromptViewDialog({ prompt, onClose }: PromptViewDialogProps) {
  const { t } = useTranslation();

  return (
    <Dialog
      open={prompt !== null}
      onClose={onClose}
      title={prompt ? `${prompt.key} · v${String(prompt.version)}` : ''}
      className="max-w-2xl"
    >
      {prompt && (
        <div className="flex flex-col gap-4">
          <div>
            <h3 className="mb-1 text-sm font-semibold text-neutral-900">
              {t('aiSettings.prompts.systemTextLabel')}
            </h3>
            <pre className="max-h-64 overflow-auto rounded-lg bg-neutral-50 p-3 text-xs whitespace-pre-wrap text-neutral-700">
              {prompt.systemText}
            </pre>
          </div>
          <div>
            <h3 className="mb-1 text-sm font-semibold text-neutral-900">
              {t('aiSettings.prompts.userTextLabel')}
            </h3>
            <pre className="max-h-64 overflow-auto rounded-lg bg-neutral-50 p-3 text-xs whitespace-pre-wrap text-neutral-700">
              {prompt.userText}
            </pre>
          </div>
        </div>
      )}
    </Dialog>
  );
}
