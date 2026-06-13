export interface AdminUserListQuery {
  keyword?: string;
  pageNumber: number;
  pageSize: number;
}

export interface AdminUserRole {
  id: string;
  code: string;
  name: string;
  isEnabled: boolean;
}

export interface AdminUser {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  phoneNumber: string | null;
  isActive: boolean;
  lastLoginAt: string | null;
  roles: AdminUserRole[];
  createdAt: string;
  lastModifiedAt: string | null;
}

export interface CreateAdminUserRequest {
  userName: string;
  email: string;
  displayName: string;
  phoneNumber?: string;
  password: string;
  isActive: boolean;
}

export interface UpdateAdminUserRequest {
  userName: string;
  email: string;
  displayName: string;
  phoneNumber?: string;
  isActive: boolean;
}

export interface ResetAdminUserPasswordRequest {
  newPassword: string;
  confirmPassword: string;
}

export interface AssignAdminUserRolesRequest {
  roleIds: string[];
}

export interface PageResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
