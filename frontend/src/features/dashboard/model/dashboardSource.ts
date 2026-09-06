/**
 * Boshqaruv panelining MANBA kesimi (P48) — `?source=school|public`.
 *
 * Egasining talabi: "dashboard raqamlari maktab va ommaviy bo'yicha aralashmasin". Shu
 * sabab bu yerda "hammasi" varianti ATAYLAB YO'Q — har bir ko'rsatkich doim aniq bitta
 * oqimga tegishli va tanlangan oqim ekranda ko'rinib turadi.
 *
 * Standart — `school`: panel tarixan maktab oqimining paneli, ommaviy makon raqamlari esa
 * o'zining alohida bo'limida ham bor (`/admin/ommaviy`). Parametr URL'da saqlanadi
 * (`dateRangeFilters.ts` bilan bir xil naqsh) — orqaga tugmasi va chuqur havola ishlaydi.
 */
export const DASHBOARD_SOURCE_VALUES = ['school', 'public'] as const;
export type DashboardSource = (typeof DASHBOARD_SOURCE_VALUES)[number];

/** URL'da qiymat bo'lmasa yoki noma'lum bo'lsa — `school`. */
export const DEFAULT_DASHBOARD_SOURCE: DashboardSource = 'school';

export function readDashboardSource(searchParams: URLSearchParams): DashboardSource {
  const value = searchParams.get('source');
  return value && (DASHBOARD_SOURCE_VALUES as readonly string[]).includes(value)
    ? (value as DashboardSource)
    : DEFAULT_DASHBOARD_SOURCE;
}
