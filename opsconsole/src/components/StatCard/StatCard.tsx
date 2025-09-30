import type { StatMetric } from '../../types/dashboard.ts';
import './StatCard.css';

type StatCardProps = StatMetric;

export function StatCard({ label, value, delta, description }: StatCardProps) {
  const isPositive = delta >= 0;
  return (
    <article className="stat-card">
      <div className="stat-card__header">
        <span className="stat-card__label">{label}</span>
        <span
          className={`stat-card__delta ${isPositive ? 'stat-card__delta--positive' : 'stat-card__delta--negative'}`}
          aria-label={`${Math.abs(delta)} percent ${isPositive ? 'increase' : 'decrease'}`}
        >
          {isPositive ? '+' : '-'}
          {Math.abs(delta).toFixed(1)}%
        </span>
      </div>
      <p className="stat-card__value">{value}</p>
      <p className="stat-card__description">{description}</p>
    </article>
  );
}

export default StatCard;
