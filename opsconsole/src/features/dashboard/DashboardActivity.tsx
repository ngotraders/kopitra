import { activities, statMetrics } from '../../data/console.ts';
import { useActivitiesFilter } from '../../hooks/useActivitiesFilter.ts';
import type { ActivityFilter } from '../../hooks/useActivitiesFilter.ts';
import { useDashboardFilters } from '../../hooks/useDashboardFilters.ts';
import type { DashboardEnvironmentFilter, DashboardTimeframe } from '../../types/console.ts';
import { ActivityTable } from '../../components/ActivityTable/ActivityTable.tsx';
import { StatCard } from '../../components/StatCard/StatCard.tsx';
import { DashboardFilterBar } from './DashboardFilterBar.tsx';
import './Dashboard.css';

const FILTER_LABELS: Record<ActivityFilter, string> = {
  all: 'All',
  success: 'Success',
  warning: 'Warnings',
  error: 'Errors',
};

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

export function DashboardActivity() {
  const { filteredActivities, statusFilter, setStatusFilter, statusTotals } =
    useActivitiesFilter(activities);
  const { timeframe, environment, setTimeframe, setEnvironment } = useDashboardFilters();

  const filters: ActivityFilter[] = ['all', 'success', 'warning', 'error'];

  return (
    <div className="dashboard">
      <DashboardFilterBar
        timeframe={timeframe}
        environment={environment}
        onTimeframeChange={setTimeframe}
        onEnvironmentChange={setEnvironment}
      />
      <p className="dashboard__summary">
        Showing {ENVIRONMENT_SUMMARY[environment]} activity for the {TIMEFRAME_SUMMARY[timeframe]}{' '}
        window.
      </p>
      <section className="dashboard__stats" aria-label="Key metrics">
        {statMetrics.map((metric) => (
          <StatCard key={metric.id} {...metric} />
        ))}
      </section>

      <section className="dashboard__filters" aria-label="Activity filters">
        {filters.map((filter) => {
          const isActive = statusFilter === filter;
          return (
            <button
              key={filter}
              type="button"
              className={`dashboard__filter ${isActive ? 'dashboard__filter--active' : ''}`}
              onClick={() => setStatusFilter(filter)}
            >
              <span>{FILTER_LABELS[filter]}</span>
              {filter === 'all' ? (
                <span className="dashboard__filter-count">{activities.length}</span>
              ) : (
                <span className="dashboard__filter-count">{statusTotals[filter]}</span>
              )}
            </button>
          );
        })}
      </section>

      <ActivityTable activities={filteredActivities} />
    </div>
  );
}

export default DashboardActivity;
