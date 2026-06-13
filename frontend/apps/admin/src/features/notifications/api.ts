import { http } from '@/shared/api/http';
import type { NotificationListQuery, NotificationMessage, NotificationPageResult } from './types';

export function getNotifications(query: NotificationListQuery) {
  return http<NotificationPageResult>('/api/notification/notifications', {
    auth: true,
    query: {
      channel: query.channel,
      keyword: query.keyword,
      pageNumber: query.pageNumber,
      pageSize: query.pageSize,
      sentFrom: query.sentFrom,
      sentTo: query.sentTo,
    },
  });
}

export function getNotificationById(id: string) {
  return http<NotificationMessage>(`/api/notification/notifications/${id}`, {
    auth: true,
  });
}
