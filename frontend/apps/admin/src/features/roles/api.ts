import { http } from '@/shared/api/http';
import type {
  AssignRolePermissionsRequest,
  CreateRoleRequest,
  Role,
  RoleDetail,
  RoleListQuery,
  RolePageResult,
  UpdateRoleRequest,
} from './types';

export function getRoles(query: RoleListQuery) {
  return http<RolePageResult>('/api/identity/roles', {
    auth: true,
    query: {
      keyword: query.keyword,
      pageNumber: query.pageNumber,
      pageSize: query.pageSize,
    },
  });
}

export function getRoleById(id: string) {
  return http<RoleDetail>(`/api/identity/roles/${id}`, {
    auth: true,
  });
}

export function createRole(request: CreateRoleRequest) {
  return http<Role>('/api/identity/roles', {
    auth: true,
    method: 'POST',
    body: request,
  });
}

export function updateRole(id: string, request: UpdateRoleRequest) {
  return http<void>(`/api/identity/roles/${id}`, {
    auth: true,
    method: 'PUT',
    body: request,
  });
}

export function deleteRole(id: string) {
  return http<void>(`/api/identity/roles/${id}`, {
    auth: true,
    method: 'DELETE',
  });
}

export function assignRolePermissions(id: string, request: AssignRolePermissionsRequest) {
  return http<void>(`/api/identity/roles/${id}/permissions`, {
    auth: true,
    method: 'PUT',
    body: request,
  });
}
