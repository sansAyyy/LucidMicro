export const AdminPermissions = {
  AdminUsersRead: 'identity.admin-users.read',
  AdminUsersCreate: 'identity.admin-users.create',
  AdminUsersUpdate: 'identity.admin-users.update',
  AdminUsersEnable: 'identity.admin-users.enable',
  AdminUsersDisable: 'identity.admin-users.disable',
  AdminUsersResetPassword: 'identity.admin-users.reset-password',
  AdminUsersDelete: 'identity.admin-users.delete',
  RolesRead: 'identity.roles.read',
  RolesManage: 'identity.roles.manage',
  RolesAssignPermissions: 'identity.roles.assign-permissions',
  NotificationsRead: 'notification.notifications.read',
  NotificationsManage: 'notification.notifications.manage',
  SettingsRead: 'admin.settings.read',
} as const;

export type AdminPermission = (typeof AdminPermissions)[keyof typeof AdminPermissions];
