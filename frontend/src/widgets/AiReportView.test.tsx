import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AiReportView, type AiReportSections } from './AiReportView';

const FULL_SECTIONS: AiReportSections = {
  summary: 'Umumiy xulosa matni',
  personalityPortrait: 'Shaxsiyat portreti matni',
  strengths: [{ title: 'Tahliliy fikrlash', description: 'Tavsif', evidence: 'Asos' }],
  growthAreas: [{ title: "O'sish zonasi", description: 'Tavsif', actionStep: 'Qadam' }],
  learningStyle: "O'quv uslubi matni",
  motivationProfile: 'Motivatsiya matni',
  activityAssessment: 'Aktivlik matni',
  careerSuggestions: [{ field: 'IT', why: 'Sabab', nextSteps: ['Kurs', 'Amaliyot'] }],
  studentRecommendations: ["O'quvchi tavsiyasi"],
  teacherNotes: ["O'qituvchi eslatmasi"],
  parentNotes: ['Ota-ona eslatmasi'],
  attentionFlags: [{ code: 'LOW_MOTIVATION', message: 'Motivatsiya past', severity: 'attention' }],
  reliabilityNote: 'Javoblar tez berilgan — natijani ehtiyot bilan talqin qiling.',
  disclaimer: 'Bu tahlil tashxis emas.',
};

describe('AiReportView', () => {
  it("to'liq ma'lumot bilan barcha bo'limlarni render qiladi", () => {
    render(<AiReportView sections={FULL_SECTIONS} />);
    expect(screen.getByText('Umumiy xulosa matni')).toBeInTheDocument();
    expect(screen.getByText('Tahliliy fikrlash')).toBeInTheDocument();
    expect(screen.getByText('Motivatsiya past')).toBeInTheDocument();
    expect(screen.getByText('Bu tahlil tashxis emas.')).toBeInTheDocument();
    // docs/09: `learningStyle`/`motivationProfile`/`activityAssessment` ilgari DTO'da umuman
    // yo'q edi — endi ular ham ekranga chiqadi.
    expect(screen.getByText("O'quv uslubi matni")).toBeInTheDocument();
    expect(screen.getByText('Motivatsiya matni')).toBeInTheDocument();
    expect(screen.getByText('Aktivlik matni')).toBeInTheDocument();
  });

  it("cheklovlar (reliabilityNote va disclaimer) hech qachon yashirilmaydi", () => {
    render(<AiReportView sections={FULL_SECTIONS} />);
    expect(
      screen.getByText(/Javoblar tez berilgan — natijani ehtiyot bilan talqin qiling\./),
    ).toBeInTheDocument();
    expect(screen.getByText(/Bu tahlil tashxis emas\./)).toBeInTheDocument();
  });

  it("faqat cheklov matni bo'lsa ham 'bo'sh' holatiga tushmaydi", () => {
    render(<AiReportView sections={{ disclaimer: 'Bu tahlil tashxis emas.' }} />);
    expect(screen.queryByText('AI hisobot hali mavjud emas.')).not.toBeInTheDocument();
    expect(screen.getByText(/Bu tahlil tashxis emas\./)).toBeInTheDocument();
  });

  it("to'ldirilmagan maydonlar (description/evidence/actionStep null) bo'sh qator chiqarmaydi", () => {
    const { container } = render(
      <AiReportView
        sections={{
          strengths: [{ title: 'Faqat sarlavha', description: null, evidence: null }],
          growthAreas: [{ title: "Faqat o'sish sarlavhasi", description: null, actionStep: null }],
        }}
      />,
    );
    expect(screen.getByText('Faqat sarlavha')).toBeInTheDocument();
    expect(screen.queryByText('Keyingi qadam:')).not.toBeInTheDocument();
    expect(container.querySelectorAll('p')).toHaveLength(2);
  });

  it("bo'sh obyekt bilan yiqilmaydi va bo'sh holat matnini ko'rsatadi", () => {
    render(<AiReportView sections={{}} />);
    expect(screen.getByText('AI hisobot hali mavjud emas.')).toBeInTheDocument();
  });

  it('null massivlar bilan yiqilmaydi', () => {
    render(
      <AiReportView
        sections={{
          summary: 'Qisqa xulosa',
          strengths: null,
          growthAreas: null,
          careerSuggestions: null,
          studentRecommendations: null,
          teacherNotes: null,
          parentNotes: null,
          attentionFlags: null,
        }}
      />,
    );
    expect(screen.getByText('Qisqa xulosa')).toBeInTheDocument();
  });

  it("attentionFlags bo'sh bo'lsa alohida karta ko'rinmaydi", () => {
    render(<AiReportView sections={{ summary: 'Xulosa', attentionFlags: [] }} />);
    expect(screen.queryByText("E'tibor talab qiladigan holatlar")).not.toBeInTheDocument();
  });

  it('dangerouslySetInnerHTML ishlatmasdan matnni oddiy matn sifatida chiqaradi', () => {
    render(<AiReportView sections={{ summary: '<script>alert(1)</script>' }} />);
    expect(screen.getByText('<script>alert(1)</script>')).toBeInTheDocument();
    expect(document.querySelector('script')).toBeNull();
  });
});
