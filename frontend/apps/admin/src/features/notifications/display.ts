import { formatDateTime } from '@/shared/utils/dateTime';
import type { NotificationChannel, NotificationStatus } from './types';

export const notificationChannelOptions: Array<{ label: string; value: NotificationChannel }> = [
  { label: '短信', value: 'Sms' },
  { label: '微信', value: 'WeChat' },
  { label: '邮件', value: 'Email' },
  { label: '站内信', value: 'InApp' },
];

export const formatNotificationDateTime = formatDateTime;

export function getNotificationChannelLabel(channel: NotificationChannel) {
  return notificationChannelOptions.find((option) => option.value === channel)?.label ?? channel;
}

export function getNotificationStatusText(status: NotificationStatus) {
  const statusMap: Record<NotificationStatus, string> = {
    Pending: '待发送',
    Sent: '已发送',
    Failed: '发送失败',
  };

  return statusMap[status] ?? status;
}

export function getNotificationStatusType(status: NotificationStatus) {
  const typeMap: Record<NotificationStatus, 'danger' | 'info' | 'success'> = {
    Pending: 'info',
    Sent: 'success',
    Failed: 'danger',
  };

  return typeMap[status] ?? 'info';
}
