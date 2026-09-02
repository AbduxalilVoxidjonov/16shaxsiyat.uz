import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ReliabilityBadge } from './ReliabilityBadge';

describe('ReliabilityBadge', () => {
  it('Reliable holatini ballar bilan ko\'rsatadi', () => {
    render(<ReliabilityBadge flag="Reliable" score={82.5} />);
    expect(screen.getByText('Ishonchli')).toBeInTheDocument();
    expect(screen.getByText('83')).toBeInTheDocument();
  });

  it('Questionable holatini ko\'rsatadi', () => {
    render(<ReliabilityBadge flag="Questionable" score={55} />);
    expect(screen.getByText('Shubhali')).toBeInTheDocument();
  });

  it('Unreliable holatini ko\'rsatadi va tooltip tushuntirishi bor', () => {
    render(<ReliabilityBadge flag="Unreliable" score={20} />);
    const badge = screen.getByText('Ishonchsiz').closest('span[title]');
    expect(badge).not.toBeNull();
    expect(badge).toHaveAttribute(
      'title',
      expect.stringContaining('ishonchsiz'),
    );
  });

  it('score berilmasa raqam ko\'rsatilmaydi, yiqilmaydi', () => {
    render(<ReliabilityBadge flag="Reliable" />);
    expect(screen.getByText('Ishonchli')).toBeInTheDocument();
  });
});
