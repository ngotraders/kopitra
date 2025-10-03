import type { Meta, StoryObj } from '@storybook/react';
import { userEvent, within, expect } from '@storybook/test';
import {
  type CopyTradingClient,
  type CopyTradeExecutionInput,
  type CreateCopyGroupInput,
  type ExpertAdvisorSession,
  type OutboxEvent,
  type CopyGroupMemberInput,
  type TradeCommandInput,
} from '../../api/integration/copyTradingClient.ts';
import { CopyTradingWorkbench } from './CopyTradingWorkbench.tsx';

class MockClient implements CopyTradingClient {
  private sessions = new Map<string, ExpertAdvisorSession>();
  private outboxes = new Map<string, OutboxEvent[]>();

  async connectExpertAdvisor(accountId: string, _authenticationKey: string): Promise<ExpertAdvisorSession> {
    const session: ExpertAdvisorSession = {
      accountId,
      sessionId: `${accountId}-session`,
      sessionToken: `${accountId}-token`,
      authKeyFingerprint: `${accountId}-fingerprint`,
    };
    this.sessions.set(accountId, session);
    this.outboxes.set(accountId, []);
    return session;
  }

  async approveExpertAdvisorSession(
    _session: ExpertAdvisorSession,
    _approvedBy: string,
  ): Promise<void> {}

  async clearOutbox(session: ExpertAdvisorSession): Promise<void> {
    this.outboxes.set(session.accountId, []);
  }

  async fetchOutbox(session: ExpertAdvisorSession): Promise<OutboxEvent[]> {
    return this.outboxes.get(session.accountId) ?? [];
  }

  async acknowledgeOutbox(session: ExpertAdvisorSession, _events: OutboxEvent[]): Promise<void> {
    this.outboxes.set(session.accountId, []);
  }

  async enqueueTradeOrder(_input: TradeCommandInput): Promise<void> {}

  async createCopyGroup(_input: CreateCopyGroupInput): Promise<void> {}

  async upsertCopyGroupMember(_input: CopyGroupMemberInput): Promise<void> {}

  async executeCopyTrade(_input: CopyTradeExecutionInput): Promise<void> {}
}

const meta: Meta<typeof CopyTradingWorkbench> = {
  component: CopyTradingWorkbench,
  title: 'Integration/CopyTradingWorkbench',
  args: {
    client: new MockClient(),
  },
};

export default meta;

type Story = StoryObj<typeof CopyTradingWorkbench>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByRole('heading', { name: /copy trading integration/i })).toBeVisible();

    await userEvent.type(canvas.getByTestId('connect-account'), 'demo-account');
    await userEvent.type(canvas.getByTestId('connect-auth'), 'secret');
    await userEvent.click(canvas.getByTestId('connect-submit'));

    await expect(canvas.getByTestId('session-row-demo-account')).toBeVisible();
  },
};

