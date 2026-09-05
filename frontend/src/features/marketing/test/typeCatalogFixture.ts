import type { Schemas } from '@/test/apiMock';

/**
 * `GET /api/public/type-catalog` javobining sinov nusxasi — 16 ta yozuv, backend qaytargani
 * kabi `code` bo'yicha alifbo tartibida (`docs/07` 1.10-bo'lim).
 *
 * Matn ATAYLAB o'rinbosar: haqiqiy kontent `SeedData/type-catalog.json` da yashaydi va uni
 * frontend testiga ko'chirish ikkita nusxa demakdir. Bu yerdagi tekshiruvlar kontentni emas,
 * SHARTNOMANI (yozuvlar soni, havolalar, maydonlarning ekranga chiqishi) sinaydi.
 */
export const TYPE_CODES = [
  'ENFJ',
  'ENFP',
  'ENTJ',
  'ENTP',
  'ESFJ',
  'ESFP',
  'ESTJ',
  'ESTP',
  'INFJ',
  'INFP',
  'INTJ',
  'INTP',
  'ISFJ',
  'ISFP',
  'ISTJ',
  'ISTP',
] as const;

export const TYPE_CATALOG_BODY = {
  types: TYPE_CODES.map((code) => ({
    code,
    name: `${code} nomi`,
    shortDescription: `${code} qisqa tavsifi`,
    longDescription: `${code} to'liq tavsifi`,
    strengths: [`${code} kuchli tomoni`],
    growthAreas: [`${code} o'sish yo'nalishi`],
    careerHints: [`${code} kasb maslahati`],
  })),
} satisfies Schemas['GetTypeCatalogResult'];
