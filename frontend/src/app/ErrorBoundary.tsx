import { Component, type ErrorInfo, type ReactNode } from 'react';
import { ErrorState } from '@/shared/ui/ErrorState';
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
        <div className="flex min-h-dvh items-center justify-center p-6">
          <ErrorState
            title={i18n.t('error.boundaryTitle')}
            description={i18n.t('error.boundaryDescription')}
            onRetry={this.handleRetry}
          />
        </div>
      );
    }
    return this.props.children;
  }
}
