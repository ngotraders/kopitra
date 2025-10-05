import { expectManagementOk, managementRequest } from './integration/config.ts';
import { clone } from './utils.ts';
import type {
  Activity,
  CommandEvent,
  CommandPreset,
  ConsoleUser,
  CopyGroupMember,
  CopyGroupPerformanceRow,
  CopyGroupRoute,
  CopyGroupSummary,
  CopyTradeFunnelStage,
  CopyTradePerformanceAggregate,
  HealthKpi,
  NavigationItem,
  OperationsIncident,
  PerformanceTrend,
  StatMetric,
  TradeAgentCommand,
  TradeAgentLogEntry,
  TradeAgentSession,
  TradeAgentSummary,
  UserActivityEvent,
  UserRecord,
} from '../types/console.ts';

export interface OpsConsoleSnapshot {
  navigationItems: NavigationItem[];
  currentUser: ConsoleUser;
  statMetrics: StatMetric[];
  activities: Activity[];
  dashboardTrends: PerformanceTrend[];
  operationsHealth: HealthKpi[];
  operationsIncidents: OperationsIncident[];
  commandPresets: CommandPreset[];
  commandEvents: CommandEvent[];
  operationsPerformanceTrends: PerformanceTrend[];
  copyTradeFunnelStages: CopyTradeFunnelStage[];
  copyTradePerformanceAggregates: CopyTradePerformanceAggregate[];
  copyGroupSummaries: CopyGroupSummary[];
  copyGroupMembers: Record<string, CopyGroupMember[]>;
  copyGroupRoutes: Record<string, CopyGroupRoute[]>;
  copyGroupPerformance: Record<string, CopyGroupPerformanceRow[]>;
  tradeAgents: TradeAgentSummary[];
  tradeAgentSessions: Record<string, TradeAgentSession[]>;
  tradeAgentCommands: Record<string, TradeAgentCommand[]>;
  tradeAgentLogs: Record<string, TradeAgentLogEntry[]>;
  users: UserRecord[];
  userActivity: Record<string, UserActivityEvent[]>;
}

let cachedSnapshot: Promise<OpsConsoleSnapshot> | null = null;

async function loadSnapshot(): Promise<OpsConsoleSnapshot> {
  const response = await managementRequest('/opsconsole/snapshot');
  await expectManagementOk(response);
  const payload = (await response.json()) as OpsConsoleSnapshot;
  return payload;
}

export async function fetchOpsConsoleSnapshot(): Promise<OpsConsoleSnapshot> {
  if (!cachedSnapshot) {
    cachedSnapshot = loadSnapshot().catch((error) => {
      cachedSnapshot = null;
      throw error;
    });
  }

  const snapshot = await cachedSnapshot;
  return clone(snapshot);
}

export function resetOpsConsoleSnapshotCache() {
  cachedSnapshot = null;
}
