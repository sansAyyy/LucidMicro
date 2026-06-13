<script setup lang="ts">
import type { FormInstance, FormRules } from 'element-plus';
import { computed, reactive, ref, watch } from 'vue';

import { HttpError } from '@/shared/api/http';
import { createRole, updateRole } from '../api';
import type { CreateRoleRequest, Role, UpdateRoleRequest } from '../types';

const props = defineProps<{
  modelValue: boolean;
  role: Role | null;
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
const isEdit = computed(() => Boolean(props.role));
const title = computed(() => (isEdit.value ? '编辑角色' : '新增角色'));

const form = reactive<CreateRoleRequest>({
  code: '',
  name: '',
  description: '',
  isEnabled: true,
});

const rules: FormRules<CreateRoleRequest> = {
  code: [
    { required: true, message: '请输入角色编码', trigger: 'blur' },
    { max: 64, message: '角色编码不能超过 64 个字符', trigger: 'blur' },
  ],
  name: [
    { required: true, message: '请输入角色名称', trigger: 'blur' },
    { max: 128, message: '角色名称不能超过 128 个字符', trigger: 'blur' },
  ],
  description: [{ max: 512, message: '说明不能超过 512 个字符', trigger: 'blur' }],
};

function fillForm(role: Role | null) {
  form.code = role?.code ?? '';
  form.name = role?.name ?? '';
  form.description = role?.description ?? '';
  form.isEnabled = role?.isEnabled ?? true;
  errorMessage.value = '';
  formRef.value?.clearValidate();
}

function close() {
  visible.value = false;
}

function normalizeDescription() {
  return form.description?.trim() || undefined;
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
    if (props.role) {
      const request: UpdateRoleRequest = {
        name: form.name,
        description: normalizeDescription(),
        isEnabled: form.isEnabled,
      };
      await updateRole(props.role.id, request);
    } else {
      await createRole({
        ...form,
        description: normalizeDescription(),
      });
    }

    emit('saved');
    close();
  } catch (error) {
    errorMessage.value = error instanceof HttpError ? error.message : '角色保存失败';
  } finally {
    isSubmitting.value = false;
  }
}

watch(
  () => props.role,
  (role) => fillForm(role),
  { immediate: true },
);
</script>

<template>
  <ElDrawer
    v-model="visible"
    class="role-form-drawer"
    direction="rtl"
    size="420px"
    :title="title"
    @open="fillForm(role)"
  >
    <ElForm ref="formRef" class="drawer-form" :model="form" :rules="rules" label-position="top">
      <ElFormItem label="角色编码" prop="code">
        <ElInput v-model.trim="form.code" :disabled="isEdit" maxlength="64" placeholder="例如 AdminUserViewer" />
      </ElFormItem>

      <ElFormItem label="角色名称" prop="name">
        <ElInput v-model.trim="form.name" maxlength="128" />
      </ElFormItem>

      <ElFormItem label="说明" prop="description">
        <ElInput v-model.trim="form.description" maxlength="512" placeholder="可选" type="textarea" :rows="4" />
      </ElFormItem>

      <ElFormItem label="状态" prop="isEnabled">
        <ElSwitch v-model="form.isEnabled" active-text="启用" inactive-text="停用" />
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
