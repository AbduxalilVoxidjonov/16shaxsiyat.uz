import type { AiProviderConfigDto } from './types';

export type ProviderStatus = 'no-key' | 'default' | 'active' | 'inactive';

/**
 * Bitta status belgisi — `docs/11` A-7: "Kalit kiritilmagan" / "Faol" / "Default" / "Nofaol".
 * Ustuvorlik: kalit yo'q bo'lsa hammasidan oldin shu ko'rsatiladi (chunki `isDefault`/`isActive`
 * kalitsiz baribir ishlamaydi); keyin `Default`, keyin oddiy `Faol`/`Nofaol`.
 */
export function getProviderStatus(config: AiProviderConfigDto | null): ProviderStatus {
  if (!config || !config.maskedApiKey) return 'no-key';
  if (config.isDefault) return 'default';
  return config.isActive ? 'active' : 'inactive';
}
