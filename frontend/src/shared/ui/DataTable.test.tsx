import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { DataTable, type DataTableColumn } from './DataTable';

/** Demo ma'lumot — "So'nggi sessiyalar" jadvaliga o'xshash shakl (docs/11, A-2). */
interface DemoSession {
  id: string;
  studentName: string;
  status: 'Analyzed' | 'Analyzing' | 'AnalysisFailed';
}

const DEMO_ROWS: DemoSession[] = [
  { id: '1', studentName: 'Aliyev Sardor', status: 'Analyzed' },
  { id: '2', studentName: 'Karimova Nilufar', status: 'Analyzing' },
  { id: '3', studentName: "Yo'ldoshev Jasur", status: 'AnalysisFailed' },
];

const COLUMNS: Array<DataTableColumn<DemoSession>> = [
  { id: 'studentName', header: 'F.I.Sh.', cell: (row) => row.studentName, sortable: true },
  { id: 'status', header: 'Holat', cell: (row) => row.status },
];

function renderTable(overrides: Partial<React.ComponentProps<typeof DataTable<DemoSession>>> = {}) {
  const onSortChange = vi.fn();
  const onPageChange = vi.fn();
  const onRowClick = vi.fn();

  render(
    <DataTable
      columns={COLUMNS}
      rows={DEMO_ROWS}
      rowKey={(row) => row.id}
      total={45}
      page={2}
      pageSize={20}
      sort={{ columnId: 'studentName', direction: 'asc' }}
      onSortChange={onSortChange}
      onPageChange={onPageChange}
      onRowClick={onRowClick}
      ariaLabel="Demo jadval"
      {...overrides}
    />,
  );

  return { onSortChange, onPageChange, onRowClick };
}

