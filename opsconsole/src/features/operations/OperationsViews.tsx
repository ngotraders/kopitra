import { format } from 'date-fns';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useContext, useEffect, useMemo, useState, type FormEvent } from 'react';
import { fetchOperationsCommandEvents } from '../../api/fetchOperationsCommandEvents.ts';
import { fetchOperationsCommandPresets } from '../../api/fetchOperationsCommandPresets.ts';
import { fetchCopyGroupSummaries } from '../../api/fetchCopyGroupSummaries.ts';
import { fetchOperationsHealth } from '../../api/fetchOperationsHealth.ts';
import { fetchOperationsPerformanceTrends } from '../../api/fetchOperationsPerformanceTrends.ts';
import { fetchOperationsIncidents } from '../../api/fetchOperationsIncidents.ts';
import { fetchCopyTradeFunnel } from '../../api/fetchCopyTradeFunnel.ts';
import { fetchCopyTradePerformanceAggregates } from '../../api/fetchCopyTradePerformanceAggregates.ts';
import { fetchTradeAgents } from '../../api/fetchTradeAgents.ts';
import { postOperationsCommand } from '../../api/postOperationsCommand.ts';
import { AuthContext } from '../../contexts/auth-context.ts';
import { trackTelemetry } from '../../telemetry/telemetry.ts';
import type { CommandEvent } from '../../types/console.ts';
import './OperationsViews.css';

