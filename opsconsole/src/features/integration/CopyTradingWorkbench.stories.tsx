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
  private approvals = new Set<string>();

  async connectExpertAdvisor(
    accountId: string,
    authenticationKey: string,
  ): Promise<ExpertAdvisorSession> {
    void authenticationKey;
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
    session: ExpertAdvisorSession,
    approvedBy: string,
  ): Promise<void> {
    this.approvals.add(`${session.sessionId}:${approvedBy}`);
  }

  async clearOutbox(session: ExpertAdvisorSession): Promise<void> {
    this.outboxes.set(session.accountId, []);
  }

  async fetchOutbox(session: ExpertAdvisorSession): Promise<OutboxEvent[]> {
    return this.outboxes.get(session.accountId) ?? [];
  }

  async acknowledgeOutbox(session: ExpertAdvisorSession, events: OutboxEvent[]): Promise<void> {
    if (events.length === 0) {
      return;
    }
    this.outboxes.set(session.accountId, []);
  }

  async enqueueTradeOrder(input: TradeCommandInput): Promise<void> {
    const events = this.outboxes.get(input.accountId) ?? [];
    events.push({
      id: `${input.accountId}-order-${events.length + 1}`,
      eventType: 'OrderCommand',
      payload: { commandType: input.commandType, instrument: input.instrument },
      requiresAck: true,
    });
    this.outboxes.set(input.accountId, events);
  }

  async createCopyGroup(input: CreateCopyGroupInput): Promise<void> {
    void input;
  }

  async upsertCopyGroupMember(input: CopyGroupMemberInput): Promise<void> {
    void input;
  }

  async executeCopyTrade(input: CopyTradeExecutionInput): Promise<void> {
    void input;
  }
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
