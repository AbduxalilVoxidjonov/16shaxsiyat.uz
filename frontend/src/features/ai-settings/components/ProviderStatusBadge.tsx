import { useTranslation } from 'react-i18next';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge';
import type { ProviderStatus } from '../model/providerStatus';

const VARIANT_BY_STATUS: Record<ProviderStatus, BadgeVariant> = {
  'no-key': 'neutral',
  default: 'primary',
  active: 'success',
  inactive: 'warning',
};

const LABEL_KEY_BY_STATUS: Record<ProviderStatus, string> = {
  'no-key': 'aiSettings.provider.status.noKey',
  default: 'aiSettings.provider.status.default',
  active: 'aiSettings.provider.status.active',
  inactive: 'aiSettings.provider.status.inactive',
};

export function ProviderStatusBadge({ status }: { status: ProviderStatus }) {
  const { t } = useTranslation();
  return <Badge variant={VARIANT_BY_STATUS[status]}>{t(LABEL_KEY_BY_STATUS[status])}</Badge>;
}
