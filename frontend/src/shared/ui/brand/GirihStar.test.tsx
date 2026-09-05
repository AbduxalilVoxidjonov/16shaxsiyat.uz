import { describe, expect, it } from 'vitest';
import { render } from '@testing-library/react';
import { GirihStar } from './GirihStar';

describe('GirihStar', () => {
  it('dekorativ SVG sifatida render qilinadi (aria-hidden)', () => {
    const { container } = render(<GirihStar className="text-firuza-500" />);

    const svg = container.querySelector('svg');
    expect(svg).not.toBeNull();
    expect(svg).toHaveAttribute('aria-hidden', 'true');
    expect(svg).toHaveClass('text-firuza-500');
  });

  it("standart holatda markaziy doira chiziladi, withCircle={false} bo'lsa chizilmaydi", () => {
    const { container: withCircle } = render(<GirihStar />);
    expect(withCircle.querySelectorAll('circle')).toHaveLength(1);

    const { container: withoutCircle } = render(<GirihStar withCircle={false} />);
    expect(withoutCircle.querySelectorAll('circle')).toHaveLength(0);
  });

  it('strokeWidth barcha chiziqlarga uzatiladi', () => {
    const { container } = render(<GirihStar strokeWidth={1.5} />);

    const shapes = container.querySelectorAll('rect, circle');
    expect(shapes).toHaveLength(3);
    shapes.forEach((shape) => {
      expect(shape).toHaveAttribute('stroke-width', '1.5');
    });
  });
});
