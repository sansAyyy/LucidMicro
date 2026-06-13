<script setup lang="ts">
import { computed, ref, watch } from 'vue';

import { HttpError } from '@/shared/api/http';
import { getNotificationById } from '../api';
import {
  formatNotificationDateTime,
  getNotificationChannelLabel,
  getNotificationStatusText,
  getNotificationStatusType,
} from '../display';
import type { NotificationMessage } from '../types';

const props = defineProps<{
  modelValue: boolean;
  notificationId: string | null;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const isLoading = ref(false);
const errorMessage = ref('');
const detail = ref<NotificationMessage | null>(null);

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
});

async function loadDetail() {
  if (!props.notificationId) {
    detail.value = null;
    return;
  }

  isLoading.value = true;
  errorMessage.value = '';
  detail.value = null;

  try {
    detail.value = await getNotificationById(props.notificationId);
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '通知详情加载失败';
  } finally {
    isLoading.value = false;
  }
}

watch(
  () => props.modelValue,
  (value) => {
    if (value) {
      loadDetail();
    }
  },
);
</script>

<template>
  <ElDrawer v-model="visible" direction="rtl" size="560px" title="通知详情">
    <ElAlert
      v-if="errorMessage"
      class="drawer-tip"
      :closable="false"
      show-icon
      type="error"
      :title="errorMessage"
    />

    <div v-loading="isLoading" class="notification-detail">
      <template v-if="detail">
        <ElDescriptions :column="1" border>
          <ElDescriptionsItem label="通知 ID">{{ detail.id }}</ElDescriptionsItem>
          <ElDescriptionsItem label="收件人">{{ detail.recipient }}</ElDescriptionsItem>
          <ElDescriptionsItem label="渠道">{{ getNotificationChannelLabel(detail.channel) }}</ElDescriptionsItem>
          <ElDescriptionsItem label="状态">
            <ElTag :type="getNotificationStatusType(detail.status)">
              {{ getNotificationStatusText(detail.status) }}
            </ElTag>
          </ElDescriptionsItem>
          <ElDescriptionsItem label="标题">{{ detail.subject || '-' }}</ElDescriptionsItem>
          <ElDescriptionsItem label="发送时间">{{ formatNotificationDateTime(detail.sentAt) }}</ElDescriptionsItem>
          <ElDescriptionsItem label="失败时间">{{ formatNotificationDateTime(detail.failedAt) }}</ElDescriptionsItem>
          <ElDescriptionsItem label="失败原因">{{ detail.failureReason || '-' }}</ElDescriptionsItem>
        </ElDescriptions>

        <ElCard class="notification-content-card" shadow="never">
          <template #header>内容</template>
          <p>{{ detail.content }}</p>
        </ElCard>
      </template>
    </div>
  </ElDrawer>
</template>
