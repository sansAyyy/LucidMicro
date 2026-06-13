import type { PageResult } from '@/features/admin-users/types';

export type NotificationChannel = 'Sms' | 'WeChat' | 'Email' | 'InApp';
export type NotificationStatus = 'Pending' | 'Sent' | 'Failed';

export interface NotificationListQuery {
  channel?: NotificationChannel | '';
  keyword?: string;
  pageNumber: number;
  pageSize: number;
  sentFrom?: string;
  sentTo?: string;
}

export interface NotificationMessage {
  id: string;
  recipient: string;
  channel: NotificationChannel;
  subject: string | null;
  content: string;
  status: NotificationStatus;
  sentAt: string | null;
  failedAt: string | null;
  failureReason: string | null;
}

export type NotificationPageResult = PageResult<NotificationMessage>;
