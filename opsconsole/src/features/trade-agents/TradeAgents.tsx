import { formatDistanceToNow } from 'date-fns';
import {
  tradeAgentCommands,
  tradeAgentLogs,
  tradeAgentSessions,
  tradeAgents,
} from '../../data/console.ts';
import type {
  TradeAgentCommand,
  TradeAgentLogEntry,
  TradeAgentSession,
  TradeAgentSummary,
} from '../../types/console.ts';
import { Link, NavLink, Outlet, useOutletContext, useParams } from 'react-router-dom';
import './TradeAgents.css';

interface TradeAgentDetailContext {
  agent: TradeAgentSummary;
  sessions: TradeAgentSession[];
  commands: TradeAgentCommand[];
}

interface TradeAgentSessionContext {
  agent: TradeAgentSummary;
  session: TradeAgentSession;
  logs: TradeAgentLogEntry[];
}

function useTradeAgentDetailContext() {
  return useOutletContext<TradeAgentDetailContext>();
}

function useTradeAgentSessionContext() {
  return useOutletContext<TradeAgentSessionContext>();
}

export function TradeAgentsCatalogue() {
  return (
    <div className="trade-agents">
      <header className="trade-agents__header">
        <h1>Trade agents</h1>
        <p>Monitor release versions, health status, and session counts for each trade agent.</p>
      </header>
      <table>
        <thead>
          <tr>
            <th scope="col">Agent</th>
            <th scope="col">Environment</th>
            <th scope="col">Status</th>
            <th scope="col">Release</th>
            <th scope="col">Active sessions</th>
            <th scope="col">Copy groups</th>
          </tr>
        </thead>
        <tbody>
          {tradeAgents.map((agent) => {
            const search = `?environment=${agent.environment.toLowerCase()}`;
            return (
              <tr key={agent.id}>
                <th scope="row">
                  <Link to={`/trade-agents/${agent.id}${search}`}>{agent.name}</Link>
                </th>
                <td>{agent.environment}</td>
                <td className={`trade-agents__status trade-agents__status--${agent.status}`}>
                  {agent.status}
                </td>
                <td>{agent.release}</td>
                <td>{agent.activeSessions}</td>
                <td>{agent.copyGroupCount}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

export function TradeAgentDetailLayout() {
  const { agentId = '' } = useParams();
  const agent = tradeAgents.find((item) => item.id === agentId);

  if (!agent) {
    return (
      <div className="trade-agents__empty">
        <h2>Trade agent not found</h2>
        <p>The requested agent is unavailable. Return to the catalogue to select another agent.</p>
        <Link to="/trade-agents">Back to catalogue</Link>
      </div>
    );
  }

  const contextValue: TradeAgentDetailContext = {
    agent,
    sessions: tradeAgentSessions[agent.id] ?? [],
    commands: tradeAgentCommands[agent.id] ?? [],
  };

  const search = `?environment=${agent.environment.toLowerCase()}`;
  const tabs = [
    { id: 'overview', label: 'Overview', path: `/trade-agents/${agent.id}/overview` },
    { id: 'sessions', label: 'Sessions', path: `/trade-agents/${agent.id}/sessions` },
    { id: 'commands', label: 'Commands', path: `/trade-agents/${agent.id}/commands` },
  ];

  return (
    <div className="trade-agents-detail">
      <header className="trade-agents-detail__header">
        <div>
          <h1>{agent.name}</h1>
          <p>
            {agent.environment} · Release {agent.release} · {agent.activeSessions} active sessions
          </p>
        </div>
      </header>

      <nav className="trade-agents-detail__tabs" aria-label="Trade agent tabs">
        {tabs.map((tab) => (
          <NavLink
            key={tab.id}
            to={{ pathname: tab.path, search }}
            className={({ isActive }) =>
              `trade-agents-detail__tab ${isActive ? 'trade-agents-detail__tab--active' : ''}`
            }
          >
            {tab.label}
          </NavLink>
        ))}
      </nav>

      <Outlet context={contextValue} />
    </div>
  );
}

export function TradeAgentOverview() {
  const { agent, sessions } = useTradeAgentDetailContext();
  const activeSession = sessions.find((session) => session.status === 'active');
  const lastHeartbeat = activeSession
    ? formatDistanceToNow(new Date(activeSession.lastHeartbeat), { addSuffix: true })
    : 'N/A';

  return (
    <section className="trade-agents-detail__panel" aria-label="Trade agent overview">
      <article>
        <h2>Health status</h2>
        <p>
          The agent is currently {agent.status} with {agent.activeSessions} active sessions. Last
          heartbeat {lastHeartbeat}.
        </p>
      </article>
      <article>
        <h3>Copy group assignments</h3>
        <p>{agent.copyGroupCount} copy groups mapped to this agent.</p>
      </article>
      <article>
        <h3>Release notes</h3>
        <ul>
          <li>2024.04.18 – Improved latency guardrails and error handling.</li>
          <li>2024.04.10 – Added broker fallback routing.</li>
        </ul>
      </article>
    </section>
  );
}

export function TradeAgentSessions() {
  const { agent, sessions } = useTradeAgentDetailContext();

  return (
    <section className="trade-agents-detail__panel" aria-label="Trade agent sessions">
      <table>
        <thead>
          <tr>
            <th scope="col">Broker account</th>
            <th scope="col">Environment</th>
            <th scope="col">Status</th>
            <th scope="col">Latency</th>
            <th scope="col">Started</th>
            <th scope="col">Heartbeat</th>
          </tr>
        </thead>
        <tbody>
          {sessions.map((session) => (
            <tr key={session.id}>
              <th scope="row">
                <Link
                  to={`/trade-agents/${agent.id}/sessions/${session.id}/details?environment=${agent.environment.toLowerCase()}`}
                >
                  {session.brokerAccount}
                </Link>
              </th>
              <td>{session.environment}</td>
              <td>{session.status}</td>
              <td>{session.latencyMs ? `${session.latencyMs} ms` : 'N/A'}</td>
              <td>{formatDistanceToNow(new Date(session.startedAt), { addSuffix: true })}</td>
              <td>{formatDistanceToNow(new Date(session.lastHeartbeat), { addSuffix: true })}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

export function TradeAgentCommands() {
  const { commands } = useTradeAgentDetailContext();

  return (
    <section className="trade-agents-detail__panel" aria-label="Trade agent commands">
      <table>
        <thead>
          <tr>
            <th scope="col">Command</th>
            <th scope="col">Operator</th>
            <th scope="col">Issued</th>
            <th scope="col">Status</th>
          </tr>
        </thead>
        <tbody>
          {commands.map((command) => (
            <tr key={command.id}>
              <th scope="row">{command.command}</th>
              <td>{command.operator}</td>
              <td>{formatDistanceToNow(new Date(command.issuedAt), { addSuffix: true })}</td>
              <td className={`trade-agents__status trade-agents__status--${command.status}`}>
                {command.status}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

export function TradeAgentSessionLayout() {
  const { agentId = '', sessionId = '' } = useParams();
  const agent = tradeAgents.find((item) => item.id === agentId);
  const session = agent
    ? (tradeAgentSessions[agent.id] ?? []).find((item) => item.id === sessionId)
    : undefined;

  if (!agent || !session) {
    return (
      <div className="trade-agents__empty">
        <h2>Session not found</h2>
        <p>The requested trade agent session could not be located.</p>
        <Link to="/trade-agents">Back to catalogue</Link>
      </div>
    );
  }

  const contextValue: TradeAgentSessionContext = {
    agent,
    session,
    logs: tradeAgentLogs[session.id] ?? [],
  };

  const base = `/trade-agents/${agent.id}/sessions/${session.id}`;
  const search = `?environment=${agent.environment.toLowerCase()}`;

  return (
    <div className="trade-agents-detail">
      <header className="trade-agents-detail__header">
        <div>
          <h1>
            {agent.name} · {session.brokerAccount}
          </h1>
          <p>
            {session.environment} session · {session.status} · Latency{' '}
            {session.latencyMs ? `${session.latencyMs} ms` : 'n/a'}
          </p>
        </div>
      </header>

      <nav className="trade-agents-detail__tabs" aria-label="Session tabs">
        <NavLink
          to={{ pathname: `${base}/details`, search }}
          className={({ isActive }) =>
            `trade-agents-detail__tab ${isActive ? 'trade-agents-detail__tab--active' : ''}`
          }
        >
          Details
        </NavLink>
        <NavLink
          to={{ pathname: `${base}/logs`, search }}
          className={({ isActive }) =>
            `trade-agents-detail__tab ${isActive ? 'trade-agents-detail__tab--active' : ''}`
          }
        >
          Logs
        </NavLink>
      </nav>

      <Outlet context={contextValue} />
    </div>
  );
}

export function TradeAgentSessionDetails() {
  const { session } = useTradeAgentSessionContext();

  return (
    <section className="trade-agents-detail__panel" aria-label="Session details">
      <dl className="trade-agents-session__grid">
        <div>
          <dt>Broker account</dt>
          <dd>{session.brokerAccount}</dd>
        </div>
        <div>
          <dt>Status</dt>
          <dd>{session.status}</dd>
        </div>
        <div>
          <dt>Environment</dt>
          <dd>{session.environment}</dd>
        </div>
        <div>
          <dt>Started</dt>
          <dd>{new Date(session.startedAt).toLocaleString()}</dd>
        </div>
        <div>
          <dt>Last heartbeat</dt>
          <dd>{new Date(session.lastHeartbeat).toLocaleString()}</dd>
        </div>
        <div>
          <dt>Latency</dt>
          <dd>{session.latencyMs ? `${session.latencyMs} ms` : 'N/A'}</dd>
        </div>
      </dl>
    </section>
  );
}

export function TradeAgentSessionLogs() {
  const { logs } = useTradeAgentSessionContext();

  return (
    <section className="trade-agents-detail__panel" aria-label="Session logs">
      <table>
        <thead>
          <tr>
            <th scope="col">Timestamp</th>
            <th scope="col">Level</th>
            <th scope="col">Message</th>
          </tr>
        </thead>
        <tbody>
          {logs.map((log) => (
            <tr key={log.id}>
              <th scope="row">{new Date(log.timestamp).toLocaleString()}</th>
              <td>{log.level}</td>
              <td>{log.message}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
