import { AppProviders } from '@/app/providers';
import { AppRouter } from '@/app/router';

/** Ilova ildizi — barcha provayderlar (`app/providers.tsx`) + router (`app/router.tsx`). */
export function App() {
  return (
    <AppProviders>
      <AppRouter />
    </AppProviders>
  );
}

export default App;
