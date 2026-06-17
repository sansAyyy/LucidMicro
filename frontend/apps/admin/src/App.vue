<script setup lang="ts">
import zhCn from 'element-plus/es/locale/lang/zh-cn';

import RouteFallback from '@/shared/ui/RouteFallback.vue';
</script>

<template>
  <ElConfigProvider :locale="zhCn">
    <RouterView v-slot="{ Component }">
      <Suspense>
        <component :is="Component" />

        <template #fallback>
          <RouteFallback />
        </template>
      </Suspense>
    </RouterView>
  </ElConfigProvider>
</template>