export function OperationsOverview() {
  const { data: operationsHealth = [] } = useQuery({
    queryKey: ['operations', 'health'],
    queryFn: fetchOperationsHealth,
    staleTime: 30 * 1000,
  });
  const { data: incidents = [] } = useQuery({
    queryKey: ['operations', 'incidents'],
    queryFn: fetchOperationsIncidents,
    staleTime: 60 * 1000,
  });
  const { data: funnelStages = [] } = useQuery({
    queryKey: ['copyTrade', 'funnel'],
    queryFn: fetchCopyTradeFunnel,
    staleTime: 60 * 1000,
  });
  return (
    <div className="operations">
      <section className="operations__grid" aria-label="Health indicators">
        {operationsHealth.map((item) => (
          <article key={item.id} className={`operations__card operations__card--${item.status}`}>
            <header>
              <h3>{item.label}</h3>
              <span>{item.value}</span>
            </header>
            <p>{item.helper}</p>
          </article>
        ))}
      </section>

      <section className="operations__table" aria-label="Active incidents">
        <header>
          <h2>Active incidents</h2>
          <p>Monitor and triage events impacting replication health.</p>
        </header>
        <ul className="operations__incidents-list">
          {incidents.map((incident) => (
            <li key={incident.id} className="operations__incident">
              <div className="operations__incident-header">
                <span className={`operations__badge operations__badge--${incident.severity}`}>
                  {incident.severity}
                </span>
                <h3>{incident.title}</h3>
                <span className="operations__incident-status">{incident.status}</span>
              </div>
              <p>{incident.summary}</p>
              <footer>
                <span>
                  Opened {format(new Date(incident.openedAt), 'MMM d, HH:mm')} · Owner{' '}
                  {incident.owner}
                </span>
                {incident.acknowledgedAt ? (
                  <span>
                    Acknowledged {format(new Date(incident.acknowledgedAt), 'MMM d, HH:mm')}
                  </span>
                ) : (
                  <span>Awaiting acknowledgement</span>
                )}
              </footer>
            </li>
          ))}
        </ul>
      </section>

      <section className="operations__table" aria-label="Copy trade funnel">
        <header>
          <h2>Copy trade funnel</h2>
          <p>Compare notification fan-out with fills and realized P&amp;L.</p>
        </header>
        <table>
          <thead>
            <tr>
              <th scope="col">Environment</th>
              <th scope="col">Notifications</th>
              <th scope="col">Acknowledgements</th>
              <th scope="col">Fills</th>
              <th scope="col">Fill %</th>
              <th scope="col">P&amp;L</th>
            </tr>
          </thead>
          <tbody>
            {funnelStages.map((stage) => {
              const fillRate = stage.notifications
                ? ((stage.fills / stage.notifications) * 100).toFixed(1)
                : '0.0';
              return (
                <tr key={stage.id}>
                  <th scope="row">{stage.label}</th>
                  <td>{stage.notifications.toLocaleString()}</td>
                  <td>{stage.acknowledgements.toLocaleString()}</td>
                  <td>{stage.fills.toLocaleString()}</td>
                  <td>{fillRate}%</td>
                  <td>${stage.pnl.toLocaleString()}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </section>
    </div>
  );
}

export function OperationsCommands() {
  const { data: commandPresets = [] } = useQuery({
    queryKey: ['operations', 'commandPresets'],
    queryFn: fetchOperationsCommandPresets,
    staleTime: 30 * 1000,
  });
  const auth = useContext(AuthContext);
  const queryClient = useQueryClient();
  const operatorName = auth?.user.name ?? 'Unknown operator';
  const { data: commandEvents = [] } = useQuery({
    queryKey: ['operations', 'commandEvents'],
    queryFn: fetchOperationsCommandEvents,
    staleTime: 15 * 1000,
  });
  const { data: copyGroupSummaries = [] } = useQuery({
    queryKey: ['copyGroups', 'summaries'],
    queryFn: fetchCopyGroupSummaries,
    staleTime: 60 * 1000,
  });
  const { data: tradeAgents = [] } = useQuery({
    queryKey: ['tradeAgents'],
    queryFn: fetchTradeAgents,
    staleTime: 60 * 1000,
  });
  const copyGroupOptions = useMemo(
    () =>
      copyGroupSummaries.map((group) => ({
        id: group.id,
        label: group.name,
        scopeLabel: `Copy group ${group.name}`,
      })),
    [copyGroupSummaries],
  );
  const tradeAgentOptions = useMemo(
    () =>
      tradeAgents.map((agent) => ({
        id: agent.id,
        label: agent.name,
        scopeLabel: `Trade agent ${agent.name}`,
      })),
    [tradeAgents],
  );
  const [scopeType, setScopeType] = useState<'copy-group' | 'trade-agent'>('copy-group');
  const options = scopeType === 'copy-group' ? copyGroupOptions : tradeAgentOptions;
  const [scopeId, setScopeId] = useState(options[0]?.id ?? '');
  const [command, setCommand] = useState('');
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!options.length) {
      return;
    }
    if (!options.some((option) => option.id === scopeId)) {
      setScopeId(options[0].id);
    }
  }, [options, scopeId]);

  const { mutateAsync: issueCommand, isPending } = useMutation({
    mutationFn: postOperationsCommand,
    onSuccess: (event) => {
      queryClient.setQueryData<CommandEvent[]>(['operations', 'commandEvents'], (existing = []) => [
        event,
        ...existing,
      ]);
      setCommand('');
      setStatusMessage(`Command sent to ${event.scope}`);
      trackTelemetry({
        type: 'command.issued',
        commandId: event.id,
        scope: event.scope,
        operator: event.operator,
      });
    },
  });

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const selectedOption = options.find((option) => option.id === scopeId);
    if (!selectedOption || !command.trim()) {
      return;
    }
    await issueCommand({
      command: command.trim(),
      scope: selectedOption.scopeLabel,
      operator: operatorName,
    });
  };

  return (
    <div className="operations">
      <section aria-label="Command presets" className="operations__grid">
        {commandPresets.map((preset) => (
          <article key={preset.id} className="operations__preset">
            <header>
              <h3>{preset.name}</h3>
              <span>{preset.targetCount} targets</span>
            </header>
            <p>{preset.description}</p>
            <footer>Last run {format(new Date(preset.lastRun), 'HH:mm, MMM d')}</footer>
          </article>
        ))}
      </section>

      <section aria-label="Command composer" className="operations__table operations__composer">
        <header>
          <h2>Compose command</h2>
          <p>Target a copy group or trade agent and issue operational actions.</p>
        </header>
        <form className="operations__composer-form" onSubmit={handleSubmit}>
          <div className="operations__composer-row">
            <label className="operations__composer-field">
              <span>Command</span>
              <input
                type="text"
                value={command}
                onChange={(event) => setCommand(event.target.value)}
                placeholder="Restart trade agent"
              />
            </label>
          </div>
          <div className="operations__composer-row operations__composer-row--split">
            <label className="operations__composer-field">
              <span>Scope type</span>
              <select
                value={scopeType}
                onChange={(event) =>
                  setScopeType(event.target.value as 'copy-group' | 'trade-agent')
                }
              >
                <option value="copy-group">Copy group</option>
                <option value="trade-agent">Trade agent</option>
              </select>
            </label>
            <label className="operations__composer-field">
              <span>Target</span>
              <select value={scopeId} onChange={(event) => setScopeId(event.target.value)}>
                {options.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
          </div>
          <div className="operations__composer-actions">
            <button type="submit" disabled={!command.trim() || !scopeId || isPending}>
              {isPending ? 'Sending…' : 'Send command'}
            </button>
            <span aria-live="polite" className="operations__composer-status">
              {statusMessage}
            </span>
          </div>
        </form>
      </section>

      <section aria-label="Recent commands" className="operations__table">
        <header>
          <h2>Command queue</h2>
          <p>Live stream of actions issued in the last hour.</p>
        </header>
        <table>
          <thead>
            <tr>
              <th scope="col">Command</th>
              <th scope="col">Scope</th>
              <th scope="col">Operator</th>
              <th scope="col">Issued</th>
              <th scope="col">Status</th>
            </tr>
          </thead>
          <tbody>
            {commandEvents.map((event) => (
              <tr key={event.id}>
                <th scope="row">{event.command}</th>
                <td>{event.scope}</td>
                <td>{event.operator}</td>
                <td>{format(new Date(event.issuedAt), 'HH:mm:ss')}</td>
                <td>
                  <span className={`operations__status operations__status--${event.status}`}>
                    {event.status}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}

export function OperationsHistory() {
  const { data: commandEvents = [] } = useQuery({
    queryKey: ['operations', 'commandEvents'],
    queryFn: fetchOperationsCommandEvents,
    staleTime: 15 * 1000,
  });
  return (
    <div className="operations">
      <section aria-label="Command history" className="operations__table">
        <header>
          <h2>Execution history</h2>
          <p>Audit trail of commands, acknowledgements, and outcomes.</p>
        </header>
        <table>
          <thead>
            <tr>
              <th scope="col">Timestamp</th>
              <th scope="col">Operator</th>
              <th scope="col">Action</th>
              <th scope="col">Scope</th>
              <th scope="col">Status</th>
            </tr>
          </thead>
          <tbody>
            {commandEvents.map((event) => (
              <tr key={`history-${event.id}`}>
                <td>{format(new Date(event.issuedAt), 'MMM d, HH:mm:ss')}</td>
                <td>{event.operator}</td>
                <td>{event.command}</td>
                <td>{event.scope}</td>
                <td>
                  <span className={`operations__status operations__status--${event.status}`}>
                    {event.status}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}

export function OperationsPerformance() {
  const { data: performanceTrends = [] } = useQuery({
    queryKey: ['operations', 'performanceTrends'],
    queryFn: fetchOperationsPerformanceTrends,
    staleTime: 60 * 1000,
  });
  const { data: copyGroupSummaries = [] } = useQuery({
    queryKey: ['copyGroups', 'summaries'],
    queryFn: fetchCopyGroupSummaries,
    staleTime: 60 * 1000,
  });
  const { data: performanceAggregates = [] } = useQuery({
    queryKey: ['copyTrade', 'aggregates'],
    queryFn: fetchCopyTradePerformanceAggregates,
    staleTime: 60 * 1000,
  });
  return (
    <div className="operations">
      <section className="operations__table" aria-label="Copy trade aggregates">
        <header>
          <h2>Copy trade aggregates</h2>
          <p>Notification fan-out, fill conversion, and profitability by environment.</p>
        </header>
        <table>
          <thead>
            <tr>
              <th scope="col">Environment</th>
              <th scope="col">Timeframe</th>
              <th scope="col">Notifications</th>
              <th scope="col">Trade agents reached</th>
              <th scope="col">Fills</th>
              <th scope="col">Fill %</th>
              <th scope="col">Avg P&amp;L / agent</th>
              <th scope="col">Total P&amp;L</th>
            </tr>
          </thead>
          <tbody>
            {performanceAggregates.map((aggregate) => (
              <tr key={aggregate.id}>
                <th scope="row">{aggregate.environment}</th>
                <td>{aggregate.timeframe}</td>
                <td>{aggregate.notifications.toLocaleString()}</td>
                <td>{aggregate.tradeAgentsReached.toLocaleString()}</td>
                <td>{aggregate.fills.toLocaleString()}</td>
                <td>{(aggregate.fillRate * 100).toFixed(1)}%</td>
                <td>${aggregate.avgPnlPerAgent.toLocaleString()}</td>
                <td>${aggregate.pnl.toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="operations__table" aria-label="Performance summary">
        <header>
          <h2>Copy trading conversion</h2>
          <p>Compare aggregated copy-trade signals with downstream results.</p>
        </header>
        <table>
          <thead>
            <tr>
              <th scope="col">Metric</th>
              <th scope="col">Current</th>
              <th scope="col">Previous</th>
              <th scope="col">Δ%</th>
            </tr>
          </thead>
          <tbody>
            {performanceTrends.map((trend) => (
              <tr key={trend.id}>
                <th scope="row">{trend.label}</th>
                <td>{trend.current}</td>
                <td>{trend.previous}</td>
                <td className={trend.delta >= 0 ? 'positive' : 'negative'}>
                  {trend.delta >= 0 ? '+' : ''}
                  {trend.delta}%
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="operations__table" aria-label="Top copy groups">
        <header>
          <h2>Copy group performance</h2>
          <p>Notification fan-out, fills, and P&amp;L across managed groups.</p>
        </header>
        <table>
          <thead>
            <tr>
              <th scope="col">Group</th>
              <th scope="col">Environment</th>
              <th scope="col">Notifications</th>
              <th scope="col">Fills</th>
              <th scope="col">Fill %</th>
              <th scope="col">P&amp;L (24h)</th>
            </tr>
          </thead>
          <tbody>
            {copyGroupSummaries.map((group) => {
              const fillRate = group.notifications24h
                ? ((group.fills24h / group.notifications24h) * 100).toFixed(1)
                : '0.0';
              return (
                <tr key={group.id}>
                  <th scope="row">{group.name}</th>
                  <td>{group.environment}</td>
                  <td>{group.notifications24h.toLocaleString()}</td>
                  <td>{group.fills24h.toLocaleString()}</td>
                  <td>{fillRate}%</td>
                  <td>${group.pnl24h.toLocaleString()}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </section>
    </div>
  );
}
