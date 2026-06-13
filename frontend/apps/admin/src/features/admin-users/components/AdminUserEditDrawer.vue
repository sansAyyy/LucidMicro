<script setup lang="ts">
import type { FormInstance, FormRules } from 'element-plus';
import { computed, reactive, ref, watch } from 'vue';

import { HttpError } from '@/shared/api/http';
import { updateAdminUser } from '../api';
import type { AdminUser, UpdateAdminUserRequest } from '../types';

const props = defineProps<{
  adminUser: AdminUser | null;
  modelValue: boolean;
}>();

const emit = defineEmits<{
  saved: [];
  'update:modelValue': [value: boolean];
}>();

const formRef = ref<FormInstance>();
const isSubmitting = ref(false);
const errorMessage = ref('');

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
});

const form = reactive<UpdateAdminUserRequest>({
  userName: '',
  email: '',
  displayName: '',
  phoneNumber: '',
  isActive: true,
});

const rules: FormRules<UpdateAdminUserRequest> = {
  userName: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { max: 64, message: '用户名不能超过 64 个字符', trigger: 'blur' },
  ],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { max: 256, message: '邮箱不能超过 256 个字符', trigger: 'blur' },
  ],
  displayName: [
    { required: true, message: '请输入显示名', trigger: 'blur' },
    { max: 128, message: '显示名不能超过 128 个字符', trigger: 'blur' },
  ],
  phoneNumber: [{ max: 32, message: '手机号不能超过 32 个字符', trigger: 'blur' }],
};

function fillForm(adminUser: AdminUser | null) {
  form.userName = adminUser?.userName ?? '';
  form.email = adminUser?.email ?? '';
  form.displayName = adminUser?.displayName ?? '';
  form.phoneNumber = adminUser?.phoneNumber ?? '';
  form.isActive = adminUser?.isActive ?? true;
  errorMessage.value = '';
  formRef.value?.clearValidate();
}

function close() {
  visible.value = false;
}

async function submit() {
  if (!props.adminUser || isSubmitting.value) {
    return;
  }

  const isValid = await formRef.value?.validate().catch(() => false);
  if (!isValid) {
    return;
  }

  isSubmitting.value = true;
  errorMessage.value = '';

  try {
    await updateAdminUser(props.adminUser.id, {
      ...form,
      phoneNumber: form.phoneNumber?.trim() || undefined,
    });
    emit('saved');
    close();
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '管理员保存失败';
  } finally {
    isSubmitting.value = false;
  }
}

watch(
  () => props.adminUser,
  (adminUser) => fillForm(adminUser),
  { immediate: true },
);
</script>

<template>
  <ElDrawer
    v-model="visible"
    class="admin-user-edit-drawer"
    direction="rtl"
    size="420px"
    title="编辑管理员"
    @open="fillForm(adminUser)"
  >
    <ElForm ref="formRef" class="drawer-form" :model="form" :rules="rules" label-position="top">
      <ElFormItem label="用户名" prop="userName">
        <ElInput v-model.trim="form.userName" maxlength="64" />
      </ElFormItem>

      <ElFormItem label="邮箱" prop="email">
        <ElInput v-model.trim="form.email" maxlength="256" />
      </ElFormItem>

      <ElFormItem label="显示名" prop="displayName">
        <ElInput v-model.trim="form.displayName" maxlength="128" />
      </ElFormItem>

      <ElFormItem label="手机号" prop="phoneNumber">
        <ElInput v-model.trim="form.phoneNumber" maxlength="32" placeholder="可选" />
      </ElFormItem>

      <ElFormItem label="状态" prop="isActive">
        <ElSwitch v-model="form.isActive" active-text="启用" inactive-text="停用" />
      </ElFormItem>

      <ElAlert v-if="errorMessage" :closable="false" show-icon type="error" :title="errorMessage" />
    </ElForm>

    <template #footer>
      <div class="drawer-footer">
        <ElButton @click="close">取消</ElButton>
        <ElButton type="primary" :loading="isSubmitting" @click="submit">保存</ElButton>
      </div>
    </template>
  </ElDrawer>
</template>
