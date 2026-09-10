import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import type { CatalogQuestionItem, CatalogSection } from '../model/types';
import { BranchingPreview } from './BranchingPreview';

function section(overrides: Partial<CatalogSection> = {}): CatalogSection {
  return {
    id: 's-1',
    testDefinitionId: 't-1',
    code: 'S1',
    titleUz: "Asosiy ma'lumotlar",
    descriptionUz: null,
    displayOrder: 1,
    visibility: null,
    ...overrides,
  };
}

function question(overrides: Partial<CatalogQuestionItem> = {}): CatalogQuestionItem {
  return {
    id: 'q-1',
    code: 'Q1_6',
    order: 6,
    textUz: "Hozirda qo'shimcha o'quv kurslariga qatnashasizmi?",
    textRu: null,
    textEn: null,
    type: 'SingleChoice',
    scale: 'SURVEY',
    direction: 1,
    weight: 1,
    isRequired: true,
    isActive: true,
    isSystem: false,
    sectionId: 's-1',
    visibility: null,
    placeholder: null,
    inputPattern: null,
    maxLength: null,
    minSelections: null,
    maxSelections: null,
    options: [
      { textUz: "Ha, Intellect o'quv markazida o'qiyman", value: 1, displayOrder: 1 },
      { textUz: 'Yoq', value: 2, displayOrder: 2 },
    ],
    ...overrides,
  };
}

describe('BranchingPreview', () => {
  it("bo'lim yo'q bo'lsa bo'sh holat", () => {
    render(<BranchingPreview sections={[]} questions={[]} />);
    expect(screen.getByText(/Hali bo'lim yo'q/)).toBeInTheDocument();
  });

  it("shartsiz bo'lim 'har doim ko'rinadi' deb ko'rsatiladi", () => {
    render(<BranchingPreview sections={[section()]} questions={[]} />);
    expect(screen.getByText("Har doim ko'rinadi")).toBeInTheDocument();
  });

  it("shartli bo'lim jumla ko'rinishida chiqadi", () => {
    const s2a = section({
      id: 's-2a',
      code: 'S2A',
      titleUz: "Intellect o'quvchilari uchun",
      displayOrder: 2,
      visibility: {
        match: 'All',
        conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [1] }],
      },
    });
    render(<BranchingPreview sections={[section(), s2a]} questions={[question()]} />);

    expect(
      screen.getByText(/Q1_6 savoliga javob "Ha, Intellect o'quv markazida o'qiyman" bo'lsa/),
    ).toBeInTheDocument();
  });

  it('savollar soni bo‘lim bo‘yicha to‘g‘ri hisoblanadi', () => {
    render(
      <BranchingPreview
        sections={[section()]}
        questions={[question({ id: 'q-1' }), question({ id: 'q-2', code: 'Q1_7' })]}
      />,
    );
    expect(screen.getByText('2 ta savol')).toBeInTheDocument();
  });
});
