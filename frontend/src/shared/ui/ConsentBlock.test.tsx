import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ConsentBlock } from './ConsentBlock';

const LABEL = "Ma'lumotlarim ta'lim maqsadida ishlatilishiga roziman";

describe('ConsentBlock', () => {
  it("matn checkbox yorlig'i bilan bir xil bo'lsa faqat bir marta ko'rinadi", () => {
    render(<ConsentBlock consentText={`${LABEL}.`} checked={false} onChange={() => {}} />);

    expect(screen.getAllByText(new RegExp(LABEL))).toHaveLength(1);
    expect(screen.getByRole('checkbox', { name: LABEL })).toBeInTheDocument();
  });

  it("boshqacha matn checkbox ustida ko'rsatiladi", () => {
    const text = "Test javoblarim ta'limiy maqsadda qayta ishlanishiga roziman.";
    render(<ConsentBlock consentText={text} checked={false} onChange={() => {}} />);

    expect(screen.getByText(text)).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: LABEL })).toBeInTheDocument();
  });
});
