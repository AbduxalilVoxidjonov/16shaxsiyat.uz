import { describe, expect, it } from 'vitest';
import { computeAuditDiff } from './auditDiff';

describe('computeAuditDiff', () => {
  it('ikkala tomon ham bo\'sh bo\'lsa bo\'sh massiv qaytaradi', () => {
    expect(computeAuditDiff(null, null)).toEqual([]);
    expect(computeAuditDiff(undefined, undefined)).toEqual([]);
  });

  it("faqat o'zgargan maydonlarni qaytaradi, o'zgarmaganlarini o'tkazib yuboradi", () => {
    const before = JSON.stringify({ name: 'Eski nom', isActive: true, region: "Farg'ona" });
    const after = JSON.stringify({ name: 'Yangi nom', isActive: true, region: "Farg'ona" });

    const diff = computeAuditDiff(before, after);

    expect(diff).toEqual([{ key: 'name', change: 'changed', before: 'Eski nom', after: 'Yangi nom' }]);
  });

  it("faqat `after`da bo'lgan maydonni 'qo'shildi' deb belgilaydi", () => {
    const diff = computeAuditDiff(null, JSON.stringify({ studentId: 'abc-123' }));

    expect(diff).toEqual([{ key: 'studentId', change: 'added', before: null, after: 'abc-123' }]);
  });

  it("faqat `before`da bo'lgan maydonni 'olib tashlandi' deb belgilaydi", () => {
    const diff = computeAuditDiff(JSON.stringify({ accessCode: '123456' }), null);

    expect(diff).toEqual([{ key: 'accessCode', change: 'removed', before: '123456', after: null }]);
  });

  it('son va mantiqiy qiymatlarni matn sifatida formatlaydi', () => {
    const before = JSON.stringify({ dailyRegistrationLimit: 500, isActive: false });
    const after = JSON.stringify({ dailyRegistrationLimit: 1000, isActive: true });

    const diff = computeAuditDiff(before, after);

    expect(diff).toEqual([
      { key: 'dailyRegistrationLimit', change: 'changed', before: '500', after: '1000' },
      { key: 'isActive', change: 'changed', before: 'false', after: 'true' },
    ]);
  });

  it('parslanmaydigan JSON uchun bo\'sh massiv qaytaradi', () => {
    expect(computeAuditDiff('not-json', 'also not json')).toEqual([]);
  });

  it('natijani kalit bo\'yicha alifbo tartibida qaytaradi', () => {
    const before = JSON.stringify({ zeta: 1, alpha: 1 });
    const after = JSON.stringify({ zeta: 2, alpha: 2 });

    const diff = computeAuditDiff(before, after);

    expect(diff.map((row) => row.key)).toEqual(['alpha', 'zeta']);
  });
});
