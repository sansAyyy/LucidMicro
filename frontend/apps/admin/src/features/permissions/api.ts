import { http } from '@/shared/api/http';
import type { Permission } from './types';

export function getPermissions() {
  return http<Permission[]>('/api/identity/permissions', {
    auth: true,
  });
}
