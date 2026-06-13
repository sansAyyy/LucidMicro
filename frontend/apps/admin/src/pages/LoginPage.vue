<script setup lang="ts">
import { Lock, User } from '@element-plus/icons-vue';
import { reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import { HttpError } from '@/shared/api/http';
import { useAuthStore } from '@/shared/auth/useAuthStore';

const router = useRouter();
const route = useRoute();
const auth = useAuthStore();
const isSubmitting = ref(false);
const errorMessage = ref('');

const form = reactive({
  loginName: 'admin',
  password: 'Admin@123456',
});

function getLoginErrorMessage(error: unknown) {
  if (error instanceof HttpError) {
    if (error.status === 401 || error.status === 400) {
      return '账号或密码不正确';
    }

    return error.message;
  }

  return error instanceof Error ? error.message : '登录失败';
}

async function submit() {
  if (!form.loginName || !form.password || isSubmitting.value) {
    return;
  }

  isSubmitting.value = true;
  errorMessage.value = '';

  try {
    await auth.login({
      loginName: form.loginName,
      password: form.password,
    });

    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/dashboard';
    await router.push(redirect);
  } catch (error) {
    errorMessage.value = getLoginErrorMessage(error);
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<template>
  <section class="login-panel">
    <div class="login-copy">
      <span class="brand-mark">LM</span>
      <h1>LucidMicro Admin</h1>
      <p>统一管理 Identity、Notification 和后续业务服务。</p>
    </div>

    <ElForm class="login-form" :model="form" label-position="top" @submit.prevent="submit">
      <ElFormItem label="账号">
        <ElInput v-model.trim="form.loginName" autocomplete="username" name="loginName" :prefix-icon="User" />
      </ElFormItem>

      <ElFormItem label="密码">
        <ElInput
          v-model="form.password"
          autocomplete="current-password"
          name="password"
          :prefix-icon="Lock"
          show-password
          type="password"
          @keyup.enter="submit"
        />
      </ElFormItem>

      <ElButton class="login-button" type="primary" :loading="isSubmitting" @click="submit">
        登录
      </ElButton>

      <ElAlert v-if="errorMessage" :closable="false" show-icon type="error" :title="errorMessage" />
    </ElForm>
  </section>
</template>
