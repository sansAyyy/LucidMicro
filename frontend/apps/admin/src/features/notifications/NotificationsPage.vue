<script setup lang="ts">
import { Refresh, Search, View } from '@element-plus/icons-vue';
import { onMounted, reactive, ref } from 'vue';

import { HttpError } from '@/shared/api/http';
import { getNotifications } from './api';
import NotificationDetailDrawer from './components/NotificationDetailDrawer.vue';
import {
  formatNotificationDateTime,
  getNotificationChannelLabel,
  getNotificationStatusText,
  getNotificationStatusType,
  notificationChannelOptions,
} from './display';
import type { NotificationChannel, NotificationMessage, NotificationPageResult } from './types';

const isLoading = ref(false);
const isDetailDrawerOpen = ref(false);
const errorMessage = ref('');
const tableData = ref<NotificationMessage[]>([]);
const selectedNotificationId = ref<string | null>(null);
const totalCount = ref(0);

const query = reactive({
  channel: '' as NotificationChannel | '',
  keyword: '',
  pageNumber: 1,
  pageSize: 20,
  sentRange: [] as string[],
});

function applyResult(result: NotificationPageResult) {
  tableData.value = result.items;
  totalCount.value = result.totalCount;
  query.pageNumber = result.pageNumber;
  query.pageSize = result.pageSize;
}

async function loadNotifications() {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const result = await getNotifications({
      channel: query.channel,
      keyword: query.keyword,
      pageNumber: query.pageNumber,
      pageSize: query.pageSize,
      sentFrom: query.sentRange[0],
      sentTo: query.sentRange[1],
    });
    applyResult(result);
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '通知列表加载失败';
  } finally {
    isLoading.value = false;
  }
}

function search() {
  query.pageNumber = 1;
  loadNotifications();
}

function reset() {
  query.channel = '';
  query.keyword = '';
  query.pageNumber = 1;
  query.sentRange = [];
  loadNotifications();
}

function changePage(pageNumber: number) {
  query.pageNumber = pageNumber;
  loadNotifications();
}

function changePageSize(pageSize: number) {
  query.pageSize = pageSize;
  query.pageNumber = 1;
  loadNotifications();
}

function openDetail(row: unknown) {
  const notification = row as NotificationMessage;
  selectedNotificationId.value = notification.id;
  isDetailDrawerOpen.value = true;
}

onMounted(loadNotifications);
</script>

<template>
  <section class="page-stack">
    <div class="page-header">
      <div class="section-heading">
        <p>Notification</p>
        <h2>通知</h2>
      </div>
    </div>

    <ElCard shadow="never">
      <ElForm class="toolbar-form" :model="query" inline @submit.prevent="search">
        <ElFormItem label="关键字">
          <ElInput
            v-model.trim="query.keyword"
            clearable
            placeholder="收件人、标题、内容"
            :prefix-icon="Search"
            @keyup.enter="search"
          />
        </ElFormItem>

        <ElFormItem label="渠道">
          <ElSelect v-model="query.channel" clearable placeholder="全部渠道">
            <ElOption
              v-for="option in notificationChannelOptions"
              :key="option.value"
              :label="option.label"
              :value="option.value"
            />
          </ElSelect>
        </ElFormItem>

        <ElFormItem label="发送日期">
          <ElDatePicker
            v-model="query.sentRange"
            end-placeholder="结束日期"
            range-separator="至"
            start-placeholder="开始日期"
            type="daterange"
            value-format="YYYY-MM-DD"
          />
        </ElFormItem>

        <ElFormItem>
          <ElButton :icon="Search" type="primary" @click="search">查询</ElButton>
          <ElButton :icon="Refresh" @click="reset">重置</ElButton>
        </ElFormItem>
      </ElForm>
    </ElCard>

    <ElAlert v-if="errorMessage" :closable="false" show-icon type="error" :title="errorMessage" />

    <ElCard shadow="never">
      <ElTable v-loading="isLoading" :data="tableData" row-key="id">
        <ElTableColumn prop="recipient" label="收件人" min-width="180" show-overflow-tooltip />
        <ElTableColumn label="渠道" width="110">
          <template #default="{ row }">
            {{ getNotificationChannelLabel(row.channel) }}
          </template>
        </ElTableColumn>
        <ElTableColumn label="状态" width="120">
          <template #default="{ row }">
            <ElTag :type="getNotificationStatusType(row.status)">
              {{ getNotificationStatusText(row.status) }}
            </ElTag>
          </template>
        </ElTableColumn>
        <ElTableColumn prop="subject" label="标题" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.subject || '-' }}
          </template>
        </ElTableColumn>
        <ElTableColumn prop="content" label="内容" min-width="260" show-overflow-tooltip />
        <ElTableColumn label="发送时间" min-width="180">
          <template #default="{ row }">
            {{ formatNotificationDateTime(row.sentAt) }}
          </template>
        </ElTableColumn>
        <ElTableColumn label="失败时间" min-width="180">
          <template #default="{ row }">
            {{ formatNotificationDateTime(row.failedAt) }}
          </template>
        </ElTableColumn>
        <ElTableColumn prop="failureReason" label="失败原因" min-width="220" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.failureReason || '-' }}
          </template>
        </ElTableColumn>
        <ElTableColumn fixed="right" label="操作" width="100">
          <template #default="{ row }">
            <ElButton :icon="View" link type="primary" @click="openDetail(row)">查看</ElButton>
          </template>
        </ElTableColumn>
      </ElTable>

      <div class="table-footer">
        <ElPagination
          background
          layout="total, sizes, prev, pager, next"
          :current-page="query.pageNumber"
          :page-size="query.pageSize"
          :page-sizes="[10, 20, 50]"
          :total="totalCount"
          @current-change="changePage"
          @size-change="changePageSize"
        />
      </div>
    </ElCard>

    <NotificationDetailDrawer v-model="isDetailDrawerOpen" :notification-id="selectedNotificationId" />
  </section>
</template>
