<script setup lang="ts">
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { computed, onMounted, ref } from 'vue'
import { apiBaseUrl, useRoboOpsApi } from '../composables/useRoboOpsApi'
import type {
  Robot,
  TaskDefinition,
  TeleoperationSession,
  RobotTelemetry,
  PipelineRun,
  RobotCommandType,
} from '../types'

const { token, role, login, request } = useRoboOpsApi()

const connectionState = ref('Disconnected')
const robots = ref<Robot[]>([])
const tasks = ref<TaskDefinition[]>([])
const sessions = ref<TeleoperationSession[]>([])
const pipelines = ref<PipelineRun[]>([])
const telemetry = ref<Record<string, RobotTelemetry>>({})
const selectedRobotId = ref('')
const selectedTaskId = ref('')
const commandLog = ref<string[]>([])
const feedbackNotes = ref('Good coverage, labels visible, no unsafe motion.')

const selectedRobot = computed(() => robots.value.find((robot) => robot.id === selectedRobotId.value))
const selectedTelemetry = computed(() => telemetry.value[selectedRobotId.value])
const selectedTask = computed(() => tasks.value.find((task) => task.id === selectedTaskId.value))
const activeSession = computed(() =>
  sessions.value.find((session) => session.robotId === selectedRobotId.value),
)

async function loadDashboard() {
  const [robotData, taskData, sessionData, telemetryData] = await Promise.all([
    request<Robot[]>('/api/robots'),
    request<TaskDefinition[]>('/api/tasks'),
    request<TeleoperationSession[]>('/api/sessions'),
    request<RobotTelemetry[]>('/api/telemetry'),
  ])

  robots.value = robotData
  tasks.value = taskData
  sessions.value = sessionData
  telemetry.value = Object.fromEntries(telemetryData.map((item) => [item.robotId, item]))
  selectedRobotId.value ||= robotData[0]?.id ?? ''
  selectedTaskId.value ||= taskData[0]?.id ?? ''
  await refreshPipelines()
}

async function refreshPipelines() {
  pipelines.value = await request<PipelineRun[]>('/api/pipelines')
}

