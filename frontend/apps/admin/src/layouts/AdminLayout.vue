<script setup lang="ts">
import { SwitchButton } from '@element-plus/icons-vue';
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import { buildNavigation, isNavigationGroup } from '@/app/navigation';
import { useAuthStore } from '@/shared/auth/useAuthStore';

const router = useRouter();
const route = useRoute();
const auth = useAuthStore();

const displayName = computed(() => auth.displayName);
const navigation = computed(() => buildNavigation(router, auth.hasPermissions));

function logout() {
  auth.clearSession();
  router.push({ name: 'login' });
}
</script>

<template>
  <div class="admin-shell">
    <aside class="admin-sidebar">
      <div class="brand">
        <span class="brand-mark">LM</span>
        <span class="brand-name">LucidMicro</span>
      </div>

      <ElMenu class="main-nav" :default-active="route.path" router>
        <template v-for="entry in navigation" :key="isNavigationGroup(entry) ? entry.key : entry.path">
          <ElSubMenu v-if="isNavigationGroup(entry)" :index="entry.key">
            <template #title>
              <span>{{ entry.title }}</span>
            </template>

            <ElMenuItem v-for="item in entry.children" :key="item.path" :index="item.path">
              <ElIcon v-if="item.icon"><component :is="item.icon" /></ElIcon>
              <span>{{ item.title }}</span>
            </ElMenuItem>
          </ElSubMenu>

          <ElMenuItem v-else :index="entry.path">
            <ElIcon v-if="entry.icon"><component :is="entry.icon" /></ElIcon>
            <span>{{ entry.title }}</span>
          </ElMenuItem>
        </template>
      </ElMenu>
    </aside>

    <div class="admin-main">
      <header class="topbar">
        <div>
          <p class="topbar-eyebrow">Admin</p>
          <h1>管理控制台</h1>
        </div>
        <div class="topbar-actions">
          <span class="user-name">{{ displayName }}</span>
          <ElButton :icon="SwitchButton" plain @click="logout">退出</ElButton>
        </div>
      </header>

      <main class="content">
        <RouterView />
      </main>
    </div>
  </div>
</template>
