import { Spinner } from '@/shared/ui/Spinner';

/** `lazy()` bilan yuklanayotgan route'lar uchun `<Suspense>` fallback'i. */
export function RouteFallback() {
  return (
    <div className="flex min-h-[50vh] items-center justify-center">
      <Spinner size={28} />
    </div>
  );
}
