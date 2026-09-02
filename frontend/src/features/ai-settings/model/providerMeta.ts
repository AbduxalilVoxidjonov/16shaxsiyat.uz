import type { AiProviderKind } from './types';

/**
 * Tavsiya etilgan modellar — backend `RecommendedAiModels`
 * (`src/StudentRoadMap.Application/Ai/RecommendedAiModels.cs`) bilan AYNAN bir xil qiymatlar.
 * `prompts/28` MAXSUS DIQQAT #5: bular faqat TAKLIF — admin istalgan model nomini qo'lda
 * kiritishi mumkin, majburlanmaydi (shu sabab `model` maydoni oddiy matn input, select emas).
 */
export const RECOMMENDED_AI_MODELS: Record<AiProviderKind, string> = {
  Gemini: 'gemini-2.0-flash',
  OpenAi: 'gpt-4.1-mini',
  Anthropic: 'claude-sonnet-5',
};

/** Provayder kartasi tartibi va i18n kalit prefiksi — `docs/11` A-7: "3 ta provider kartasi". */
export const AI_PROVIDER_DISPLAY_ORDER: readonly AiProviderKind[] = ['Gemini', 'OpenAi', 'Anthropic'];
