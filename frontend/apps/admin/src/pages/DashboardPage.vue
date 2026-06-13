<script setup lang="ts">
import { Connection, Guide, Monitor } from '@element-plus/icons-vue';

const metrics = [
  { label: '服务', value: '2', hint: 'Identity / Notification', icon: Monitor },
  { label: '网关', value: 'YARP', hint: 'Gateway 统一入口', icon: Guide },
  { label: '发现', value: 'Consul', hint: '动态服务发现', icon: Connection },
];
</script>

<template>
  <section class="page-stack">
    <div class="section-heading">
      <p>System</p>
      <h2>运行概览</h2>
    </div>

    <div class="metric-grid">
      <ElCard v-for="metric in metrics" :key="metric.label" class="metric-card" shadow="never">
        <div class="metric-title">
          <ElIcon><component :is="metric.icon" /></ElIcon>
          <span>{{ metric.label }}</span>
        </div>
        <strong>{{ metric.value }}</strong>
        <small>{{ metric.hint }}</small>
      </ElCard>
    </div>

    <ElCard class="work-panel" shadow="never">
      <div>
        <ElTag type="warning">Next</ElTag>
        <h3>管理端基础能力</h3>
      </div>
      <p>
        当前已经接入管理员、角色和通知列表。后续可以继续补充通知详情、系统设置和更完整的运行观测入口。
      </p>
    </ElCard>
  </section>
</template>
