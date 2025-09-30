import { format } from 'date-fns';
import type { Activity } from '../../types/dashboard.ts';
import './ActivityTable.css';

export interface ActivityTableProps {
  activities: Activity[];
}

export function ActivityTable({ activities }: ActivityTableProps) {
  return (
    <section className="activity">
      <header className="activity__header">
        <h2>Recent activity</h2>
        <p>Operational events synced from the last 6 hours.</p>
      </header>
      <table className="activity__table">
        <thead>
          <tr>
            <th scope="col">Time</th>
            <th scope="col">User</th>
            <th scope="col">Action</th>
            <th scope="col">Target</th>
            <th scope="col">Status</th>
          </tr>
        </thead>
        <tbody>
          {activities.map((activity) => (
            <tr key={activity.id}>
              <td>{format(new Date(activity.timestamp), 'HH:mm')}</td>
              <td>{activity.user}</td>
              <td>{activity.action}</td>
              <td>{activity.target}</td>
              <td>
                <span className={`activity__pill activity__pill--${activity.status}`}>
                  {activity.status}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

export default ActivityTable;
