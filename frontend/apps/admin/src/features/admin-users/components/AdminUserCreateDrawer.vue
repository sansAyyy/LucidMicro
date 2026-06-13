<script setup lang="ts">
import type { FormInstance, FormRules } from 'element-plus';
import { computed, reactive, ref } from 'vue';

import { HttpError } from '@/shared/api/http';
import { createAdminUser } from '../api';
import type { CreateAdminUserRequest } from '../types';

const props = defineProps<{
  modelValue: boolean;
}>();

const emit = defineEmits<{
  created: [];
  'update:modelValue': [value: boolean];
}>();

const formRef = ref<FormInstance>();
const isSubmitting = ref(false);
const errorMessage = ref('');

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
});

const form = reactive<CreateAdminUserRequest>({
  userName: '',
  email: '',
  displayName: '',
  phoneNumber: '',
  password: '',
  isActive: true,
});

const rules: FormRules<CreateAdminUserRequest> = {
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
  password: [
    { required: true, message: '请输入初始密码', trigger: 'blur' },
    { max: 2048, message: '初始密码不能超过 2048 个字符', trigger: 'blur' },
  ],
};

function resetForm() {
  form.userName = '';
  form.email = '';
  form.displayName = '';
  form.phoneNumber = '';
  form.password = '';
  form.isActive = true;
  errorMessage.value = '';
  formRef.value?.clearValidate();
}

function close() {
  visible.value = false;
}

async function submit() {
  if (isSubmitting.value) {
    return;
  }

  const isValid = await formRef.value?.validate().catch(() => false);
  if (!isValid) {
    return;
  }

  isSubmitting.value = true;
  errorMessage.value = '';

  try {
    await createAdminUser({
      ...form,
      phoneNumber: form.phoneNumber?.trim() || undefined,
    });
    emit('created');
    close();
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '管理员创建失败';
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<template>
  <ElDrawer
    v-model="visible"
    class="admin-user-create-drawer"
    direction="rtl"
    size="420px"
    title="新增管理员"
    @closed="resetForm"
  >
    <ElForm ref="formRef" class="drawer-form" :model="form" :rules="rules" label-position="top">
      <ElFormItem label="用户名" prop="userName">
        <ElInput v-model.trim="form.userName" maxlength="64" placeholder="例如 admin-user" />
      </ElFormItem>

      <ElFormItem label="邮箱" prop="email">
        <ElInput v-model.trim="form.email" maxlength="256" placeholder="例如 admin@example.com" />
      </ElFormItem>

      <ElFormItem label="显示名" prop="displayName">
        <ElInput v-model.trim="form.displayName" maxlength="128" placeholder="用于后台展示" />
      </ElFormItem>

      <ElFormItem label="手机号" prop="phoneNumber">
        <ElInput v-model.trim="form.phoneNumber" maxlength="32" placeholder="可选" />
      </ElFormItem>

      <ElFormItem label="初始密码" prop="password">
        <ElInput v-model="form.password" show-password type="password" @keyup.enter="submit" />
      </ElFormItem>

      <ElFormItem label="状态" prop="isActive">
        <ElSwitch v-model="form.isActive" active-text="启用" inactive-text="停用" />
      </ElFormItem>

      <ElAlert v-if="errorMessage" :closable="false" show-icon type="error" :title="errorMessage" />
    </ElForm>

    <template #footer>
      <div class="drawer-footer">
        <ElButton @click="close">取消</ElButton>
        <ElButton type="primary" :loading="isSubmitting" @click="submit">创建</ElButton>
      </div>
    </template>
  </ElDrawer>
</template>
