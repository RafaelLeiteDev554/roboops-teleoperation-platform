<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { useRoboOpsApi } from '../composables/useRoboOpsApi'
import type { Robot, TaskDefinition, TeleoperationSession } from '../types'

const { login, request } = useRoboOpsApi()

const sessions = ref<TeleoperationSession[]>([])
const robots = ref<Record<string, Robot>>({})
const tasks = ref<Record<string, TaskDefinition>>({})
const error = ref('')

function robotName(id: string) {
  return robots.value[id]?.name ?? id.slice(0, 8)
}

function taskName(id: string) {
  return tasks.value[id]?.name ?? id.slice(0, 8)
}

function formatWhen(iso: string) {
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return iso
  }
}

onMounted(async () => {
  try {
    await login()
    const [sessionData, robotList, taskList] = await Promise.all([
      request<TeleoperationSession[]>('/api/sessions'),
      request<Robot[]>('/api/robots'),
      request<TaskDefinition[]>('/api/tasks'),
    ])
    sessions.value = sessionData
    robots.value = Object.fromEntries(robotList.map((r) => [r.id, r]))
    tasks.value = Object.fromEntries(taskList.map((t) => [t.id, t]))
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load sessions'
  }
})
</script>

<template>
  <main class="shell narrow">
    <header class="page-head">
      <p class="eyebrow">Data collection</p>
      <h1>Teleoperation sessions</h1>
      <p class="subtitle">
        Browse recorded operator runs, ingestion status and quality score. Open a row for full detail.
      </p>
    </header>

    <p v-if="error" class="inline-error">{{ error }}</p>

    <div v-else-if="sessions.length === 0" class="panel empty-state">
      <p>No sessions yet. Start one from the <RouterLink to="/">operator console</RouterLink>.</p>
    </div>

    <div v-else class="session-table-wrap panel">
      <div class="table session-table">
        <div class="table-row table-head">
          <span>Started</span>
          <span>Operator</span>
          <span>Robot</span>
          <span>Task</span>
          <span>Quality</span>
          <span>Ingestion</span>
          <span></span>
        </div>
        <div v-for="s in sessions" :key="s.id" class="table-row">
          <span>{{ formatWhen(s.startedAt) }}</span>
          <span>{{ s.operator }}</span>
          <span>{{ robotName(s.robotId) }}</span>
          <span>{{ taskName(s.taskId) }}</span>
          <strong>{{ s.qualityScore }}</strong>
          <span>{{ s.ingestionStatus }}</span>
          <RouterLink class="link-detail" :to="`/sessions/${s.id}`">Detail</RouterLink>
        </div>
      </div>
    </div>
  </main>
</template>

<style scoped>
.narrow {
  max-width: 1100px;
}

.page-head {
  margin-bottom: 1.25rem;
}

.inline-error {
  color: #ff6170;
  margin: 0 0 1rem;
}

.empty-state {
  padding: 1.5rem;
  color: var(--muted, #91a2bd);
}

.empty-state a {
  color: var(--accent, #42d392);
}

.session-table-wrap {
  padding: 0;
  overflow: hidden;
}

.session-table {
  display: grid;
  gap: 0;
}

.session-table .table-row {
  grid-template-columns: 1.4fr 1fr 1fr 1.2fr 80px 110px 90px;
  font-size: 0.9rem;
}

.link-detail {
  color: var(--accent, #42d392);
  font-weight: 700;
  text-decoration: none;
}

.link-detail:hover {
  text-decoration: underline;
}

@media (max-width: 900px) {
  .session-table .table-row {
    grid-template-columns: 1fr;
    gap: 0.35rem;
  }

  .session-table .table-head {
    display: none;
  }
}
</style>
