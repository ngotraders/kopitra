import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from './layouts/AppLayout.tsx';
import { DashboardActivity } from './features/dashboard/DashboardActivity.tsx';
import { DashboardStatistics } from './features/dashboard/DashboardStatistics.tsx';
import {
  OperationsCommands,
  OperationsHistory,
  OperationsOverview,
  OperationsPerformance,
} from './features/operations/OperationsViews.tsx';
import {
  CopyGroupDetailLayout,
  CopyGroupMembership,
  CopyGroupOverview,
  CopyGroupPerformance,
  CopyGroupRouting,
  CopyGroupsList,
} from './features/copy-groups/CopyGroups.tsx';
import {
  TradeAgentCommands,
  TradeAgentDetailLayout,
  TradeAgentOverview,
  TradeAgentSessionDetails,
  TradeAgentSessionLayout,
  TradeAgentSessionLogs,
  TradeAgentSessions,
  TradeAgentsCatalogue,
} from './features/trade-agents/TradeAgents.tsx';
import {
  AdminUserActivity,
  AdminUserDetailLayout,
  AdminUserOverview,
  AdminUserPermissions,
  AdminUsersList,
} from './features/admin/AdminUsers.tsx';
import { NotFound } from './features/not-found/NotFound.tsx';
import { AuthProvider } from './contexts';
import { RequireRoles } from './routes/RequireRoles.tsx';
import { RequireAuth } from './routes/RequireAuth.tsx';
import { LoginPage } from './features/auth/LoginPage.tsx';
import type { ConsoleUser } from './types/console.ts';
import { CopyTradingWorkbench } from './features/integration/CopyTradingWorkbench.tsx';

export interface AppProps {
  onSignOut?: () => void;
  initialUser?: ConsoleUser;
}

function App({ onSignOut, initialUser }: AppProps) {
  return (
    <AuthProvider user={initialUser}>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<RequireAuth />}>
            <Route path="/" element={<AppLayout onSignOut={onSignOut} />}>
              <Route index element={<Navigate to="/dashboard/activity" replace />} />
              <Route path="dashboard">
                <Route index element={<Navigate to="activity" replace />} />
                <Route path="activity" element={<DashboardActivity />} />
                <Route path="statistics" element={<DashboardStatistics />} />
              </Route>
              <Route path="operations">
                <Route index element={<Navigate to="overview" replace />} />
                <Route path="overview" element={<OperationsOverview />} />
                <Route path="commands" element={<OperationsCommands />} />
                <Route path="history" element={<OperationsHistory />} />
                <Route path="performance" element={<OperationsPerformance />} />
              </Route>
              <Route path="integration">
                <Route index element={<Navigate to="copy-trading" replace />} />
                <Route path="copy-trading" element={<CopyTradingWorkbench />} />
              </Route>
              <Route path="copy-groups">
                <Route index element={<CopyGroupsList />} />
                <Route path=":groupId" element={<CopyGroupDetailLayout />}>
                  <Route index element={<Navigate to="overview" replace />} />
                  <Route path="overview" element={<CopyGroupOverview />} />
                  <Route path="membership" element={<CopyGroupMembership />} />
                  <Route path="routing" element={<CopyGroupRouting />} />
                  <Route path="performance" element={<CopyGroupPerformance />} />
                </Route>
              </Route>
              <Route path="trade-agents">
                <Route index element={<TradeAgentsCatalogue />} />
                <Route path=":agentId" element={<TradeAgentDetailLayout />}>
                  <Route index element={<Navigate to="overview" replace />} />
                  <Route path="overview" element={<TradeAgentOverview />} />
                  <Route path="sessions" element={<TradeAgentSessions />} />
                  <Route path="commands" element={<TradeAgentCommands />} />
                </Route>
                <Route path=":agentId/sessions/:sessionId" element={<TradeAgentSessionLayout />}>
                  <Route index element={<Navigate to="details" replace />} />
                  <Route path="details" element={<TradeAgentSessionDetails />} />
                  <Route path="logs" element={<TradeAgentSessionLogs />} />
                </Route>
              </Route>
              <Route element={<RequireRoles roles={['admin']} />}>
                <Route path="admin">
                  <Route path="users">
                    <Route index element={<AdminUsersList />} />
                    <Route path=":userId" element={<AdminUserDetailLayout />}>
                      <Route index element={<Navigate to="overview" replace />} />
                      <Route path="overview" element={<AdminUserOverview />} />
                      <Route path="permissions" element={<AdminUserPermissions />} />
                      <Route path="activity" element={<AdminUserActivity />} />
                    </Route>
                  </Route>
                </Route>
              </Route>
              <Route path="*" element={<NotFound />} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
