<script setup lang="ts">
import { Delete, Edit, Key, Plus, Refresh, Search } from '@element-plus/icons-vue';
import { computed, onMounted, reactive, ref } from 'vue';

import { HttpError } from '@/shared/api/http';
import { AdminPermissions } from '@/shared/auth/permissions';
import { usePermissions } from '@/shared/auth/usePermissions';
import { formatDateTime } from '@/shared/utils/dateTime';
import { deleteRole, getRoles } from './api';
import RoleAssignPermissionsDrawer from './components/RoleAssignPermissionsDrawer.vue';
import RoleFormDrawer from './components/RoleFormDrawer.vue';
import type { Role, RolePageResult } from './types';

const { can, canAny } = usePermissions();
const isLoading = ref(false);
const isFormDrawerOpen = ref(false);
const isAssignPermissionsDrawerOpen = ref(false);
const errorMessage = ref('');
const tableData = ref<Role[]>([]);
const editingRole = ref<Role | null>(null);
const assigningPermissionsRole = ref<Role | null>(null);
const totalCount = ref(0);

const canOperate = computed(() =>
  canAny([
    AdminPermissions.RolesManage,
    AdminPermissions.RolesAssignPermissions,
  ]),
);

const query = reactive({
  keyword: '',
  pageNumber: 1,
  pageSize: 20,
});

function applyResult(result: RolePageResult) {
  tableData.value = result.items;
  totalCount.value = result.totalCount;
  query.pageNumber = result.pageNumber;
  query.pageSize = result.pageSize;
}

async function loadRoles() {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const result = await getRoles({
      keyword: query.keyword,
      pageNumber: query.pageNumber,
      pageSize: query.pageSize,
    });
    applyResult(result);
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '角色列表加载失败';
  } finally {
    isLoading.value = false;
  }
}

function search() {
  query.pageNumber = 1;
  loadRoles();
}

function reset() {
  query.keyword = '';
  query.pageNumber = 1;
  loadRoles();
}

function changePage(pageNumber: number) {
  query.pageNumber = pageNumber;
  loadRoles();
}

function changePageSize(pageSize: number) {
  query.pageSize = pageSize;
  query.pageNumber = 1;
  loadRoles();
}

function openCreateDrawer() {
  editingRole.value = null;
  isFormDrawerOpen.value = true;
}

function openEditDrawer(row: unknown) {
  editingRole.value = row as Role;
  isFormDrawerOpen.value = true;
}

function openAssignPermissionsDrawer(row: unknown) {
  assigningPermissionsRole.value = row as Role;
  isAssignPermissionsDrawerOpen.value = true;
}

function handleSaved() {
  if (!editingRole.value) {
    query.pageNumber = 1;
  }

  loadRoles();
}

function refreshAfterDelete() {
  if (tableData.value.length === 1 && query.pageNumber > 1) {
    query.pageNumber -= 1;
  }

  loadRoles();
}

async function removeRole(row: unknown) {
  const role = row as Role;

  if (role.isSystem) {
    ElMessage.warning('系统角色不可删除');
    return;
  }

  try {
    await ElMessageBox.confirm(`确定要删除角色「${role.name}」吗？删除后不可恢复。`, '删除角色', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      confirmButtonClass: 'el-button--danger',
      type: 'warning',
    });

    await deleteRole(role.id);
    ElMessage.success('删除成功');
    refreshAfterDelete();
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      return;
    }

    ElMessage.error(error instanceof HttpError ? error.message : '删除失败');
  }
}

onMounted(loadRoles);
</script>

<template>
  <section class="page-stack">
    <div class="page-header">
      <div class="section-heading">
        <p>Identity</p>
        <h2>角色</h2>
      </div>

      <ElButton v-if="can(AdminPermissions.RolesManage)" :icon="Plus" type="primary" @click="openCreateDrawer">
        新增角色
      </ElButton>
    </div>

    <ElCard shadow="never">
      <ElForm class="toolbar-form" :model="query" inline @submit.prevent="search">
        <ElFormItem label="关键字">
          <ElInput
            v-model.trim="query.keyword"
            clearable
            placeholder="角色编码、名称"
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
        <ElTableColumn prop="code" label="编码" min-width="180" show-overflow-tooltip />
        <ElTableColumn prop="name" label="名称" min-width="160" show-overflow-tooltip />
        <ElTableColumn prop="description" label="说明" min-width="240" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.description || '-' }}
          </template>
        </ElTableColumn>
        <ElTableColumn label="类型" width="110">
          <template #default="{ row }">
            <ElTag :type="row.isSystem ? 'warning' : 'info'">
              {{ row.isSystem ? '系统' : '自定义' }}
            </ElTag>
          </template>
        </ElTableColumn>
        <ElTableColumn label="状态" width="110">
          <template #default="{ row }">
            <ElTag :type="row.isEnabled ? 'success' : 'info'">
              {{ row.isEnabled ? '启用' : '停用' }}
            </ElTag>
          </template>
        </ElTableColumn>
        <ElTableColumn label="创建时间" min-width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createdAt) }}
          </template>
        </ElTableColumn>
        <ElTableColumn v-if="canOperate" fixed="right" label="操作" width="230">
          <template #default="{ row }">
            <ElButton
              v-if="can(AdminPermissions.RolesAssignPermissions)"
              :icon="Key"
              link
              type="primary"
              @click="openAssignPermissionsDrawer(row)"
            >
              分配权限
            </ElButton>
            <ElButton
              v-if="can(AdminPermissions.RolesManage) && !row.isSystem"
              :icon="Edit"
              link
              type="primary"
              @click="openEditDrawer(row)"
            >
              编辑
            </ElButton>
            <ElButton
              v-if="can(AdminPermissions.RolesManage) && !row.isSystem"
              :icon="Delete"
              link
              type="danger"
              @click="removeRole(row)"
            >
              删除
            </ElButton>
            <span v-if="row.isSystem" class="muted-cell">系统内置</span>
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

    <RoleFormDrawer v-model="isFormDrawerOpen" :role="editingRole" @saved="handleSaved" />
    <RoleAssignPermissionsDrawer
      v-model="isAssignPermissionsDrawerOpen"
      :role="assigningPermissionsRole"
      @saved="loadRoles"
    />
  </section>
</template>
