import { http } from '@/shared/api/http';
import type {
  AdminUser,
  AdminUserListQuery,
  AssignAdminUserRolesRequest,
  CreateAdminUserRequest,
  PageResult,
  ResetAdminUserPasswordRequest,
  UpdateAdminUserRequest,
} from './types';

export function getAdminUsers(query: AdminUserListQuery) {
  return http<PageResult<AdminUser>>('/api/identity/admin-users', {
    auth: true,
    query: {
      keyword: query.keyword,
      pageNumber: query.pageNumber,
      pageSize: query.pageSize,
    },
  });
}

export function getAdminUserById(id: string) {
  return http<AdminUser>(`/api/identity/admin-users/${id}`, {
    auth: true,
  });
}

export function createAdminUser(request: CreateAdminUserRequest) {
  return http<AdminUser>('/api/identity/admin-users', {
    auth: true,
    method: 'POST',
    body: request,
  });
}

export function updateAdminUser(id: string, request: UpdateAdminUserRequest) {
  return http<void>(`/api/identity/admin-users/${id}`, {
    auth: true,
    method: 'PUT',
    body: request,
  });
}

export function activateAdminUser(id: string) {
  return http<void>(`/api/identity/admin-users/${id}/activate`, {
    auth: true,
    method: 'PUT',
  });
}

export function deactivateAdminUser(id: string) {
  return http<void>(`/api/identity/admin-users/${id}/deactivate`, {
    auth: true,
    method: 'PUT',
  });
}

export function deleteAdminUser(id: string) {
  return http<void>(`/api/identity/admin-users/${id}`, {
    auth: true,
    method: 'DELETE',
  });
}

export function resetAdminUserPassword(id: string, request: ResetAdminUserPasswordRequest) {
  return http<void>(`/api/identity/admin-users/${id}/password`, {
    auth: true,
    method: 'PUT',
    body: request,
  });
}

export function assignAdminUserRoles(id: string, request: AssignAdminUserRolesRequest) {
  return http<void>(`/api/identity/admin-users/${id}/roles`, {
    auth: true,
    method: 'PUT',
    body: request,
  });
}
