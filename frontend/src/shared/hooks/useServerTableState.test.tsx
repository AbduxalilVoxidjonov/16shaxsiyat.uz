import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useSearchParams } from 'react-router';
import { useServerTableState } from './useServerTableState';

function TestConsumer() {
  const { page, pageSize, sort, setPage, setSort } = useServerTableState({ defaultPageSize: 20 });
  const [searchParams] = useSearchParams();

  return (
    <div>
      <p data-testid="page">{page}</p>
      <p data-testid="pageSize">{pageSize}</p>
      <p data-testid="sort">{sort ? `${sort.columnId}:${sort.direction}` : 'none'}</p>
      <p data-testid="raw-url">{searchParams.toString()}</p>
      <button type="button" onClick={() => setPage(3)}>
        go-to-page-3
      </button>
      <button type="button" onClick={() => setSort({ columnId: 'name', direction: 'asc' })}>
        sort-name-asc
      </button>
      <button type="button" onClick={() => setSort({ columnId: 'name', direction: 'desc' })}>
        sort-name-desc
      </button>
    </div>
  );
}

function renderWithRouter(initialEntry = '/students') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <TestConsumer />
    </MemoryRouter>,
  );
}

describe('useServerTableState', () => {
  it("URL da parametr bo'lmasa standart qiymatlarni qaytaradi", () => {
    renderWithRouter();

    expect(screen.getByTestId('page')).toHaveTextContent('1');
    expect(screen.getByTestId('pageSize')).toHaveTextContent('20');
    expect(screen.getByTestId('sort')).toHaveTextContent('none');
  });

  it("mavjud URL query parametrlarini o'qiydi (orqaga tugmasi ishlashi uchun)", () => {
    renderWithRouter('/students?page=4&pageSize=50&sort=grade&dir=desc');

    expect(screen.getByTestId('page')).toHaveTextContent('4');
    expect(screen.getByTestId('pageSize')).toHaveTextContent('50');
    expect(screen.getByTestId('sort')).toHaveTextContent('grade:desc');
  });

  it("URL'dagi haddan tashqari katta pageSize 100 ga qisiladi (docs/07 4-bo'lim: max 100)", () => {
    renderWithRouter('/students?pageSize=5000');

    expect(screen.getByTestId('pageSize')).toHaveTextContent('100');
  });

  it("URL'dagi pageSize=0 standart qiymatga tushadi", () => {
    renderWithRouter('/students?pageSize=0');
    expect(screen.getByTestId('pageSize')).toHaveTextContent('20');
  });

  it("URL'dagi manfiy pageSize standart qiymatga tushadi", () => {
    renderWithRouter('/students?pageSize=-5');
    expect(screen.getByTestId('pageSize')).toHaveTextContent('20');
  });

  it('setPage chaqirilganda URL yangilanadi', async () => {
    const user = userEvent.setup();
    renderWithRouter();

    await user.click(screen.getByRole('button', { name: 'go-to-page-3' }));

    expect(screen.getByTestId('page')).toHaveTextContent('3');
    expect(screen.getByTestId('raw-url')).toHaveTextContent('page=3');
  });

  it("setSort chaqirilganda URL'ga sort/dir yoziladi va sahifa 1ga qaytadi", async () => {
    const user = userEvent.setup();
    renderWithRouter('/students?page=5');

    await user.click(screen.getByRole('button', { name: 'sort-name-asc' }));

    expect(screen.getByTestId('sort')).toHaveTextContent('name:asc');
    expect(screen.getByTestId('page')).toHaveTextContent('1');
  });

  it('boshqa mavjud query parametrlari (masalan filtr) saqlanib qoladi', async () => {
    const user = userEvent.setup();
    renderWithRouter('/students?school=12&page=2');

    await user.click(screen.getByRole('button', { name: 'sort-name-desc' }));

    const url = screen.getByTestId('raw-url').textContent ?? '';
    expect(url).toContain('school=12');
    expect(url).toContain('sort=name');
    expect(url).toContain('dir=desc');
  });
});
