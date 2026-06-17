<script setup lang="ts">
import { CircleCheck, CircleClose, Delete, Edit, Key, Plus, Refresh, Search, UserFilled } from '@element-plus/icons-vue';
import { computed, onMounted, reactive, ref } from 'vue';

import { HttpError } from '@/shared/api/http';
import { AdminPermissions } from '@/shared/auth/permissions';
import { usePermissions } from '@/shared/auth/usePermissions';
import { formatDateTime } from '@/shared/utils/dateTime';
import { activateAdminUser, deactivateAdminUser, deleteAdminUser, getAdminUsers } from './api';
import AdminUserAssignRolesDrawer from './components/AdminUserAssignRolesDrawer.vue';
import AdminUserCreateDrawer from './components/AdminUserCreateDrawer.vue';
import AdminUserEditDrawer from './components/AdminUserEditDrawer.vue';
import AdminUserResetPasswordDrawer from './components/AdminUserResetPasswordDrawer.vue';
import type { AdminUser, PageResult } from './types';

const { can, canAny } = usePermissions();
const isLoading = ref(false);
const isCreateDrawerOpen = ref(false);
const isEditDrawerOpen = ref(false);
const isResetPasswordDrawerOpen = ref(false);
const isAssignRolesDrawerOpen = ref(false);
const errorMessage = ref('');
const tableData = ref<AdminUser[]>([]);
const editingAdminUser = ref<AdminUser | null>(null);
const resettingPasswordAdminUser = ref<AdminUser | null>(null);
const assigningRolesAdminUser = ref<AdminUser | null>(null);
const totalCount = ref(0);

const canOperate = computed(() =>
  canAny([
    AdminPermissions.AdminUsersUpdate,
    AdminPermissions.AdminUsersEnable,
    AdminPermissions.AdminUsersDisable,
    AdminPermissions.AdminUsersResetPassword,
    AdminPermissions.AdminUsersDelete,
  ]),
);

const query = reactive({
  keyword: '',
  pageNumber: 1,
  pageSize: 20,
});

function applyResult(result: PageResult<AdminUser>) {
  tableData.value = result.items;
  totalCount.value = result.totalCount;
  query.pageNumber = result.pageNumber;
  query.pageSize = result.pageSize;
}

async function loadAdminUsers() {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const result = await getAdminUsers({
      keyword: query.keyword,
      pageNumber: query.pageNumber,
      pageSize: query.pageSize,
    });
    applyResult(result);
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '管理员列表加载失败';
  } finally {
    isLoading.value = false;
  }
}

function search() {
  query.pageNumber = 1;
  loadAdminUsers();
}

function reset() {
  query.keyword = '';
  query.pageNumber = 1;
  loadAdminUsers();
}

function changePage(pageNumber: number) {
  query.pageNumber = pageNumber;
  loadAdminUsers();
}

function changePageSize(pageSize: number) {
  query.pageSize = pageSize;
  query.pageNumber = 1;
  loadAdminUsers();
}

function openCreateDrawer() {
  isCreateDrawerOpen.value = true;
}

function handleCreated() {
  query.pageNumber = 1;
  loadAdminUsers();
}

function openEditDrawer(row: unknown) {
  editingAdminUser.value = row as AdminUser;
  isEditDrawerOpen.value = true;
}

function handleSaved() {
  loadAdminUsers();
}

function openResetPasswordDrawer(row: unknown) {
  resettingPasswordAdminUser.value = row as AdminUser;
  isResetPasswordDrawerOpen.value = true;
}

function openAssignRolesDrawer(row: unknown) {
  assigningRolesAdminUser.value = row as AdminUser;
  isAssignRolesDrawerOpen.value = true;
}

function handleRolesSaved() {
  loadAdminUsers();
}

function refreshAfterDelete() {
  if (tableData.value.length === 1 && query.pageNumber > 1) {
    query.pageNumber -= 1;
  }

  loadAdminUsers();
}

async function toggleActive(row: unknown) {
  const adminUser = row as AdminUser;
  const nextAction = adminUser.isActive ? '停用' : '启用';

  try {
    await ElMessageBox.confirm(
      `确定要${nextAction}管理员「${adminUser.displayName || adminUser.userName}」吗？`,
      `${nextAction}管理员`,
      {
        confirmButtonText: nextAction,
        cancelButtonText: '取消',
        type: adminUser.isActive ? 'warning' : 'info',
      },
    );

    if (adminUser.isActive) {
      await deactivateAdminUser(adminUser.id);
    } else {
      await activateAdminUser(adminUser.id);
    }

    ElMessage.success(`${nextAction}成功`);
    loadAdminUsers();
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      return;
    }

    ElMessage.error(error instanceof HttpError ? error.message : `${nextAction}失败`);
  }
}

