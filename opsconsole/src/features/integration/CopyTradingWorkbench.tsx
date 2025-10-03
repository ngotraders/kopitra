import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  type CopyGroupMemberInput,
  type CopyTradingClient,
  type CreateCopyGroupInput,
  type ExpertAdvisorSession,
  type OutboxEvent,
  type TradeCommandInput,
  type CopyTradeExecutionInput,
  createCopyTradingClient,
} from '../../api/integration/copyTradingClient.ts';
import './CopyTradingWorkbench.css';

interface CopyTradingWorkbenchProps {
  client?: CopyTradingClient;
}

interface SessionState extends ExpertAdvisorSession {
  approved: boolean;
  busy: boolean;
  lastOutbox: OutboxEvent[];
  status?: string;
}

interface GroupState {
  id: string;
  name: string;
  requestedBy: string;
  members: string[];
}

const defaultClient = createCopyTradingClient();

function formatError(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }
  return 'An unexpected error occurred';
}

function parseNumber(value: string): number | undefined {
  const trimmed = value.trim();
  if (!trimmed) {
    return undefined;
  }
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : undefined;
}

export function CopyTradingWorkbench({ client = defaultClient }: CopyTradingWorkbenchProps) {
  const [sessions, setSessions] = useState<SessionState[]>([]);
  const [groups, setGroups] = useState<GroupState[]>([]);
  const [globalStatus, setGlobalStatus] = useState<string | null>(null);

  const [connectForm, setConnectForm] = useState({
    accountId: '',
    authKey: '',
  });

  const [approvalActor, setApprovalActor] = useState('ops-console');

  const [tradeForm, setTradeForm] = useState({
    sessionAccountId: '',
    commandType: 'open',
    instrument: 'USDJPY',
    orderType: 'market',
    side: 'buy',
    volume: '1',
    timeInForce: 'gtc',
    clientOrderId: 'ops-order-1',
    stopLoss: '',
    takeProfit: '',
    positionId: '',
  });

  const [groupForm, setGroupForm] = useState({
    groupId: '',
    name: '',
    requestedBy: 'ops-console',
  });

  const [memberForm, setMemberForm] = useState({
    groupId: '',
    accountId: '',
    role: 'follower',
    riskStrategy: 'balanced',
    allocation: '1',
    requestedBy: 'ops-console',
  });

  const [copyTradeForm, setCopyTradeForm] = useState({
    groupId: '',
    sourceAccount: '',
    initiatedBy: 'ops-console',
    commandType: 'open',
    instrument: 'EURUSD',
    orderType: 'market',
    side: 'buy',
    volume: '1',
    timeInForce: 'gtc',
    clientOrderId: 'copy-order-1',
    stopLoss: '',
    takeProfit: '',
    positionId: '',
  });

  const availableSessions = useMemo(
    () => sessions.map((session) => ({ id: session.accountId, label: session.accountId })),
    [sessions],
  );

  useEffect(() => {
    if (!tradeForm.sessionAccountId && availableSessions.length) {
      setTradeForm((current) => ({ ...current, sessionAccountId: availableSessions[0].id }));
    }
    if (!memberForm.accountId && availableSessions.length) {
      setMemberForm((current) => ({ ...current, accountId: availableSessions[0].id }));
    }
    if (!copyTradeForm.sourceAccount && availableSessions.length) {
      setCopyTradeForm((current) => ({ ...current, sourceAccount: availableSessions[0].id }));
    }
  }, [
    availableSessions,
    copyTradeForm.sourceAccount,
    memberForm.accountId,
    tradeForm.sessionAccountId,
  ]);

  useEffect(() => {
    if (!memberForm.groupId && groups.length) {
      setMemberForm((current) => ({ ...current, groupId: groups[0].id }));
    }
    if (!copyTradeForm.groupId && groups.length) {
      setCopyTradeForm((current) => ({ ...current, groupId: groups[0].id }));
    }
  }, [copyTradeForm.groupId, groups, memberForm.groupId]);

  const handleConnect = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    try {
      setGlobalStatus(`Connecting ${connectForm.accountId}…`);
      const session = await client.connectExpertAdvisor(
        connectForm.accountId.trim(),
        connectForm.authKey.trim(),
      );
      setSessions((current) => [
        ...current.filter((existing) => existing.accountId !== session.accountId),
        { ...session, approved: false, busy: false, lastOutbox: [] },
      ]);
      setConnectForm({ accountId: '', authKey: '' });
      setGlobalStatus(`Connected session ${session.accountId}`);
    } catch (error) {
      setGlobalStatus(formatError(error));
    }
  };

  const updateSession = (accountId: string, updater: (session: SessionState) => SessionState) => {
    setSessions((current) =>
      current.map((session) => (session.accountId === accountId ? updater(session) : session)),
    );
  };

  const handleApprove = async (session: SessionState) => {
    updateSession(session.accountId, (current) => ({
      ...current,
      busy: true,
      status: 'Approving…',
    }));
    try {
      await client.approveExpertAdvisorSession(session, approvalActor);
      updateSession(session.accountId, (current) => ({
        ...current,
        approved: true,
        busy: false,
        status: `Approved by ${approvalActor}`,
      }));
    } catch (error) {
      updateSession(session.accountId, (current) => ({
        ...current,
        busy: false,
        status: formatError(error),
      }));
    }
  };

  const handleClearOutbox = async (session: SessionState) => {
    updateSession(session.accountId, (current) => ({
      ...current,
      busy: true,
      status: 'Clearing outbox…',
    }));
    try {
      await client.clearOutbox(session);
      updateSession(session.accountId, (current) => ({
        ...current,
        busy: false,
        lastOutbox: [],
        status: 'Outbox cleared',
      }));
    } catch (error) {
      updateSession(session.accountId, (current) => ({
        ...current,
        busy: false,
        status: formatError(error),
      }));
    }
  };

  const handleFetchOutbox = async (session: SessionState) => {
    updateSession(session.accountId, (current) => ({
      ...current,
      busy: true,
      status: 'Fetching outbox…',
    }));
    try {
      const events = await client.fetchOutbox(session);
      updateSession(session.accountId, (current) => ({
        ...current,
        busy: false,
        lastOutbox: events,
        status: `Fetched ${events.length} event${events.length === 1 ? '' : 's'}`,
      }));
    } catch (error) {
      updateSession(session.accountId, (current) => ({
        ...current,
        busy: false,
        status: formatError(error),
      }));
    }
  };

  const handleAckOutbox = async (session: SessionState) => {
    updateSession(session.accountId, (current) => ({
      ...current,
      busy: true,
      status: 'Acknowledging events…',
    }));
    try {
      await client.acknowledgeOutbox(session, session.lastOutbox);
      updateSession(session.accountId, (current) => ({
        ...current,
        busy: false,
        lastOutbox: [],
        status: 'Events acknowledged',
      }));
    } catch (error) {
      updateSession(session.accountId, (current) => ({
        ...current,
        busy: false,
        status: formatError(error),
      }));
    }
  };

  const handleTradeSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const session = sessions.find((item) => item.accountId === tradeForm.sessionAccountId);
    if (!session) {
      setGlobalStatus('Select a session to send a trade order');
      return;
    }

    const payload: TradeCommandInput = {
      accountId: session.accountId,
      sessionId: session.sessionId,
      commandType: tradeForm.commandType,
      instrument: tradeForm.instrument,
      orderType: tradeForm.orderType || undefined,
      side: tradeForm.side || undefined,
      volume: parseNumber(tradeForm.volume),
      timeInForce: tradeForm.timeInForce || undefined,
      clientOrderId: tradeForm.clientOrderId || undefined,
      stopLoss: parseNumber(tradeForm.stopLoss),
      takeProfit: parseNumber(tradeForm.takeProfit),
      positionId: tradeForm.positionId || undefined,
    };

    try {
      setGlobalStatus(`Sending ${tradeForm.commandType} order to ${session.accountId}…`);
      await client.enqueueTradeOrder(payload);
      setGlobalStatus(`Trade order sent to ${session.accountId}`);
    } catch (error) {
      setGlobalStatus(formatError(error));
    }
  };

  const handleCreateGroup = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const payload: CreateCopyGroupInput = {
      groupId: groupForm.groupId.trim(),
      name: groupForm.name.trim(),
      requestedBy: groupForm.requestedBy.trim() || 'ops-console',
    };

    if (!payload.groupId || !payload.name) {
      setGlobalStatus('Group ID and name are required');
      return;
    }

    try {
      setGlobalStatus(`Creating copy group ${payload.groupId}…`);
      await client.createCopyGroup(payload);
      setGroups((current) => [
        ...current.filter((group) => group.id !== payload.groupId),
        {
          id: payload.groupId,
          name: payload.name,
          requestedBy: payload.requestedBy,
          members: [],
        },
      ]);
      setGroupForm((current) => ({ ...current, groupId: '', name: '' }));
      setGlobalStatus(`Copy group ${payload.groupId} created`);
    } catch (error) {
      setGlobalStatus(formatError(error));
    }
  };

  const handleAddMember = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const targetGroup = groups.find((group) => group.id === memberForm.groupId);
    const session = sessions.find((item) => item.accountId === memberForm.accountId);
    if (!targetGroup || !session) {
      setGlobalStatus('Select a valid group and expert advisor session');
      return;
    }

    const payload: CopyGroupMemberInput = {
      groupId: targetGroup.id,
      accountId: session.accountId,
      role: memberForm.role,
      riskStrategy: memberForm.riskStrategy,
      allocation: parseNumber(memberForm.allocation) ?? 1,
      requestedBy: memberForm.requestedBy.trim() || 'ops-console',
    };

    try {
      setGlobalStatus(`Adding ${payload.accountId} to ${payload.groupId}…`);
      await client.upsertCopyGroupMember(payload);
      setGroups((current) =>
        current.map((group) =>
          group.id === payload.groupId
            ? {
                ...group,
                members: Array.from(new Set([...group.members, payload.accountId])),
              }
            : group,
        ),
      );
      setGlobalStatus(`Added ${payload.accountId} to ${payload.groupId}`);
    } catch (error) {
      setGlobalStatus(formatError(error));
    }
  };

  const handleExecuteCopyTrade = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!copyTradeForm.groupId) {
      setGlobalStatus('Select a copy group');
      return;
    }
    const payload: CopyTradeExecutionInput = {
      groupId: copyTradeForm.groupId,
      sourceAccount: copyTradeForm.sourceAccount.trim(),
      initiatedBy: copyTradeForm.initiatedBy.trim() || undefined,
      commandType: copyTradeForm.commandType,
      instrument: copyTradeForm.instrument,
      orderType: copyTradeForm.orderType || undefined,
      side: copyTradeForm.side || undefined,
      volume: parseNumber(copyTradeForm.volume),
      timeInForce: copyTradeForm.timeInForce || undefined,
      clientOrderId: copyTradeForm.clientOrderId || undefined,
      stopLoss: parseNumber(copyTradeForm.stopLoss),
      takeProfit: parseNumber(copyTradeForm.takeProfit),
      positionId: copyTradeForm.positionId || undefined,
    };

    try {
      setGlobalStatus(`Executing copy trade for ${payload.groupId}…`);
      await client.executeCopyTrade(payload);
      setGlobalStatus(`Copy trade dispatched for ${payload.groupId}`);
    } catch (error) {
      setGlobalStatus(formatError(error));
    }
  };

  return (
    <div className="copy-trading-workbench">
      <header>
        <h1>Copy trading integration workbench</h1>
        <p>
          Connect expert advisors, approve sessions, orchestrate copy trades, and inspect outbox
          events using live services.
        </p>
        <p
          aria-live="polite"
          data-testid="workbench-status"
          className="copy-trading-workbench__status"
        >
          {globalStatus}
        </p>
      </header>

      <section aria-label="Connect expert advisors" className="copy-trading-workbench__section">
        <h2>1. Connect expert advisors</h2>
        <form
          className="copy-trading-workbench__form"
          onSubmit={handleConnect}
          data-testid="connect-form"
        >
          <label>
            <span>Account ID</span>
            <input
              data-testid="connect-account"
              type="text"
              value={connectForm.accountId}
              onChange={(event) =>
                setConnectForm((current) => ({ ...current, accountId: event.target.value }))
              }
              required
            />
          </label>
          <label>
            <span>Authentication key</span>
            <input
              data-testid="connect-auth"
              type="text"
              value={connectForm.authKey}
              onChange={(event) =>
                setConnectForm((current) => ({ ...current, authKey: event.target.value }))
              }
              required
            />
          </label>
          <button type="submit" data-testid="connect-submit">
            Connect
          </button>
        </form>
      </section>

      <section aria-label="Manage sessions" className="copy-trading-workbench__section">
        <h2>2. Manage sessions</h2>
        <label className="copy-trading-workbench__inline-field">
          <span>Approved by</span>
          <input
            data-testid="approve-actor"
            type="text"
            value={approvalActor}
            onChange={(event) => setApprovalActor(event.target.value)}
          />
        </label>
        <table className="copy-trading-workbench__table">
          <thead>
            <tr>
              <th scope="col">Account</th>
              <th scope="col">Session</th>
              <th scope="col">Approved</th>
              <th scope="col">Actions</th>
              <th scope="col">Status</th>
            </tr>
          </thead>
          <tbody>
            {sessions.map((session) => (
              <tr key={session.accountId} data-testid={`session-row-${session.accountId}`}>
                <th scope="row">{session.accountId}</th>
                <td>{session.sessionId}</td>
                <td data-testid={`session-approved-${session.accountId}`}>
                  {session.approved ? 'Yes' : 'No'}
                </td>
                <td>
                  <div className="copy-trading-workbench__actions">
                    <button
                      type="button"
                      data-testid={`approve-session-${session.accountId}`}
                      onClick={() => handleApprove(session)}
                      disabled={session.busy}
                    >
                      Approve
                    </button>
                    <button
                      type="button"
                      data-testid={`clear-outbox-${session.accountId}`}
                      onClick={() => handleClearOutbox(session)}
                      disabled={session.busy}
                    >
                      Clear outbox
                    </button>
                    <button
                      type="button"
                      data-testid={`fetch-outbox-${session.accountId}`}
                      onClick={() => handleFetchOutbox(session)}
                      disabled={session.busy}
                    >
                      Refresh outbox
                    </button>
                    <button
                      type="button"
                      data-testid={`ack-outbox-${session.accountId}`}
                      onClick={() => handleAckOutbox(session)}
                      disabled={session.busy || session.lastOutbox.length === 0}
                    >
                      Ack events
                    </button>
                  </div>
                </td>
                <td data-testid={`session-status-${session.accountId}`}>
                  {session.status ?? 'Idle'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {sessions.map((session) => (
          <div key={`outbox-${session.accountId}`} className="copy-trading-workbench__outbox">
            <h3>
              Outbox · {session.accountId} ({session.lastOutbox.length} events)
            </h3>
            <textarea
              readOnly
              spellCheck={false}
              data-testid={`outbox-json-${session.accountId}`}
              value={session.lastOutbox.length ? JSON.stringify(session.lastOutbox, null, 2) : '[]'}
            />
          </div>
        ))}
      </section>

      <section aria-label="Direct trade orders" className="copy-trading-workbench__section">
        <h2>3. Direct trade orders</h2>
        <form
          className="copy-trading-workbench__form"
          onSubmit={handleTradeSubmit}
          data-testid="trade-form"
        >
          <label>
            <span>Session</span>
            <select
              data-testid="trade-session"
              value={tradeForm.sessionAccountId}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, sessionAccountId: event.target.value }))
              }
              required
            >
              <option value="" disabled>
                Select session
              </option>
              {availableSessions.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Command type</span>
            <select
              data-testid="trade-command-type"
              value={tradeForm.commandType}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, commandType: event.target.value }))
              }
            >
              <option value="open">open</option>
              <option value="close">close</option>
            </select>
          </label>
          <label>
            <span>Instrument</span>
            <input
              data-testid="trade-instrument"
              type="text"
              value={tradeForm.instrument}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, instrument: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Order type</span>
            <select
              data-testid="trade-order-type"
              value={tradeForm.orderType}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, orderType: event.target.value }))
              }
            >
              <option value="market">market</option>
              <option value="limit">limit</option>
            </select>
          </label>
          <label>
            <span>Side</span>
            <select
              data-testid="trade-side"
              value={tradeForm.side}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, side: event.target.value }))
              }
            >
              <option value="buy">buy</option>
              <option value="sell">sell</option>
            </select>
          </label>
          <label>
            <span>Volume</span>
            <input
              data-testid="trade-volume"
              type="number"
              step="0.01"
              value={tradeForm.volume}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, volume: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Time in force</span>
            <input
              data-testid="trade-tif"
              type="text"
              value={tradeForm.timeInForce}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, timeInForce: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Client order ID</span>
            <input
              data-testid="trade-client-order"
              type="text"
              value={tradeForm.clientOrderId}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, clientOrderId: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Stop loss</span>
            <input
              data-testid="trade-stop-loss"
              type="number"
              step="0.0001"
              value={tradeForm.stopLoss}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, stopLoss: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Take profit</span>
            <input
              data-testid="trade-take-profit"
              type="number"
              step="0.0001"
              value={tradeForm.takeProfit}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, takeProfit: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Position ID</span>
            <input
              data-testid="trade-position-id"
              type="text"
              value={tradeForm.positionId}
              onChange={(event) =>
                setTradeForm((current) => ({ ...current, positionId: event.target.value }))
              }
            />
          </label>
          <button type="submit" data-testid="trade-submit">
            Send order
          </button>
        </form>
      </section>

      <section aria-label="Copy group management" className="copy-trading-workbench__section">
        <h2>4. Copy group management</h2>
        <form
          className="copy-trading-workbench__form"
          onSubmit={handleCreateGroup}
          data-testid="group-form"
        >
          <label>
            <span>Group ID</span>
            <input
              data-testid="group-id"
              type="text"
              value={groupForm.groupId}
              onChange={(event) =>
                setGroupForm((current) => ({ ...current, groupId: event.target.value }))
              }
              required
            />
          </label>
          <label>
            <span>Name</span>
            <input
              data-testid="group-name"
              type="text"
              value={groupForm.name}
              onChange={(event) =>
                setGroupForm((current) => ({ ...current, name: event.target.value }))
              }
              required
            />
          </label>
          <label>
            <span>Requested by</span>
            <input
              data-testid="group-requested-by"
              type="text"
              value={groupForm.requestedBy}
              onChange={(event) =>
                setGroupForm((current) => ({ ...current, requestedBy: event.target.value }))
              }
            />
          </label>
          <button type="submit" data-testid="group-submit">
            Create group
          </button>
        </form>

        <form
          className="copy-trading-workbench__form"
          onSubmit={handleAddMember}
          data-testid="member-form"
        >
          <label>
            <span>Group</span>
            <select
              data-testid="member-group"
              value={memberForm.groupId}
              onChange={(event) =>
                setMemberForm((current) => ({ ...current, groupId: event.target.value }))
              }
              required
            >
              <option value="" disabled>
                Select group
              </option>
              {groups.map((group) => (
                <option key={group.id} value={group.id}>
                  {group.name} ({group.id})
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Account</span>
            <select
              data-testid="member-account"
              value={memberForm.accountId}
              onChange={(event) =>
                setMemberForm((current) => ({ ...current, accountId: event.target.value }))
              }
              required
            >
              <option value="" disabled>
                Select session
              </option>
              {availableSessions.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Role</span>
            <select
              data-testid="member-role"
              value={memberForm.role}
              onChange={(event) =>
                setMemberForm((current) => ({ ...current, role: event.target.value }))
              }
            >
              <option value="leader">leader</option>
              <option value="follower">follower</option>
            </select>
          </label>
          <label>
            <span>Risk strategy</span>
            <input
              data-testid="member-risk"
              type="text"
              value={memberForm.riskStrategy}
              onChange={(event) =>
                setMemberForm((current) => ({ ...current, riskStrategy: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Allocation</span>
            <input
              data-testid="member-allocation"
              type="number"
              step="0.1"
              value={memberForm.allocation}
              onChange={(event) =>
                setMemberForm((current) => ({ ...current, allocation: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Requested by</span>
            <input
              data-testid="member-requested-by"
              type="text"
              value={memberForm.requestedBy}
              onChange={(event) =>
                setMemberForm((current) => ({ ...current, requestedBy: event.target.value }))
              }
            />
          </label>
          <button type="submit" data-testid="member-submit">
            Upsert member
          </button>
        </form>

        <div className="copy-trading-workbench__groups">
          {groups.map((group) => (
            <article key={group.id} data-testid={`group-card-${group.id}`}>
              <h3>
                {group.name} ({group.id})
              </h3>
              <p>Requested by {group.requestedBy}</p>
              <ul>
                {group.members.map((member) => (
                  <li key={`${group.id}-${member}`}>{member}</li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      </section>

      <section aria-label="Execute copy trades" className="copy-trading-workbench__section">
        <h2>5. Execute copy trades</h2>
        <form
          className="copy-trading-workbench__form"
          onSubmit={handleExecuteCopyTrade}
          data-testid="copy-trade-form"
        >
          <label>
            <span>Group</span>
            <select
              data-testid="copy-group"
              value={copyTradeForm.groupId}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, groupId: event.target.value }))
              }
              required
            >
              <option value="" disabled>
                Select group
              </option>
              {groups.map((group) => (
                <option key={`copy-${group.id}`} value={group.id}>
                  {group.name} ({group.id})
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Source account</span>
            <select
              data-testid="copy-source"
              value={copyTradeForm.sourceAccount}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, sourceAccount: event.target.value }))
              }
              required
            >
              <option value="" disabled>
                Select leader
              </option>
              {availableSessions.map((option) => (
                <option key={`source-${option.id}`} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Initiated by</span>
            <input
              data-testid="copy-initiated-by"
              type="text"
              value={copyTradeForm.initiatedBy}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, initiatedBy: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Command type</span>
            <select
              data-testid="copy-command-type"
              value={copyTradeForm.commandType}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, commandType: event.target.value }))
              }
            >
              <option value="open">open</option>
              <option value="close">close</option>
            </select>
          </label>
          <label>
            <span>Instrument</span>
            <input
              data-testid="copy-instrument"
              type="text"
              value={copyTradeForm.instrument}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, instrument: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Order type</span>
            <select
              data-testid="copy-order-type"
              value={copyTradeForm.orderType}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, orderType: event.target.value }))
              }
            >
              <option value="market">market</option>
              <option value="limit">limit</option>
            </select>
          </label>
          <label>
            <span>Side</span>
            <select
              data-testid="copy-side"
              value={copyTradeForm.side}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, side: event.target.value }))
              }
            >
              <option value="buy">buy</option>
              <option value="sell">sell</option>
            </select>
          </label>
          <label>
            <span>Volume</span>
            <input
              data-testid="copy-volume"
              type="number"
              step="0.01"
              value={copyTradeForm.volume}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, volume: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Time in force</span>
            <input
              data-testid="copy-tif"
              type="text"
              value={copyTradeForm.timeInForce}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, timeInForce: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Client order ID</span>
            <input
              data-testid="copy-client-order"
              type="text"
              value={copyTradeForm.clientOrderId}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, clientOrderId: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Stop loss</span>
            <input
              data-testid="copy-stop-loss"
              type="number"
              step="0.0001"
              value={copyTradeForm.stopLoss}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, stopLoss: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Take profit</span>
            <input
              data-testid="copy-take-profit"
              type="number"
              step="0.0001"
              value={copyTradeForm.takeProfit}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, takeProfit: event.target.value }))
              }
            />
          </label>
          <label>
            <span>Position ID</span>
            <input
              data-testid="copy-position-id"
              type="text"
              value={copyTradeForm.positionId}
              onChange={(event) =>
                setCopyTradeForm((current) => ({ ...current, positionId: event.target.value }))
              }
            />
          </label>
          <button type="submit" data-testid="copy-submit">
            Execute copy trade
          </button>
        </form>
      </section>
    </div>
  );
}

export default CopyTradingWorkbench;
