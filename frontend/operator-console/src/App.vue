<script setup lang="ts">
import { RouterLink, RouterView, useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <div class="app-shell">
    <header class="top-nav">
      <RouterLink class="brand" to="/">RoboOps</RouterLink>
      <nav class="nav-links">
        <RouterLink to="/" :class="{ active: route.path === '/' }">Console</RouterLink>
        <RouterLink to="/sessions" :class="{ active: route.path.startsWith('/sessions') }">Sessions</RouterLink>
      </nav>
    </header>
    <RouterView />
  </div>
</template>
