<template>
  <div class="route-fallback">
    <ElIcon class="route-fallback__icon">
      <Loading />
    </ElIcon>
    <span>页面加载中...</span>
  </div>
</template>

<script setup lang="ts">
import { Loading } from '@element-plus/icons-vue';
</script>
