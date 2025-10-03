import type { DashboardEnvironmentFilter, DashboardTimeframe } from '../../types/console.ts';
import './Dashboard.css';

const TIMEFRAME_OPTIONS: Array<{ value: DashboardTimeframe; label: string }> = [
  { value: '24h', label: '24 hours' },
  { value: '7d', label: '7 days' },
  { value: '30d', label: '30 days' },
];

const ENVIRONMENT_OPTIONS: Array<{ value: DashboardEnvironmentFilter; label: string }> = [
  { value: 'production', label: 'Production' },
  { value: 'sandbox', label: 'Sandbox' },
  { value: 'all', label: 'All environments' },
];

export interface DashboardFilterBarProps {
  timeframe: DashboardTimeframe;
  environment: DashboardEnvironmentFilter;
  onTimeframeChange: (next: DashboardTimeframe) => void;
  onEnvironmentChange: (next: DashboardEnvironmentFilter) => void;
}

export function DashboardFilterBar({
  timeframe,
  environment,
  onTimeframeChange,
  onEnvironmentChange,
}: DashboardFilterBarProps) {
  return (
    <div className="dashboard__toolbar" aria-label="Dashboard filters">
      <div className="dashboard__segment" role="group" aria-label="Timeframe">
        <span className="dashboard__segment-label">Timeframe</span>
        <div className="dashboard__segment-options">
          {TIMEFRAME_OPTIONS.map(({ value, label }) => {
            const active = value === timeframe;
            return (
              <button
                key={value}
                type="button"
                className={`dashboard__segment-button ${
                  active ? 'dashboard__segment-button--active' : ''
                }`}
                aria-pressed={active}
                onClick={() => onTimeframeChange(value)}
              >
                {label}
              </button>
            );
          })}
        </div>
      </div>
      <div className="dashboard__segment" role="group" aria-label="Environment">
        <span className="dashboard__segment-label">Environment</span>
        <div className="dashboard__segment-options">
          {ENVIRONMENT_OPTIONS.map(({ value, label }) => {
            const active = value === environment;
            return (
              <button
                key={value}
                type="button"
                className={`dashboard__segment-button ${
                  active ? 'dashboard__segment-button--active' : ''
                }`}
                aria-pressed={active}
                onClick={() => onEnvironmentChange(value)}
              >
                {label}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

export default DashboardFilterBar;