async function connectRealtime() {
  const connection = new HubConnectionBuilder()
    .withUrl(`${apiBaseUrl}/hubs/telemetry`, {
      accessTokenFactory: () => token.value,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build()

  connection.on('telemetryUpdated', (update: RobotTelemetry) => {
    telemetry.value = { ...telemetry.value, [update.robotId]: update }
  })

  connection.on('commandAccepted', (command: { type: RobotCommandType; issuedBy: string }) => {
    commandLog.value = [`${command.type} accepted for ${command.issuedBy}`, ...commandLog.value].slice(0, 5)
  })

  await connection.start()
  connectionState.value = 'Connected'
  await connection.invoke('WatchDashboard')

  for (const robot of robots.value) {
    await connection.invoke('WatchRobot', robot.id)
  }

  return connection
}

let realtimeConnection: Awaited<ReturnType<typeof connectRealtime>> | null = null

async function startSession() {
  if (!selectedRobotId.value || !selectedTaskId.value) {
    return
  }

  const session = await request<TeleoperationSession>('/api/sessions', {
    method: 'POST',
    body: JSON.stringify({
      robotId: selectedRobotId.value,
      taskId: selectedTaskId.value,
      operator: 'portfolio-operator',
    }),
  })

  sessions.value = [session, ...sessions.value]
  commandLog.value = [`Session ${session.id.slice(0, 8)} started`, ...commandLog.value].slice(0, 5)
}

async function sendCommand(type: RobotCommandType, linearVelocity = 0, angularVelocity = 0) {
  if (!realtimeConnection || !selectedRobotId.value) {
    return
  }

  await realtimeConnection.invoke('SendCommand', {
    robotId: selectedRobotId.value,
    type,
    linearVelocity,
    angularVelocity,
    issuedBy: 'portfolio-operator',
  })
}

async function submitFeedback() {
  const session = activeSession.value
  if (!session) {
    return
  }

  await request(`/api/sessions/${session.id}/feedback`, {
    method: 'POST',
    body: JSON.stringify({
      reviewer: 'portfolio-reviewer',
      videoQuality: 4,
      controlResponsiveness: selectedTelemetry.value?.latencyMs && selectedTelemetry.value.latencyMs > 120 ? 3 : 5,
      labelCompleteness: 4,
      notes: feedbackNotes.value,
    }),
  })

  await loadDashboard()
}

onMounted(async () => {
  try {
    await login()
    await loadDashboard()
    realtimeConnection = await connectRealtime()
  } catch (error) {
    connectionState.value = `Error: ${error instanceof Error ? error.message : 'unknown failure'}`
  }
})
</script>

<template>
  <main class="shell">
    <header class="hero">
      <div>
        <p class="eyebrow">RoboOps Teleoperation Platform</p>
        <h1>Operator console for real-time robot data collection.</h1>
        <p class="subtitle">
          A portfolio-grade teleoperation system with SignalR commands, simulated telemetry,
          quality feedback, RBAC and pipeline monitoring.
        </p>
      </div>
      <div class="status-card">
        <span>Realtime</span>
        <strong>{{ connectionState }}</strong>
        <small>Signed in as {{ role || 'loading' }}</small>
      </div>
    </header>

    <section class="grid">
      <article class="panel span-2">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Teleoperation</p>
            <h2>{{ selectedRobot?.name ?? 'Loading robot' }}</h2>
          </div>
          <button class="danger" @click="sendCommand('Stop')">Emergency stop</button>
        </div>

        <div class="video">
          <div class="reticle"></div>
          <span>Mock WebRTC video feed</span>
        </div>

        <div class="controls">
          <label>
            Robot
            <select v-model="selectedRobotId">
              <option v-for="robot in robots" :key="robot.id" :value="robot.id">
                {{ robot.name }} - {{ robot.location }}
              </option>
            </select>
          </label>
          <label>
            Task
            <select v-model="selectedTaskId">
              <option v-for="task in tasks" :key="task.id" :value="task.id">
                {{ task.name }}
              </option>
            </select>
          </label>
          <button @click="startSession">Start session</button>
        </div>

        <div class="command-pad">
          <button @click="sendCommand('Move', 1, 0)">Forward</button>
          <button @click="sendCommand('Move', 0.4, -1)">Turn left</button>
          <button @click="sendCommand('Move', 0.4, 1)">Turn right</button>
          <button @click="sendCommand('Pause')">Pause</button>
        </div>
      </article>

      <article class="panel">
        <p class="eyebrow">Live telemetry</p>
        <h2>{{ selectedTelemetry?.status ?? 'Waiting' }}</h2>
        <div class="metrics">
          <div>
            <span>Latency</span>
            <strong>{{ selectedTelemetry?.latencyMs ?? '-' }} ms</strong>
          </div>
          <div>
            <span>FPS</span>
            <strong>{{ selectedTelemetry?.framesPerSecond ?? '-' }}</strong>
          </div>
          <div>
            <span>Packet loss</span>
            <strong>{{ selectedTelemetry?.packetLossPercent ?? '-' }}%</strong>
          </div>
          <div>
            <span>Battery</span>
            <strong>{{ selectedTelemetry?.batteryPercent?.toFixed(1) ?? '-' }}%</strong>
          </div>
        </div>
      </article>

      <article class="panel">
        <p class="eyebrow">Robot configuration</p>
        <h2>{{ selectedRobot?.model ?? 'No robot selected' }}</h2>
        <dl v-if="selectedRobot" class="config-list">
          <dt>Firmware</dt>
          <dd>{{ selectedRobot.configuration.firmwareVersion }}</dd>
          <dt>Camera</dt>
          <dd>{{ selectedRobot.configuration.cameraProfile }}</dd>
          <dt>Sensors</dt>
          <dd>{{ selectedRobot.configuration.sensorSuite }}</dd>
          <dt>Network</dt>
          <dd>{{ selectedRobot.configuration.networkProfile }}</dd>
        </dl>
      </article>

      <article class="panel">
        <p class="eyebrow">Data quality feedback</p>
        <h2>{{ activeSession ? `Score ${activeSession.qualityScore}` : 'No active session' }}</h2>
        <p class="task-copy">{{ selectedTask?.description }}</p>
        <textarea v-model="feedbackNotes"></textarea>
        <button :disabled="!activeSession" @click="submitFeedback">Submit review feedback</button>
      </article>

      <article class="panel span-2">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Pipeline dashboard</p>
            <h2>Collected teleoperation sessions</h2>
          </div>
          <button @click="refreshPipelines">Refresh</button>
        </div>

        <div class="table">
          <div class="table-row table-head">
            <span>Dataset</span>
            <span>Status</span>
            <span>Quality flags</span>
          </div>
          <div v-for="run in pipelines" :key="run.sessionId" class="table-row">
            <span>{{ run.datasetName }}</span>
            <strong>{{ run.status }}</strong>
            <span>{{ run.qualityFlags.length ? run.qualityFlags.join(', ') : 'Ready for ingestion' }}</span>
          </div>
        </div>
      </article>

      <article class="panel">
        <p class="eyebrow">Command log</p>
        <h2>Operator events</h2>
        <ul class="log-list">
          <li v-for="item in commandLog" :key="item">{{ item }}</li>
        </ul>
      </article>
    </section>
  </main>
</template>
