import { Bell, House, User, UserFilled } from '@element-plus/icons-vue';
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';

import AdminLayout from '@/layouts/AdminLayout.vue';
import AuthLayout from '@/layouts/AuthLayout.vue';
import { AdminPermissions } from '@/shared/auth/permissions';
import { useAuthStore } from '@/shared/auth/useAuthStore';
import './navigation';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    component: AuthLayout,
    children: [
      {
        path: '',
        name: 'login',
        component: () => import('@/pages/LoginPage.vue'),
        meta: { public: true },
      },
    ],
  },
  {
    path: '/',
    component: AdminLayout,
    children: [
      {
        path: '',
        redirect: '/dashboard',
      },
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('@/pages/DashboardPage.vue'),
        meta: {
          icon: House,
          menu: {
            order: 0,
          },
          title: '概览',
        },
      },
      {
        path: 'admin-users',
        redirect: '/identity/admin-users',
      },
      {
        path: 'identity/admin-users',
        name: 'identity-admin-users',
        component: () => import('@/features/admin-users/AdminUsersPage.vue'),
        meta: {
          icon: User,
          menu: {
            group: 'identity',
            groupOrder: 10,
            groupTitle: 'Identity',
            order: 10,
          },
          requiredPermissions: [AdminPermissions.AdminUsersRead],
          title: '管理员',
        },
      },
      {
        path: 'identity/roles',
        name: 'identity-roles',
        component: () => import('@/features/roles/RolesPage.vue'),
        meta: {
          icon: UserFilled,
          menu: {
            group: 'identity',
            groupOrder: 10,
            groupTitle: 'Identity',
            order: 20,
          },
          requiredPermissions: [AdminPermissions.RolesRead],
          title: '角色',
        },
      },
      {
        path: 'notification/notifications',
        name: 'notification-notifications',
        component: () => import('@/features/notifications/NotificationsPage.vue'),
        meta: {
          icon: Bell,
          menu: {
            group: 'notification',
            groupOrder: 20,
            groupTitle: 'Notification',
            order: 10,
          },
          requiredPermissions: [AdminPermissions.NotificationsRead],
          title: '通知',
        },
      },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/pages/NotFoundPage.vue'),
    meta: { public: true },
  },
];

export const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach(async (to) => {
  const auth = useAuthStore();

  if (to.meta.public) {
    return true;
  }

  if (!(await auth.ensureSession())) {
    return {
      name: 'login',
      query: {
        redirect: to.fullPath,
      },
    };
  }

  if (auth.hasPermissions(to.meta.requiredPermissions)) {
    return true;
  }

  return { name: 'dashboard' };
});