describe('DataTable', () => {
  it("demo ma'lumotni jadval sifatida ko'rsatadi", () => {
    renderTable();

    expect(screen.getByRole('table', { name: 'Demo jadval' })).toBeInTheDocument();
    expect(screen.getByText('Aliyev Sardor')).toBeInTheDocument();
    expect(screen.getByText('Karimova Nilufar')).toBeInTheDocument();
    expect(screen.getByText("Yo'ldoshev Jasur")).toBeInTheDocument();
  });

  it("saralanadigan ustun sarlavhasiga bosilganda onSortChange to'g'ri yo'nalish bilan chaqiriladi", async () => {
    const user = userEvent.setup();
    const { onSortChange } = renderTable();

    // Joriy saralash `studentName asc` — bosilganda `desc`ga almashishi kerak.
    await user.click(screen.getByRole('button', { name: /F\.I\.Sh\./ }));

    expect(onSortChange).toHaveBeenCalledWith({ columnId: 'studentName', direction: 'desc' });
  });

  it("saralanmagan ustunga bosilganda yo'nalish `asc`dan boshlanadi", async () => {
    const user = userEvent.setup();
    const { onSortChange } = renderTable({ sort: null });

    await user.click(screen.getByRole('button', { name: /F\.I\.Sh\./ }));

    expect(onSortChange).toHaveBeenCalledWith({ columnId: 'studentName', direction: 'asc' });
  });

  it("`aria-sort` joriy saralangan ustunda to'g'ri qiymatga ega (WAI-ARIA jadval naqshi)", () => {
    renderTable({ sort: { columnId: 'studentName', direction: 'asc' } });

    expect(screen.getByRole('columnheader', { name: /F\.I\.Sh\./ })).toHaveAttribute(
      'aria-sort',
      'ascending',
    );
  });

  it("`aria-sort` yo'nalish `desc`ga o'zgarganda ham sinxron yangilanadi", () => {
    renderTable({ sort: { columnId: 'studentName', direction: 'desc' } });

    expect(screen.getByRole('columnheader', { name: /F\.I\.Sh\./ })).toHaveAttribute(
      'aria-sort',
      'descending',
    );
  });

  it('saralanadigan, lekin hozircha saralanmagan ustunda `aria-sort="none"` bo\'ladi', () => {
    renderTable({ sort: { columnId: 'status', direction: 'asc' } });

    // `status` ustuni `sortable` emas — `aria-sort` umuman qo'yilmaydi (pastdagi testga qarang);
    // `studentName` esa saralanadigan, lekin joriy saralash boshqa ustunda — demak `none`.
    expect(screen.getByRole('columnheader', { name: /F\.I\.Sh\./ })).toHaveAttribute(
      'aria-sort',
      'none',
    );
  });

  it("saralanmaydigan ustunda `aria-sort` atributi umuman yo'q", () => {
    renderTable({ sort: null });

    expect(screen.getByRole('columnheader', { name: 'Holat' })).not.toHaveAttribute('aria-sort');
  });

  it("sahifalash tugmalari bosilganda onPageChange chaqiriladi va chegaralarda o'chiriladi", async () => {
    const user = userEvent.setup();
    const { onPageChange } = renderTable({ page: 2, pageSize: 20, total: 45 });

    // 45 ta yozuv, 20 tadan — 3 sahifa; joriy sahifa 2 — ikkala tugma ham faol.
    const prevButton = screen.getByRole('button', { name: 'Oldingi sahifa' });
    const nextButton = screen.getByRole('button', { name: 'Keyingi sahifa' });
    expect(prevButton).toBeEnabled();
    expect(nextButton).toBeEnabled();

    await user.click(nextButton);
    expect(onPageChange).toHaveBeenCalledWith(3);

    await user.click(prevButton);
    expect(onPageChange).toHaveBeenCalledWith(1);

    expect(screen.getByText('2 / 3')).toBeInTheDocument();
  });

  it('oxirgi sahifada "Keyingi" tugmasi o\'chiriladi', () => {
    renderTable({ page: 3, pageSize: 20, total: 45 });
    expect(screen.getByRole('button', { name: 'Keyingi sahifa' })).toBeDisabled();
  });

  it('birinchi sahifada "Oldingi" tugmasi o\'chiriladi', () => {
    renderTable({ page: 1, pageSize: 20, total: 45 });
    expect(screen.getByRole('button', { name: 'Oldingi sahifa' })).toBeDisabled();
  });

  it('qatorga bosilganda onRowClick tegishli yozuv bilan chaqiriladi', async () => {
    const user = userEvent.setup();
    const { onRowClick } = renderTable();

    await user.click(screen.getByRole('button', { name: /Aliyev Sardor/ }));

    expect(onRowClick).toHaveBeenCalledWith(DEMO_ROWS[0]);
  });

  it('klaviatura bilan (Enter) qatorni faollashtirish mumkin', async () => {
    const user = userEvent.setup();
    const { onRowClick } = renderTable();

    const row = screen.getByRole('button', { name: /Karimova Nilufar/ });
    row.focus();
    await user.keyboard('{Enter}');

    expect(onRowClick).toHaveBeenCalledWith(DEMO_ROWS[1]);
  });

  it("yuklanish holatida skelet qatorlar ko'rsatiladi, ma'lumot yo'q", () => {
    renderTable({ isLoading: true, rows: [] });
    expect(screen.queryByText('Aliyev Sardor')).not.toBeInTheDocument();
  });

  it("bo'sh massivda EmptyState ko'rsatiladi", () => {
    renderTable({ rows: [], total: 0 });
    expect(screen.getByText("Bu yerda hali hech narsa yo'q")).toBeInTheDocument();
  });

  it("xato holatida ErrorState va qayta urinish tugmasi ko'rsatiladi, jadval chizilmaydi", async () => {
    const onRetry = vi.fn();
    const user = userEvent.setup();
    render(
      <DataTable
        columns={COLUMNS}
        rows={[]}
        rowKey={(row) => row.id}
        total={0}
        page={1}
        pageSize={20}
        sort={null}
        onSortChange={vi.fn()}
        onPageChange={vi.fn()}
        isError
        onRetry={onRetry}
        ariaLabel="Demo jadval"
      />,
    );

    expect(screen.queryByRole('table')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Qayta urinish' }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });
});
