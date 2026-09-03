import { describe, expect, it } from 'vitest';
import { buildAssessmentsQueryString } from '../api/useAssessmentsQuery';
import { hasActiveAssessmentsFilters, readAssessmentsFilters } from './assessmentsFilters';

function read(search: string) {
  return readAssessmentsFilters(new URLSearchParams(search));
}

describe('readAssessmentsFilters', () => {
  it("boshqaruv panelidan kelgan `?status=Analyzing` ni o'qiydi", () => {
    expect(read('status=Analyzing').status).toBe('Analyzing');
  });

  it("noma'lum holat qiymati jimgina tashlanadi", () => {
    expect(read('status=HACKED').status).toBe('');
  });

  it('faqat `YYYY-MM-DD` shaklidagi sana qabul qilinadi', () => {
    expect(read('from=2026-08-01&to=salom')).toMatchObject({ from: '2026-08-01', to: '' });
  });

  it('filtrsiz URL da hech bir filtr faol emas', () => {
    expect(hasActiveAssessmentsFilters(read('page=2&sort=startedAt'))).toBe(false);
    expect(hasActiveAssessmentsFilters(read('schoolId=school-1'))).toBe(true);
  });
});

describe('buildAssessmentsQueryString', () => {
  it('sahifalash va saralashni yuboradi', () => {
    const qs = buildAssessmentsQueryString({ page: 2, pageSize: 20, sort: '-startedAt' });
    expect(qs).toContain('page=2');
    expect(qs).toContain('pageSize=20');
    expect(qs).toContain('sort=-startedAt');
  });

  it("sana oralig'ini kun boshi va kun OXIRIGACHA kengaytiradi", () => {
    const qs = new URLSearchParams(
      buildAssessmentsQueryString({ page: 1, pageSize: 20, from: '2026-08-01', to: '2026-08-30' }),
    );
    expect(qs.get('from')).toBe('2026-08-01T00:00:00.000Z');
    // Aks holda 30-avgust kuni boshlangan sessiyalar filtrdan tushib qolardi.
    expect(qs.get('to')).toBe('2026-08-30T23:59:59.999Z');
  });

  it("bo'sh filtrlar umuman yuborilmaydi", () => {
    const qs = new URLSearchParams(buildAssessmentsQueryString({ page: 1, pageSize: 20 }));
    expect(qs.has('status')).toBe(false);
    expect(qs.has('schoolId')).toBe(false);
    expect(qs.has('from')).toBe(false);
  });
});
