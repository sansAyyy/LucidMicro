import type { PageResult } from '@/features/admin-users/types';

export interface RoleListQuery {
  keyword?: string;
  pageNumber: number;
  pageSize: number;
}

export interface Role {
  id: string;
  code: string;
  name: string;
  description: string | null;
  isSystem: boolean;
  isEnabled: boolean;
  createdAt: string;
  lastModifiedAt: string | null;
}

export interface RoleDetail extends Role {
  permissionIds: string[];
}

export interface CreateRoleRequest {
  code: string;
  name: string;
  description?: string;
  isEnabled: boolean;
}

export interface UpdateRoleRequest {
  name: string;
  description?: string;
  isEnabled: boolean;
}

export interface AssignRolePermissionsRequest {
  permissionIds: string[];
}

export type RolePageResult = PageResult<Role>;
