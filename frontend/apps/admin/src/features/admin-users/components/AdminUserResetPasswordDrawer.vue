<script setup lang="ts">
import type { FormInstance, FormRules } from 'element-plus';
import { computed, reactive, ref } from 'vue';

import { HttpError } from '@/shared/api/http';
import { resetAdminUserPassword } from '../api';
import type { AdminUser, ResetAdminUserPasswordRequest } from '../types';

const props = defineProps<{
  adminUser: AdminUser | null;
  modelValue: boolean;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const formRef = ref<FormInstance>();
const isSubmitting = ref(false);
const errorMessage = ref('');

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
});

const form = reactive<ResetAdminUserPasswordRequest>({
  newPassword: '',
  confirmPassword: '',
});

const rules: FormRules<ResetAdminUserPasswordRequest> = {
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { max: 2048, message: '新密码不能超过 2048 个字符', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    { max: 2048, message: '确认密码不能超过 2048 个字符', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== form.newPassword) {
          callback(new Error('两次输入的密码不一致'));
          return;
        }

        callback();
      },
      trigger: 'blur',
    },
  ],
};

function resetForm() {
  form.newPassword = '';
  form.confirmPassword = '';
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
    await resetAdminUserPassword(props.adminUser.id, form);
    ElMessage.success('密码已重置');
    close();
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '密码重置失败';
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<template>
  <ElDrawer
    v-model="visible"
    class="admin-user-reset-password-drawer"
    direction="rtl"
    size="420px"
    title="重置密码"
    @closed="resetForm"
  >
    <ElAlert
      class="drawer-tip"
      :closable="false"
      show-icon
      type="warning"
      :title="`正在重置管理员「${adminUser?.displayName || adminUser?.userName || '-'}」的密码`"
    />

    <ElForm ref="formRef" class="drawer-form" :model="form" :rules="rules" label-position="top">
      <ElFormItem label="新密码" prop="newPassword">
        <ElInput v-model="form.newPassword" show-password type="password" />
      </ElFormItem>

      <ElFormItem label="确认密码" prop="confirmPassword">
        <ElInput v-model="form.confirmPassword" show-password type="password" @keyup.enter="submit" />
      </ElFormItem>

      <ElAlert v-if="errorMessage" :closable="false" show-icon type="error" :title="errorMessage" />
    </ElForm>

    <template #footer>
      <div class="drawer-footer">
        <ElButton @click="close">取消</ElButton>
        <ElButton type="primary" :loading="isSubmitting" @click="submit">重置密码</ElButton>
      </div>
    </template>
  </ElDrawer>
</template>
