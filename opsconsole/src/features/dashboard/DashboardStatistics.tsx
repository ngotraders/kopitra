import { dashboardTrends, statMetrics } from '../../data/console.ts';
import { StatCard } from '../../components/StatCard/StatCard.tsx';
import './DashboardStatistics.css';

export function DashboardStatistics() {
  return (
    <div className="dashboard-statistics">
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
