import { dashboardTrends, statMetrics } from '../../data/console.ts';
import { StatCard } from '../../components/StatCard/StatCard.tsx';
import { useDashboardFilters } from '../../hooks/useDashboardFilters.ts';
import type { DashboardEnvironmentFilter, DashboardTimeframe } from '../../types/console.ts';
import { DashboardFilterBar } from './DashboardFilterBar.tsx';
import './Dashboard.css';
import './DashboardStatistics.css';

const TIMEFRAME_SUMMARY: Record<DashboardTimeframe, string> = {
  '24h': 'last 24 hours',
  '7d': 'last 7 days',
  '30d': 'last 30 days',
};

const ENVIRONMENT_SUMMARY: Record<DashboardEnvironmentFilter, string> = {
  production: 'production',
  sandbox: 'sandbox',
  all: 'all environments',
};

export function DashboardStatistics() {
  const { timeframe, environment, setTimeframe, setEnvironment } = useDashboardFilters();

  return (
    <div className="dashboard-statistics">
      <DashboardFilterBar
        timeframe={timeframe}
        environment={environment}
        onTimeframeChange={setTimeframe}
        onEnvironmentChange={setEnvironment}
      />
      <p className="dashboard__summary">
        Viewing {ENVIRONMENT_SUMMARY[environment]} metrics over the {TIMEFRAME_SUMMARY[timeframe]}{' '}
        window.
      </p>
      <section className="dashboard-statistics__metrics" aria-label="Key performance metrics">
        {statMetrics.map((metric) => (
          <StatCard key={metric.id} {...metric} />
        ))}
      </section>

      <section className="dashboard-statistics__trends" aria-label="Performance trends">
        <header>
          <h2>Trend comparisons</h2>
          <p>Compare the current 24 hour period with the previous trading window.</p>
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
            {dashboardTrends.map((trend) => (
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
    </div>
  );
}

export default DashboardStatistics;
