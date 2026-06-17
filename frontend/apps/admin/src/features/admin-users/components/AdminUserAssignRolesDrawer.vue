<script setup lang="ts">
import { Refresh, Search } from '@element-plus/icons-vue';
import type { TableInstance } from 'element-plus';
import { computed, nextTick, reactive, ref, watch } from 'vue';

import { getRoles } from '@/features/roles/api';
import type { Role, RolePageResult } from '@/features/roles/types';
import { HttpError } from '@/shared/api/http';
import { assignAdminUserRoles, getAdminUserById } from '../api';
import type { AdminUser, AdminUserRole } from '../types';

const props = defineProps<{
  adminUser: AdminUser | null;
  modelValue: boolean;
}>();

const emit = defineEmits<{
  saved: [];
  'update:modelValue': [value: boolean];
}>();

const tableRef = ref<TableInstance>();
const isLoading = ref(false);
const isSubmitting = ref(false);
const errorMessage = ref('');
const roles = ref<Role[]>([]);
const roleMap = ref<Map<string, Role | AdminUserRole>>(new Map());
const selectedRoleIds = ref<Set<string>>(new Set());
const totalCount = ref(0);

const query = reactive({
  keyword: '',
  pageNumber: 1,
  pageSize: 10,
});

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
});

const selectedCount = computed(() => selectedRoleIds.value.size);
const selectedRoles = computed(() =>
  Array.from(selectedRoleIds.value).map((roleId) => ({
    id: roleId,
    role: roleMap.value.get(roleId),
  })),
);

function cacheRoles(nextRoles: Array<Role | AdminUserRole>) {
  const nextRoleMap = new Map(roleMap.value);

  for (const role of nextRoles) {
    nextRoleMap.set(role.id, role);
  }

  roleMap.value = nextRoleMap;
}

function applyResult(result: RolePageResult) {
  roles.value = result.items;
  cacheRoles(result.items);
  totalCount.value = result.totalCount;
  query.pageNumber = result.pageNumber;
  query.pageSize = result.pageSize;
  syncCurrentPageSelection();
}

async function loadAssignedRoles() {
  if (!props.adminUser) {
    return;
  }

  const adminUser = await getAdminUserById(props.adminUser.id);
  selectedRoleIds.value = new Set(adminUser.roles.map((role) => role.id));
  cacheRoles(adminUser.roles);
  syncCurrentPageSelection();
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

function syncCurrentPageSelection() {
  nextTick(() => {
    tableRef.value?.clearSelection();

    for (const role of roles.value) {
      if (selectedRoleIds.value.has(role.id)) {
        tableRef.value?.toggleRowSelection(role, true);
      }
    }
  });
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

function handleSelectionChange(selection: Role[]) {
  const currentPageRoleIds = new Set(roles.value.map((role) => role.id));
  const currentSelectionRoleIds = new Set(selection.map((role) => role.id));
  const nextSelectedRoleIds = new Set(selectedRoleIds.value);

  for (const roleId of currentPageRoleIds) {
    nextSelectedRoleIds.delete(roleId);
  }

  for (const roleId of currentSelectionRoleIds) {
    nextSelectedRoleIds.add(roleId);
  }

  selectedRoleIds.value = nextSelectedRoleIds;
}

function removeSelectedRole(roleId: string) {
  const nextSelectedRoleIds = new Set(selectedRoleIds.value);
  nextSelectedRoleIds.delete(roleId);
  selectedRoleIds.value = nextSelectedRoleIds;
  syncCurrentPageSelection();
}

function selectable(role: Role) {
  return role.isEnabled;
}

function open() {
  roleMap.value = new Map();
  selectedRoleIds.value = new Set(props.adminUser?.roles.map((role) => role.id) ?? []);
  cacheRoles(props.adminUser?.roles ?? []);
  query.keyword = '';
  query.pageNumber = 1;
  query.pageSize = 10;
  loadAssignedRoles().catch((error) => {
    errorMessage.value = error instanceof HttpError ? error.message : '已分配角色加载失败';
  });
  loadRoles();
}

function close() {
  visible.value = false;
}

async function submit() {
  if (!props.adminUser || isSubmitting.value) {
    return;
  }

  isSubmitting.value = true;
  errorMessage.value = '';

  try {
    await assignAdminUserRoles(props.adminUser.id, {
      roleIds: Array.from(selectedRoleIds.value),
    });
    ElMessage.success('角色已更新');
    emit('saved');
    close();
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '角色分配失败';
  } finally {
    isSubmitting.value = false;
  }
}

watch(
  () => props.modelValue,
  (value) => {
    if (value) {
      open();
    }
  },
);
</script>

<template>
  <ElDrawer
    v-model="visible"
    class="admin-user-assign-roles-drawer"
    direction="rtl"
    size="640px"
    title="分配角色"
  >
    <div class="admin-user-assign-roles-drawer__body">
      <ElAlert
        class="drawer-tip"
        :closable="false"
        show-icon
        type="info"
        :title="`正在为管理员「${adminUser?.displayName || adminUser?.userName || '-'}」分配角色`"
      />

      <ElCard class="selected-roles-card" shadow="never">
        <div class="selected-roles-header">
          <span>已选角色</span>
          <small>{{ selectedCount }} 个</small>
        </div>

        <div class="selected-roles-body">
          <div v-if="selectedRoles.length > 0" class="selected-role-tags">
            <ElTag
              v-for="item in selectedRoles"
              :key="item.id"
              closable
              :type="item.role?.isEnabled === false ? 'info' : 'primary'"
              @close="removeSelectedRole(item.id)"
            >
              {{ item.role ? `${item.role.name} (${item.role.code})` : item.id.slice(0, 8) }}
            </ElTag>
          </div>

          <ElEmpty v-else class="selected-roles-empty" description="尚未选择角色" :image-size="56" />
        </div>
      </ElCard>

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

      <ElAlert v-if="errorMessage" class="drawer-tip" :closable="false" show-icon type="error" :title="errorMessage" />

      <ElCard class="drawer-table-card" shadow="never">
        <ElTable
          ref="tableRef"
          v-loading="isLoading"
          :data="roles"
          row-key="id"
          @selection-change="handleSelectionChange"
        >
          <ElTableColumn type="selection" width="44" :selectable="selectable" reserve-selection />
          <ElTableColumn prop="code" label="编码" min-width="120" show-overflow-tooltip />
          <ElTableColumn prop="name" label="名称" min-width="120" show-overflow-tooltip />
          <ElTableColumn prop="description" label="说明" min-width="156" show-overflow-tooltip>
            <template #default="{ row }">
              {{ row.description || '-' }}
            </template>
          </ElTableColumn>
          <ElTableColumn label="状态" width="76">
            <template #default="{ row }">
              <ElTag :type="row.isEnabled ? 'success' : 'info'">
                {{ row.isEnabled ? '启用' : '停用' }}
              </ElTag>
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
    </div>

    <template #footer>
      <div class="drawer-footer">
        <span class="drawer-selection-summary">已选择 {{ selectedCount }} 个角色</span>
        <ElButton @click="close">取消</ElButton>
        <ElButton type="primary" :loading="isSubmitting" @click="submit">保存</ElButton>
      </div>
    </template>
  </ElDrawer>
</template>