async function deleteUser(row: unknown) {
  const adminUser = row as AdminUser;

  try {
    await ElMessageBox.confirm(
      `确定要删除管理员「${adminUser.displayName || adminUser.userName}」吗？删除后不可恢复。`,
      '删除管理员',
      {
        confirmButtonText: '删除',
        cancelButtonText: '取消',
        confirmButtonClass: 'el-button--danger',
        type: 'warning',
      },
    );

    await deleteAdminUser(adminUser.id);
    ElMessage.success('删除成功');
    refreshAfterDelete();
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      return;
    }

    ElMessage.error(error instanceof HttpError ? error.message : '删除失败');
  }
}

onMounted(loadAdminUsers);
</script>

<template>
  <section class="page-stack">
    <div class="page-header">
      <div class="section-heading">
        <p>Identity</p>
        <h2>管理员</h2>
      </div>

      <ElButton v-if="can(AdminPermissions.AdminUsersCreate)" :icon="Plus" type="primary" @click="openCreateDrawer">
        新增管理员
      </ElButton>
    </div>

    <ElCard shadow="never">
      <ElForm class="toolbar-form" :model="query" inline @submit.prevent="search">
        <ElFormItem label="关键字">
          <ElInput
            v-model.trim="query.keyword"
            clearable
            placeholder="用户名、邮箱、显示名、手机号"
            :prefix-icon="Search"
            @keyup.enter="search"
          />
        </ElFormItem>

        <ElFormItem>
          <ElButton :icon="Search" type="primary" @click="search">查询</ElButton>
          <ElButton :icon="Refresh" @click="reset">重置</ElButton>
        </ElFormItem>
      </ElForm>
    </ElCard>

    <ElAlert v-if="errorMessage" :closable="false" show-icon type="error" :title="errorMessage" />

    <ElCard class="data-table-card" shadow="never">
      <ElTable v-loading="isLoading" :data="tableData" row-key="id">
        <ElTableColumn prop="userName" label="用户名" min-width="150" />
        <ElTableColumn prop="email" label="邮箱" min-width="220" show-overflow-tooltip />
        <ElTableColumn prop="displayName" label="显示名" min-width="150" />
        <ElTableColumn prop="phoneNumber" label="手机号" min-width="140">
          <template #default="{ row }">
            {{ row.phoneNumber || '-' }}
          </template>
        </ElTableColumn>
        <ElTableColumn label="状态" width="110">
          <template #default="{ row }">
            <ElTag :type="row.isActive ? 'success' : 'info'">
              {{ row.isActive ? '启用' : '停用' }}
            </ElTag>
          </template>
        </ElTableColumn>
        <ElTableColumn label="最近登录" min-width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.lastLoginAt) }}
          </template>
        </ElTableColumn>
        <ElTableColumn label="创建时间" min-width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createdAt) }}
          </template>
        </ElTableColumn>
        <ElTableColumn v-if="canOperate" fixed="right" label="操作" width="390">
          <template #default="{ row }">
            <ElButton
              v-if="can(AdminPermissions.AdminUsersUpdate)"
              :icon="Edit"
              link
              type="primary"
              @click="openEditDrawer(row)"
            >
              编辑
            </ElButton>
            <ElButton
              v-if="can(AdminPermissions.AdminUsersUpdate)"
              :icon="UserFilled"
              link
              type="primary"
              @click="openAssignRolesDrawer(row)"
            >
              分配角色
            </ElButton>
            <ElButton
              v-if="can(AdminPermissions.AdminUsersResetPassword)"
              :icon="Key"
              link
              type="primary"
              @click="openResetPasswordDrawer(row)"
            >
              重置密码
            </ElButton>
            <ElButton
              v-if="
                row.isActive
                  ? can(AdminPermissions.AdminUsersDisable)
                  : can(AdminPermissions.AdminUsersEnable)
              "
              :icon="row.isActive ? CircleClose : CircleCheck"
              link
              :type="row.isActive ? 'warning' : 'success'"
              @click="toggleActive(row)"
            >
              {{ row.isActive ? '停用' : '启用' }}
            </ElButton>
            <ElButton
              v-if="can(AdminPermissions.AdminUsersDelete)"
              :icon="Delete"
              link
              type="danger"
              @click="deleteUser(row)"
            >
              删除
            </ElButton>
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

    <AdminUserCreateDrawer v-model="isCreateDrawerOpen" @created="handleCreated" />
    <AdminUserEditDrawer v-model="isEditDrawerOpen" :admin-user="editingAdminUser" @saved="handleSaved" />
    <AdminUserResetPasswordDrawer
      v-model="isResetPasswordDrawerOpen"
      :admin-user="resettingPasswordAdminUser"
    />
    <AdminUserAssignRolesDrawer
      v-model="isAssignRolesDrawerOpen"
      :admin-user="assigningRolesAdminUser"
      @saved="handleRolesSaved"
    />
  </section>
</template>
