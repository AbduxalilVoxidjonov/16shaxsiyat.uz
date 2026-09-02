/**
 * `before/after` JSON'ini tushunarli shaklga keltiruvchi pastki mantiq — `docs/11` A-9:
 * "qator kengaytirilsa `before/after` JSON farqi ko'rinadi (oddiy diff ko'rinishi)",
 * `prompts/28` MAXSUS DIQQAT #6: "UI ham `before/after` JSON'ini xom ko'rsatmasin,
 * tushunarli shaklga keltirsin." Backend `AuditSnapshot` orqali shaxsiy ma'lumotsiz,
 * tekis (bir darajali) obyekt sifatida yozadi (`Application/Admin/Common/AuditSnapshot.cs`),
 * shu sabab bu yerda maydon darajasidagi (field-level) diff yetarli — ikkala tomonni
 * yonma-yon xom JSON sifatida ko'rsatish shart emas.
 */

export type AuditDiffChange = 'added' | 'removed' | 'changed';

export interface AuditDiffRow {
  key: string;
  change: AuditDiffChange;
  before: string | null;
  after: string | null;
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return '—';
  if (typeof value === 'string') return value;
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  return JSON.stringify(value);
}

function parseJsonObject(raw: string | null | undefined): Record<string, unknown> | null {
  if (!raw) return null;
  try {
    const parsed: unknown = JSON.parse(raw);
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      return parsed as Record<string, unknown>;
    }
    return null;
  } catch {
    return null;
  }
}

/**
 * `beforeJson`/`afterJson` (xom JSON matn yoki `null`) dan maydon darajasidagi diff qatorlarini
 * hisoblaydi. Faqat O'ZGARGAN (qo'shilgan/olib tashlangan/qiymati farq qilgan) maydonlar
 * qaytariladi — o'zgarmagan maydonlar diff'da shovqin bo'ladi. Parslanmaydigan yoki bo'sh
 * JSON uchun bo'sh massiv qaytadi (UI bo'sh holatni o'zi ko'rsatadi).
 */
export function computeAuditDiff(
  beforeJson: string | null | undefined,
  afterJson: string | null | undefined,
): AuditDiffRow[] {
  const before = parseJsonObject(beforeJson);
  const after = parseJsonObject(afterJson);
  if (!before && !after) return [];

  const keys = new Set<string>([...Object.keys(before ?? {}), ...Object.keys(after ?? {})]);
  const rows: AuditDiffRow[] = [];

  for (const key of keys) {
    const hasBefore = before !== null && Object.hasOwn(before, key);
    const hasAfter = after !== null && Object.hasOwn(after, key);

    if (hasBefore && !hasAfter) {
      rows.push({ key, change: 'removed', before: formatValue(before?.[key]), after: null });
      continue;
    }
    if (!hasBefore && hasAfter) {
      rows.push({ key, change: 'added', before: null, after: formatValue(after?.[key]) });
      continue;
    }

    const beforeFormatted = formatValue(before?.[key]);
    const afterFormatted = formatValue(after?.[key]);
    if (beforeFormatted !== afterFormatted) {
      rows.push({ key, change: 'changed', before: beforeFormatted, after: afterFormatted });
    }
  }

  rows.sort((a, b) => a.key.localeCompare(b.key));
  return rows;
}
