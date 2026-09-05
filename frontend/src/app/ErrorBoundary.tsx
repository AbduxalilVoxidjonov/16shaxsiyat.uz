import { Component, type ErrorInfo, type ReactNode } from 'react';
import { GirihStar, PatternBackdrop } from '@/shared/ui/brand';
import i18n from '@/shared/lib/i18n';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
}

/**
 * Ildiz xato chegarasi (docs/10, 7-bo'lim: "Har sahifa `ErrorBoundary` ichida").
 * React error boundary faqat klass komponent bo'lishi mumkin.
 *
 * Ko'rinishi ataylab VAZMIN — qizil "xavf" paneli emas: bu ekranni ko'p hollarda test
 * yechayotgan o'quvchi ko'radi va uni qo'rqitmaslik kerak. Xatoning texnik tafsiloti
 * (stack) hech qachon ekranga chiqmaydi, faqat konsolga yoziladi.
 */
export class ErrorBoundary extends Component<Props, State> {
  override state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  override componentDidCatch(error: Error, info: ErrorInfo): void {
    // Ishlab chiqarishda Sentry ulanguncha vaqtinchalik log (VITE_SENTRY_DSN — docs/10, 8-bo'lim).
    console.error('[ErrorBoundary]', error, info.componentStack);
  }

  handleRetry = (): void => {
    this.setState({ hasError: false });
  };

  override render(): ReactNode {
    if (this.state.hasError) {
      return (
        <div className="relative flex min-h-dvh items-center justify-center overflow-hidden bg-paper px-5 py-16">
          <PatternBackdrop className="opacity-40" />

          <div
            role="alert"
            className="card relative flex max-w-md flex-col items-center gap-4 px-6 py-12 text-center animate-fade-up"
          >
            <span className="relative grid size-24 place-items-center">
              <GirihStar className="absolute inset-0 text-line-strong" strokeWidth={1.5} />
              <GirihStar
                className="absolute inset-[24%] text-ink-faint"
                strokeWidth={2}
                withCircle={false}
              />
            </span>

            <h1 className="font-display text-xl font-extrabold tracking-tight text-ink balance sm:text-2xl">
              {i18n.t('error.boundaryTitle')}
            </h1>
            <p className="max-w-sm text-[15px] leading-relaxed text-ink-soft">
              {i18n.t('error.boundaryDescription')}
            </p>

            <button
              type="button"
              className="btn btn-md btn-primary mt-2"
              onClick={this.handleRetry}
            >
              {i18n.t('common.retry')}
            </button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
