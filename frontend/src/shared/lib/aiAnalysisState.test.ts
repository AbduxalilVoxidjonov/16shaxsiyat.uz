import { describe, expect, it } from 'vitest';
import { hasAiReport, resolveAiAnalysisViewState } from './aiAnalysisState';

const REPORT = { status: 'Succeeded', summary: 'Xulosa' };
const FAILED_RECORD = { status: 'Failed', summary: null };

describe('hasAiReport', () => {
  it("yozuv yo'q bo'lsa — hisobot yo'q", () => {
    expect(hasAiReport(null)).toBe(false);
    expect(hasAiReport(undefined)).toBe(false);
  });

  it("yiqilgan yozuv hisobot EMAS — \"tahlil qilinmagan\" bilan chalkashmasligi uchun alohida", () => {
    expect(hasAiReport(FAILED_RECORD)).toBe(false);
  });

  it("muvaffaqiyatli yozuv — hisobot", () => {
    expect(hasAiReport(REPORT)).toBe(true);
  });

  it("navbatdagi yozuv yonida eski mazmun turgan bo'lsa — hisobot BOR", () => {
    // `docs/09` 11-bo'lim: yangi tahlil muvaffaqiyatli bo'lmaguncha eskisi `IsCurrent` qoladi.
    expect(hasAiReport({ status: 'Running', summary: 'Oldingi xulosa' })).toBe(true);
    expect(hasAiReport({ status: 'Running', summary: '   ' })).toBe(false);
  });
});

describe('resolveAiAnalysisViewState', () => {
  it("Completed + hisobot yo'q → \"AI tahlil qilish\", tasdiqsiz", () => {
    const view = resolveAiAnalysisViewState({ status: 'Completed', analysis: null });

    expect(view.action).toBe('run');
    expect(view.requiresConfirmation).toBe(false);
    expect(view.showReport).toBe(false);
    expect(view.showSkeleton).toBe(false);
    expect(view.blockedReason).toBeNull();
  });

  it('Analyzed + hisobot bor → hisobot va "Qayta tahlil", tasdiq bilan', () => {
    const view = resolveAiAnalysisViewState({ status: 'Analyzed', analysis: REPORT });

    expect(view.action).toBe('rerun');
    expect(view.requiresConfirmation).toBe(true);
    expect(view.showReport).toBe(true);
  });

  it('Analyzing + MAVJUD hisobot → eski hisobot QOLADI, skelet YO\'Q', () => {
    const view = resolveAiAnalysisViewState({ status: 'Analyzing', analysis: REPORT });

    expect(view.showReport).toBe(true);
    expect(view.showSkeleton).toBe(false);
    expect(view.showAnalyzingBanner).toBe(true);
    // Tahlil allaqachon navbatda — ikkinchi marta navbatga qo'yish AI xarajatini ikkilantiradi.
    expect(view.action).toBeNull();
  });

  it("Analyzing + hech qachon tahlil qilinmagan → skelet", () => {
    const view = resolveAiAnalysisViewState({ status: 'Analyzing', analysis: null });

    expect(view.showSkeleton).toBe(true);
    expect(view.showReport).toBe(false);
    expect(view.showAnalyzingBanner).toBe(true);
  });

  it("AnalysisFailed → sabab kartasi va \"Qayta urinish\"; hisobot yo'q bo'lsa tasdiqsiz", () => {
    const view = resolveAiAnalysisViewState({ status: 'AnalysisFailed', analysis: FAILED_RECORD });

    expect(view.showFailure).toBe(true);
    expect(view.action).toBe('retry');
    expect(view.requiresConfirmation).toBe(false);
    expect(view.showSkeleton).toBe(false);
  });

  it('AnalysisFailed + oldingi hisobot hali turgan bo\'lsa → tasdiq so\'raladi', () => {
    const view = resolveAiAnalysisViewState({ status: 'AnalysisFailed', analysis: REPORT });

    expect(view.showReport).toBe(true);
    expect(view.requiresConfirmation).toBe(true);
  });

  it.each(['Draft', 'InProgress', 'Abandoned'] as const)(
    "%s → tugma YO'Q, sabab oldindan aytiladi (backend `409` qaytaradi)",
    (status) => {
      const view = resolveAiAnalysisViewState({ status, analysis: null });

      expect(view.action).toBeNull();
      expect(view.blockedReason).toBe('sessionNotCompleted');
    },
  );

  it("holat NOMA'LUM bo'lsa tugma ochiq qoladi — serverning qarori taxmin qilinmaydi", () => {
    const view = resolveAiAnalysisViewState({ status: null, analysis: null });

    expect(view.action).toBe('run');
    expect(view.blockedReason).toBeNull();
  });

  it("sessiya umuman yo'q bo'lsa — tugma ham, skelet ham yo'q", () => {
    const view = resolveAiAnalysisViewState({
      status: null,
      analysis: null,
      hasAssessment: false,
    });

    expect(view.action).toBeNull();
    expect(view.blockedReason).toBe('noAssessment');
    expect(view.showSkeleton).toBe(false);
  });
});
