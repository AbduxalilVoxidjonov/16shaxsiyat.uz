import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { SectionIntro } from './SectionIntro';

describe('SectionIntro', () => {
  it("sarlavhani h2 sifatida ko'rsatadi", () => {
    render(<SectionIntro title="Asosiy ma'lumotlar" />);
    expect(screen.getByRole('heading', { level: 2, name: "Asosiy ma'lumotlar" })).toBeInTheDocument();
  });

  it("tavsif berilsa ko'rsatadi", () => {
    render(<SectionIntro title="Bo'lim" description="Bu bo'limni hamma to'ldiradi." />);
    expect(screen.getByText("Bu bo'limni hamma to'ldiradi.")).toBeInTheDocument();
  });

  it("tavsif berilmasa (null) hech narsa qo'shimcha ko'rsatilmaydi", () => {
    render(<SectionIntro title="Bo'lim" description={null} />);
    expect(screen.queryByText(/./, { selector: 'p' })).not.toBeInTheDocument();
  });
});
