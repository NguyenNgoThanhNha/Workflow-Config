import { lazy } from 'react';
import { createBrowserRouter, type RouteObject } from 'react-router-dom';
import { AppLayout } from '@/layouts/app-layout';
import { AuthLayout } from '@/layouts/auth-layout';
import { ForgotPasswordPage, LoginPage, RegisterPage, ResetPasswordPage } from '@/features/auth';
import { WorkflowCreatePage, WorkflowDesignerPage, WorkflowEditPage, WorkflowListPage } from '@/features/workflows';
import { PERMISSIONS } from '@/lib/permissions';
import { HomeRedirect, NotFound, PermissionRoute, ProtectedRoute, PublicOnlyRoute } from './route-guards';

// Cài đặt / log API được tách chunk riêng (sơ đồ workflow tự lazy trong feature).
const KanbanListPage = lazy(() => import('@/features/kanbans').then((m) => ({ default: m.KanbanListPage })));
const KanbanCreatePage = lazy(() => import('@/features/kanbans').then((m) => ({ default: m.KanbanCreatePage })));
const KanbanEditPage = lazy(() => import('@/features/kanbans').then((m) => ({ default: m.KanbanEditPage })));
const KanbanBoardPage = lazy(() => import('@/features/kanbans').then((m) => ({ default: m.KanbanBoardPage })));
const SettingsPage = lazy(() => import('@/features/settings').then((m) => ({ default: m.SettingsPage })));
const ApiLogsPage = lazy(() => import('@/features/api-logs').then((m) => ({ default: m.ApiLogsPage })));

export const routes: RouteObject[] = [
  {
    element: <AuthLayout />,
    children: [
      {
        element: <PublicOnlyRoute />,
        children: [
          { path: '/login', element: <LoginPage /> },
          { path: '/register', element: <RegisterPage /> },
          { path: '/forgot-password', element: <ForgotPasswordPage /> },
        ],
      },
      { path: '/reset-password', element: <ResetPasswordPage /> },
    ],
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <HomeRedirect /> },
          {
            element: <PermissionRoute anyOf={PERMISSIONS.workflows} />,
            children: [
              { path: '/workflows', element: <WorkflowListPage /> },
              { path: '/workflows/:id', element: <WorkflowEditPage /> },
              { path: '/workflows/:id/designer', element: <WorkflowDesignerPage /> },
            ],
          },
          {
            element: <PermissionRoute anyOf={PERMISSIONS.createWorkflow} />,
            children: [{ path: '/workflows/new', element: <WorkflowCreatePage /> }],
          },
          {
            element: <PermissionRoute anyOf={PERMISSIONS.kanbans} />,
            children: [
              { path: '/kanbans', element: <KanbanListPage /> },
              { path: '/kanbans/:id', element: <KanbanEditPage /> },
              { path: '/kanbans/:id/board', element: <KanbanBoardPage /> },
            ],
          },
          {
            element: <PermissionRoute anyOf={PERMISSIONS.createKanban} />,
            children: [{ path: '/kanbans/new', element: <KanbanCreatePage /> }],
          },
          {
            element: <PermissionRoute anyOf={PERMISSIONS.settings} />,
            children: [{ path: '/settings', element: <SettingsPage /> }],
          },
          {
            element: <PermissionRoute anyOf={PERMISSIONS.apiLogs} />,
            children: [{ path: '/api-logs', element: <ApiLogsPage /> }],
          },
          { path: '*', element: <NotFound /> },
        ],
      },
    ],
  },
];

export const router = createBrowserRouter(routes, {
  future: {
    v7_relativeSplatPath: true,
    v7_fetcherPersist: true,
    v7_normalizeFormMethod: true,
    v7_partialHydration: true,
    v7_skipActionErrorRevalidation: true,
  },
});
