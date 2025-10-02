import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { expect, within } from '@storybook/test';
import {
  TradeAgentCommands,
  TradeAgentDetailLayout,
  TradeAgentOverview,
  TradeAgentSessionDetails,
  TradeAgentSessionLayout,
  TradeAgentSessionLogs,
  TradeAgentSessions,
  TradeAgentsCatalogue,
} from './TradeAgents';

const meta: Meta<typeof TradeAgentsCatalogue> = {
  component: TradeAgentsCatalogue,
  title: 'TradeAgents/Catalogue',
};

export default meta;

type Story = StoryObj<typeof meta>;

export const Catalogue: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/trade agents/i)).toBeVisible();
  },
};

function renderAgentDetail(path: string) {
  return (
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/trade-agents/:agentId" element={<TradeAgentDetailLayout />}>
          <Route path="overview" element={<TradeAgentOverview />} />
          <Route path="sessions" element={<TradeAgentSessions />} />
          <Route path="commands" element={<TradeAgentCommands />} />
        </Route>
      </Routes>
    </MemoryRouter>
  );
}

function renderSessionDetail(path: string) {
  return (
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route
          path="/trade-agents/:agentId/sessions/:sessionId"
          element={<TradeAgentSessionLayout />}
        >
          <Route path="details" element={<TradeAgentSessionDetails />} />
          <Route path="logs" element={<TradeAgentSessionLogs />} />
        </Route>
      </Routes>
    </MemoryRouter>
  );
}

export const DetailOverview: StoryObj<typeof meta> = {
  render: () => renderAgentDetail('/trade-agents/ta-1402/overview?environment=production'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/health status/i)).toBeVisible();
  },
};

export const DetailSessions: StoryObj<typeof meta> = {
  render: () => renderAgentDetail('/trade-agents/ta-1402/sessions?environment=production'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByRole('table')).toBeInTheDocument();
  },
};

export const DetailCommands: StoryObj<typeof meta> = {
  render: () => renderAgentDetail('/trade-agents/ta-1402/commands?environment=production'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/commands/i)).toBeVisible();
  },
};

export const SessionDetails: StoryObj<typeof meta> = {
  render: () =>
    renderSessionDetail(
      '/trade-agents/ta-1402/sessions/session-9001/details?environment=production',
    ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/broker account/i)).toBeVisible();
  },
};

export const SessionLogs: StoryObj<typeof meta> = {
  render: () =>
    renderSessionDetail('/trade-agents/ta-1402/sessions/session-9001/logs?environment=production'),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/heartbeat acknowledged/i)).toBeVisible();
  },
};
