import { format } from 'date-fns';
import {
  commandEvents,
  commandPresets,
  copyGroupSummaries,
  operationsHealth,
  operationsPerformanceTrends,
} from '../../data/console.ts';
import './OperationsViews.css';

export function OperationsOverview() {
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
    </div>
  );
}

export function OperationsCommands() {
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
  return (
    <div className="operations">
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
            {operationsPerformanceTrends.map((trend) => (
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
          <p>Notification fan-out, fills, and P&L across managed groups.</p>
        </header>
        <table>
          <thead>
            <tr>
              <th scope="col">Group</th>
              <th scope="col">Environment</th>
              <th scope="col">Notifications</th>
              <th scope="col">Fills</th>
              <th scope="col">Fill %</th>
              <th scope="col">P&L (24h)</th>
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
