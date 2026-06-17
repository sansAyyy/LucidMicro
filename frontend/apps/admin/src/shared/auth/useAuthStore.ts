import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { HttpError, http } from '@/shared/api/http';
import type { AdminPermission } from './permissions';
import { clearTokens, getStoredTokens, saveTokens } from './token';

interface LoginRequest {
  loginName: string;
  password: string;
}

interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface CurrentAdminUser {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  phoneNumber: string | null;
  isActive: boolean;
  lastLoginAt: string | null;
  permissions: string[];
}

export const useAuthStore = defineStore('auth', () => {
  const storedTokens = getStoredTokens();
  const accessToken = ref(storedTokens?.accessToken ?? null);
  const refreshToken = ref(storedTokens?.refreshToken ?? null);
  const currentUser = ref<CurrentAdminUser | null>(null);
  const hasLoadedCurrentUser = ref(false);
  let currentUserRequest: Promise<CurrentAdminUser | null> | null = null;
  let sessionRequest: Promise<boolean> | null = null;

  const isAuthenticated = computed(() => Boolean(accessToken.value));
  const displayName = computed(() => currentUser.value?.displayName ?? currentUser.value?.userName ?? 'Admin');
  const permissions = computed(() => new Set(currentUser.value?.permissions ?? []));

  async function login(request: LoginRequest) {
    const response = await http<LoginResponse>('/api/identity/admin-auth/login', {
      method: 'POST',
      body: request,
    });

    saveTokens(response);
    accessToken.value = response.accessToken;
    refreshToken.value = response.refreshToken;
    hasLoadedCurrentUser.value = false;
    await loadCurrentUser();
  }

  async function refreshSession() {
    if (!refreshToken.value) {
      return false;
    }

    try {
      const response = await http<LoginResponse>('/api/identity/admin-auth/refresh', {
        method: 'POST',
        body: {
          refreshToken: refreshToken.value,
        },
      });

      saveTokens(response);
      accessToken.value = response.accessToken;
      refreshToken.value = response.refreshToken;
      return true;
    } catch {
      clearSession();
      return false;
    }
  }

  async function loadCurrentUser(): Promise<CurrentAdminUser | null> {
    if (!accessToken.value) {
      return null;
    }

    currentUserRequest ??= (async (): Promise<CurrentAdminUser | null> => {
      try {
        currentUser.value = await http<CurrentAdminUser>('/api/identity/admin-auth/me', {
          token: accessToken.value,
        });
        hasLoadedCurrentUser.value = true;
        return currentUser.value;
      } catch (error) {
        if (error instanceof HttpError && error.status === 401 && (await refreshSession())) {
          currentUserRequest = null;
          return loadCurrentUser();
        }

        clearSession();
        throw error;
      } finally {
        currentUserRequest = null;
      }
    })();

    return currentUserRequest;
  }

  async function ensureSession() {
    if (!accessToken.value) {
      return false;
    }

    if (hasLoadedCurrentUser.value) {
      return true;
    }

    sessionRequest ??= (async () => {
      try {
        await loadCurrentUser();
        return true;
      } catch {
        return false;
      } finally {
        sessionRequest = null;
      }
    })();

    return sessionRequest;
  }

  function clearSession() {
    accessToken.value = null;
    refreshToken.value = null;
    currentUser.value = null;
    hasLoadedCurrentUser.value = false;
    currentUserRequest = null;
    sessionRequest = null;
    clearTokens();
  }

  function hasPermission(permission: AdminPermission | string) {
    return permissions.value.has(permission);
  }

  function hasPermissions(requiredPermissions?: ReadonlyArray<AdminPermission | string>) {
    if (!requiredPermissions || requiredPermissions.length === 0) {
      return true;
    }

    return requiredPermissions.every(hasPermission);
  }

  return {
    accessToken,
    refreshToken,
    currentUser,
    displayName,
    isAuthenticated,
    hasPermission,
    hasPermissions,
    login,
    loadCurrentUser,
    ensureSession,
    clearSession,
  };
});
