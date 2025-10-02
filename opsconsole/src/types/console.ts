export type ActivityStatus = 'success' | 'warning' | 'error';

export interface Activity {
  id: string;
  timestamp: string;
  user: string;
  action: string;
  status: ActivityStatus;
  target: string;
}

export interface StatMetric {
  id: string;
  label: string;
  value: string;
  delta: number;
  description: string;
}

export interface NavigationItem {
  id: string;
  label: string;
  to: string;
  badge?: string;
}

export type Environment = 'Production' | 'Sandbox';

export type ConsoleRole = 'operator' | 'admin' | 'analyst';

export interface ConsoleUser {
  id: string;
  name: string;
  email: string;
  roles: ConsoleRole[];
}

export type DashboardTimeframe = '24h' | '7d' | '30d';
export type DashboardEnvironmentFilter = 'production' | 'sandbox' | 'all';

export interface PerformanceTrend {
  id: string;
  label: string;
  current: string;
  previous: string;
  delta: number;
}

export interface CommandPreset {
  id: string;
  name: string;
  description: string;
  targetCount: number;
  lastRun: string;
}

export interface CommandEvent {
  id: string;
  command: string;
  scope: string;
  issuedAt: string;
  operator: string;
  status: 'pending' | 'executed' | 'failed';
}

export interface HealthKpi {
  id: string;
  label: string;
  value: string;
  status: 'good' | 'degraded' | 'critical' | 'attention';
  helper: string;
}

export type IncidentSeverity = 'critical' | 'major' | 'minor';

export interface OperationsIncident {
  id: string;
  title: string;
  severity: IncidentSeverity;
  openedAt: string;
  acknowledgedAt?: string;
  owner: string;
  status: 'open' | 'acknowledged' | 'resolved';
  summary: string;
}

export interface CopyTradeFunnelStage {
  id: string;
  label: string;
  notifications: number;
  acknowledgements: number;
  fills: number;
  pnl: number;
}

export interface CopyTradePerformanceAggregate {
  id: string;
  timeframe: DashboardTimeframe;
  environment: Environment;
  notifications: number;
  tradeAgentsReached: number;
  fills: number;
  pnl: number;
  fillRate: number;
  avgPnlPerAgent: number;
}

export interface CopyGroupSummary {
  id: string;
  name: string;
  environment: Environment;
  status: 'healthy' | 'attention' | 'paused';
  members: number;
  tradeAgents: number;
  notifications24h: number;
  fills24h: number;
  pnl24h: number;
}

export interface CopyGroupMember {
  id: string;
  name: string;
  role: 'Trader' | 'Trade Agent';
  status: 'active' | 'inactive';
  pnl7d: number;
}

export interface CopyGroupRoute {
  id: string;
  destination: string;
  weight: number;
  status: 'healthy' | 'degraded';
}

export interface CopyGroupPerformanceRow {
  agentId: string;
  agentName: string;
  notifications: number;
  fills: number;
  pnl: number;
  winRate: number;
  latencyMs: number;
}

export interface CopyGroupDetail {
  group: CopyGroupSummary;
  members: CopyGroupMember[];
  routes: CopyGroupRoute[];
  performance: CopyGroupPerformanceRow[];
}

export interface TradeAgentSummary {
  id: string;
  name: string;
  status: 'online' | 'degraded' | 'offline';
  environment: Environment;
  release: string;
  activeSessions: number;
  copyGroupCount: number;
}

export interface TradeAgentSession {
  id: string;
  brokerAccount: string;
  environment: Environment;
  status: 'active' | 'pending' | 'closed';
  startedAt: string;
  lastHeartbeat: string;
  latencyMs: number;
}

export interface TradeAgentLogEntry {
  id: string;
  timestamp: string;
  level: 'info' | 'warn' | 'error';
  message: string;
}

export interface TradeAgentCommand {
  id: string;
  issuedAt: string;
  operator: string;
  command: string;
  status: 'pending' | 'executed' | 'failed';
}

export interface TradeAgentDetail {
  agent: TradeAgentSummary;
  sessions: TradeAgentSession[];
  commands: TradeAgentCommand[];
}

export interface TradeAgentSessionDetail {
  agent: TradeAgentSummary;
  session: TradeAgentSession;
  logs: TradeAgentLogEntry[];
}

export interface UserRecord {
  id: string;
  name: string;
  email: string;
  role: 'Operator' | 'Admin' | 'Analyst';
  lastActive: string;
  status: 'active' | 'pending' | 'disabled';
}

export interface UserActivityEvent {
  id: string;
  timestamp: string;
  action: string;
  ip: string;
}

export interface AdminUserDetail {
  user: UserRecord;
  activity: UserActivityEvent[];
}
