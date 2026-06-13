import type { AdminPermission } from './permissions';
import { useAuthStore } from './useAuthStore';

type PermissionInput = AdminPermission | string;

export function usePermissions() {
  const auth = useAuthStore();

  function can(permission: PermissionInput) {
    return auth.hasPermission(permission);
  }

  function canAll(permissions?: ReadonlyArray<PermissionInput>) {
    return auth.hasPermissions(permissions);
  }

  function canAny(permissions: ReadonlyArray<PermissionInput>) {
    return permissions.some(can);
  }

  return {
    can,
    canAll,
    canAny,
  };
}
