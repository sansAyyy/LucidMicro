<script setup lang="ts">
import { computed, ref, watch } from 'vue';

import { getPermissions } from '@/features/permissions/api';
import type { Permission } from '@/features/permissions/types';
import { HttpError } from '@/shared/api/http';
import { assignRolePermissions, getRoleById } from '../api';
import type { Role } from '../types';

interface PermissionResourceGroup {
  code: string;
  name: string;
  permissions: Permission[];
}

interface PermissionGroup {
  code: string;
  name: string;
  resources: PermissionResourceGroup[];
}

const props = defineProps<{
  modelValue: boolean;
  role: Role | null;
}>();

const emit = defineEmits<{
  saved: [];
  'update:modelValue': [value: boolean];
}>();

const isLoading = ref(false);
const isSubmitting = ref(false);
const errorMessage = ref('');
const permissions = ref<Permission[]>([]);
const selectedPermissionIds = ref<string[]>([]);

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
});

const permissionMap = computed(() => new Map(permissions.value.map((permission) => [permission.id, permission])));
const enabledSelectedPermissionIds = computed(() =>
  selectedPermissionIds.value.filter((permissionId) => permissionMap.value.get(permissionId)?.isEnabled),
);
const selectedCount = computed(() => enabledSelectedPermissionIds.value.length);
const permissionGroups = computed(() => groupPermissions(permissions.value));

function groupPermissions(items: Permission[]) {
  const groups = new Map<string, PermissionGroup>();

  for (const permission of [...items].sort(sortPermissions)) {
    let group = groups.get(permission.groupCode);
    if (!group) {
      group = {
        code: permission.groupCode,
        name: permission.groupName,
        resources: [],
      };
      groups.set(permission.groupCode, group);
    }

    let resource = group.resources.find((item) => item.code === permission.resourceCode);
    if (!resource) {
      resource = {
        code: permission.resourceCode,
        name: permission.resourceName,
        permissions: [],
      };
      group.resources.push(resource);
    }

    resource.permissions.push(permission);
  }

  return Array.from(groups.values());
}

function sortPermissions(left: Permission, right: Permission) {
  return left.sortOrder - right.sortOrder || left.code.localeCompare(right.code);
}

async function open() {
  if (!props.role) {
    return;
  }

  isLoading.value = true;
  errorMessage.value = '';
  permissions.value = [];
  selectedPermissionIds.value = [];

  try {
    const [permissionList, roleDetail] = await Promise.all([getPermissions(), getRoleById(props.role.id)]);
    permissions.value = permissionList;
    selectedPermissionIds.value = [...roleDetail.permissionIds];
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '权限加载失败';
  } finally {
    isLoading.value = false;
  }
}

function close() {
  visible.value = false;
}

async function submit() {
  if (!props.role || isSubmitting.value) {
    return;
  }

  isSubmitting.value = true;
  errorMessage.value = '';

  try {
    await assignRolePermissions(props.role.id, {
      permissionIds: enabledSelectedPermissionIds.value,
    });
    ElMessage.success('权限已更新');
    emit('saved');
    close();
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '权限分配失败';
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
    class="role-assign-permissions-drawer"
    direction="rtl"
    size="720px"
    title="分配权限"
  >
    <ElAlert
      class="drawer-tip"
      :closable="false"
      show-icon
      type="info"
      :title="`正在为角色「${role?.name || role?.code || '-'}」分配权限`"
    />

    <ElAlert v-if="errorMessage" class="drawer-tip" :closable="false" show-icon type="error" :title="errorMessage" />

    <div v-loading="isLoading" class="permission-list">
      <ElEmpty v-if="!isLoading && permissionGroups.length === 0" description="暂无权限" :image-size="72" />

      <ElCard v-for="group in permissionGroups" :key="group.code" class="permission-group-card" shadow="never">
        <div class="permission-group-title">
          <span>{{ group.name }}</span>
          <small>{{ group.code }}</small>
        </div>

        <div class="permission-resource-list">
          <section v-for="resource in group.resources" :key="resource.code" class="permission-resource">
            <div class="permission-resource-title">
              <span>{{ resource.name }}</span>
              <small>{{ resource.code }}</small>
            </div>

            <ElCheckboxGroup v-model="selectedPermissionIds" class="permission-checkboxes">
              <ElCheckbox
                v-for="permission in resource.permissions"
                :key="permission.id"
                :disabled="!permission.isEnabled"
                :value="permission.id"
              >
                <span>{{ permission.name }}</span>
                <small>{{ permission.code }}</small>
                <ElTag v-if="!permission.isEnabled" size="small" type="info">停用</ElTag>
              </ElCheckbox>
            </ElCheckboxGroup>
          </section>
        </div>
      </ElCard>
    </div>

    <template #footer>
      <div class="drawer-footer">
        <span class="drawer-selection-summary">已选择 {{ selectedCount }} 个权限</span>
        <ElButton @click="close">取消</ElButton>
        <ElButton type="primary" :loading="isSubmitting" :disabled="isLoading" @click="submit">保存</ElButton>
      </div>
    </template>
  </ElDrawer>
</template>
