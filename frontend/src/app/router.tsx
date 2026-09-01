import { lazy, Suspense, type ComponentType, type ReactElement } from 'react';
import { createBrowserRouter, RouterProvider } from 'react-router';
import { PublicLayout } from '@/layouts/PublicLayout';
import { AdminLayout } from '@/layouts/AdminLayout';
import { ProtectedRoute } from '@/features/auth/ProtectedRoute';
import { ROUTE_PATTERNS } from '@/shared/config/routes';
import { RouteFallback } from './RouteFallback';
import NotFoundPage from './NotFoundPage';

// Ommaviy oqim (docs/10, 3-bo'lim)
const LandingPage = lazy(() => import('@/features/public-assessment/pages/LandingPage'));
const RegistrationPage = lazy(() => import('@/features/public-assessment/pages/RegistrationPage'));
const TestPage = lazy(() => import('@/features/public-assessment/pages/TestPage'));
const TestCompletePage = lazy(() => import('@/features/public-assessment/pages/TestCompletePage'));
const FinishPage = lazy(() => import('@/features/public-assessment/pages/FinishPage'));
const StudentResultPage = lazy(
  () => import('@/features/public-assessment/pages/StudentResultPage'),
);

// Admin
const LoginPage = lazy(() => import('@/features/auth/pages/LoginPage'));
const DashboardPage = lazy(() => import('@/features/dashboard/pages/DashboardPage'));
const SchoolsPage = lazy(() => import('@/features/schools/pages/SchoolsPage'));
const StudentsPage = lazy(() => import('@/features/students/pages/StudentsPage'));
const StudentProfilePage = lazy(() => import('@/features/students/pages/StudentProfilePage'));
const AssessmentsPage = lazy(() => import('@/features/assessments/pages/AssessmentsPage'));
const AssessmentDetailPage = lazy(
  () => import('@/features/assessments/pages/AssessmentDetailPage'),
);
const CatalogPage = lazy(() => import('@/features/catalog/pages/CatalogPage'));
const AiProvidersPage = lazy(() => import('@/features/ai-settings/pages/AiProvidersPage'));
const AuditLogPage = lazy(() => import('@/features/audit/pages/AuditLogPage'));
const SettingsPage = lazy(() => import('@/features/settings/pages/SettingsPage'));

/** `React.lazy` komponentini `<Suspense>` bilan o'raydi — docs/10, 7-bo'lim (route bo'yicha `lazy()`). */
function withSuspense(Component: ComponentType): ReactElement {
  return (
    <Suspense fallback={<RouteFallback />}>
      <Component />
    </Suspense>
  );
}

const router = createBrowserRouter([
  {
    element: <PublicLayout />,
    children: [
      { path: ROUTE_PATTERNS.public.landing, element: withSuspense(LandingPage) },
      { path: ROUTE_PATTERNS.public.register, element: withSuspense(RegistrationPage) },
      { path: ROUTE_PATTERNS.public.test, element: withSuspense(TestPage) },
      { path: ROUTE_PATTERNS.public.testDone, element: withSuspense(TestCompletePage) },
      { path: ROUTE_PATTERNS.public.finish, element: withSuspense(FinishPage) },
      { path: ROUTE_PATTERNS.public.result, element: withSuspense(StudentResultPage) },
    ],
  },
  {
    path: ROUTE_PATTERNS.admin.login,
    element: withSuspense(LoginPage),
  },
  {
    element: (
      <ProtectedRoute>
        <AdminLayout />
      </ProtectedRoute>
    ),
    children: [
      { path: ROUTE_PATTERNS.admin.dashboard, element: withSuspense(DashboardPage) },
      { path: ROUTE_PATTERNS.admin.schools, element: withSuspense(SchoolsPage) },
      { path: ROUTE_PATTERNS.admin.students, element: withSuspense(StudentsPage) },
      { path: ROUTE_PATTERNS.admin.studentProfile, element: withSuspense(StudentProfilePage) },
      { path: ROUTE_PATTERNS.admin.assessments, element: withSuspense(AssessmentsPage) },
      { path: ROUTE_PATTERNS.admin.assessmentDetail, element: withSuspense(AssessmentDetailPage) },
      { path: ROUTE_PATTERNS.admin.catalog, element: withSuspense(CatalogPage) },
      { path: ROUTE_PATTERNS.admin.ai, element: withSuspense(AiProvidersPage) },
      { path: ROUTE_PATTERNS.admin.audit, element: withSuspense(AuditLogPage) },
      { path: ROUTE_PATTERNS.admin.settings, element: withSuspense(SettingsPage) },
    ],
  },
  { path: '*', element: <NotFoundPage /> },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
