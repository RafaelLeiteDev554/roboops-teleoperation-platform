<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useRoboOpsApi } from '../composables/useRoboOpsApi'
import type { QualityScoreBreakdown, Robot, TaskDefinition, TeleoperationSession } from '../types'

const props = defineProps<{
  id: string
}>()

const { login, request } = useRoboOpsApi()
const route = useRoute()

const sessionId = computed(() => props.id || (route.params.id as string))

const session = ref<TeleoperationSession | null>(null)
const robot = ref<Robot | null>(null)
const task = ref<TaskDefinition | null>(null)
const quality = ref<QualityScoreBreakdown | null>(null)
const error = ref('')

async function load() {
  const id = sessionId.value
  if (!id) {
    return
  }

  error.value = ''
  try {
    await login()
    const s = await request<TeleoperationSession>(`/api/sessions/${id}`)
    session.value = s

    const [robots, tasks] = await Promise.all([
      request<Robot[]>('/api/robots'),
      request<TaskDefinition[]>('/api/tasks'),
    ])

    robot.value = robots.find((r) => r.id === s.robotId) ?? null
    task.value = tasks.find((t) => t.id === s.taskId) ?? null

    try {
      quality.value = await request<QualityScoreBreakdown>(`/api/sessions/${id}/quality`)
    } catch {
      quality.value = null
    }
  } catch (e) {
    session.value = null
    error.value = e instanceof Error ? e.message : 'Session not found'
  }
}

onMounted(load)
watch(sessionId, load)

function formatWhen(iso: string) {
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return iso
  }
}
</script>

<template>
  <main class="shell narrow">
    <p class="back">
      <RouterLink to="/sessions">← Sessions</RouterLink>
    </p>

    <p v-if="error" class="inline-error">{{ error }}</p>

    <template v-else-if="session">
      <header class="page-head">
        <p class="eyebrow">Session</p>
        <h1>{{ session.id }}</h1>
        <p class="subtitle">Started {{ formatWhen(session.startedAt) }} · operator {{ session.operator }}</p>
      </header>

      <div class="detail-grid">
        <article class="panel">
          <p class="eyebrow">Robot</p>
          <h2>{{ robot?.name ?? session.robotId }}</h2>
          <p v-if="robot" class="muted">{{ robot.model }} · {{ robot.location }}</p>
        </article>

        <article class="panel">
          <p class="eyebrow">Task</p>
          <h2>{{ task?.name ?? session.taskId }}</h2>
          <p v-if="task" class="muted">{{ task.environment }}</p>
          <p v-if="task" class="task-body">{{ task.description }}</p>
        </article>

        <article class="panel">
          <p class="eyebrow">Ingestion</p>
          <h2>{{ session.ingestionStatus }}</h2>
          <p class="muted">Quality score (session): {{ session.qualityScore }}</p>
        </article>

        <article v-if="quality" class="panel span-2">
          <p class="eyebrow">Quality breakdown</p>
          <h2>{{ quality.score }}</h2>
          <div class="metrics small">
            <div>
              <span>Video signal</span>
              <strong>{{ quality.videoSignal }}</strong>
            </div>
            <div>
              <span>Control signal</span>
              <strong>{{ quality.controlSignal }}</strong>
            </div>
            <div>
              <span>Metadata</span>
              <strong>{{ quality.metadataCompleteness }}</strong>
            </div>
          </div>
          <p v-if="quality.flags.length" class="flags">
            {{ quality.flags.join(' · ') }}
          </p>
        </article>
      </div>
    </template>
  </main>
</template>

<style scoped>
.narrow {
  max-width: 960px;
}

.back {
  margin: 0 0 1rem;
}

.back a {
  color: var(--accent, #42d392);
  font-weight: 700;
  text-decoration: none;
}

.back a:hover {
  text-decoration: underline;
}

.inline-error {
  color: #ff6170;
}

.page-head h1 {
  font-size: 1.1rem;
  word-break: break-all;
}

.muted {
  color: var(--muted, #91a2bd);
  margin: 0.25rem 0 0;
}

.task-body {
  color: var(--muted, #91a2bd);
  margin-top: 0.75rem;
  line-height: 1.45;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.span-2 {
  grid-column: span 2;
}

.metrics.small {
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-top: 0.75rem;
}

.flags {
  margin: 1rem 0 0;
  color: var(--muted, #91a2bd);
  font-size: 0.9rem;
}

@media (max-width: 700px) {
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .span-2 {
    grid-column: auto;
  }
}
</style>
