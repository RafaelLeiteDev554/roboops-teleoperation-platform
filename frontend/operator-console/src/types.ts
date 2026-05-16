export type RobotStatus = 'Online' | 'Teleoperated' | 'Paused' | 'Offline'
export type IngestionStatus = 'Captured' | 'Processing' | 'Validated' | 'Rejected' | 'Ingested'
export type RobotCommandType = 'Move' | 'Stop' | 'Pause' | 'Resume'

export interface Robot {
  id: string
  name: string
  model: string
  location: string
  status: RobotStatus
  configuration: {
    firmwareVersion: string
    cameraProfile: string
    sensorSuite: string
    networkProfile: string
    revision: number
  }
}

export interface TaskDefinition {
  id: string
  name: string
  description: string
  environment: string
  requiredLabels: string[]
}

export interface TeleoperationSession {
  id: string
  robotId: string
  taskId: string
  operator: string
  startedAt: string
  ingestionStatus: IngestionStatus
  qualityScore: number
}

export interface RobotTelemetry {
  robotId: string
  timestamp: string
  batteryPercent: number
  latencyMs: number
  framesPerSecond: number
  packetLossPercent: number
  positionX: number
  positionY: number
  headingDegrees: number
  status: RobotStatus
}

export interface PipelineRun {
  sessionId: string
  status: IngestionStatus
  datasetName: string
  updatedAt: string
  qualityFlags: string[]
}

export interface QualityScoreBreakdown {
  score: number
  videoSignal: number
  controlSignal: number
  metadataCompleteness: number
  flags: string[]
}
