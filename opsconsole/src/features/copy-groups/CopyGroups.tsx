import { format } from 'date-fns';
import { useQuery } from '@tanstack/react-query';
import { fetchCopyGroupDetail } from '../../api/fetchCopyGroupDetail.ts';
import { fetchCopyGroupSummaries } from '../../api/fetchCopyGroupSummaries.ts';
import type {
  CopyGroupMember,
  CopyGroupPerformanceRow,
  CopyGroupRoute,
  CopyGroupSummary,
} from '../../types/console.ts';
import { Link, NavLink, Outlet, useOutletContext, useParams } from 'react-router-dom';
import './CopyGroups.css';

interface CopyGroupDetailContext {
  group: CopyGroupSummary;
  members: CopyGroupMember[];
  routes: CopyGroupRoute[];
  performance: CopyGroupPerformanceRow[];
}

function useCopyGroupDetailContext() {
  return useOutletContext<CopyGroupDetailContext>();
}

export function CopyGroupsList() {
  const { data: copyGroupSummaries = [] } = useQuery({
    queryKey: ['copyGroups', 'summaries'],
    queryFn: fetchCopyGroupSummaries,
    staleTime: 60 * 1000,
  });
  return (
    <div className="copy-groups">
      <header className="copy-groups__header">
        <h1>Copy groups</h1>
        <p>Monitor membership, routing, and notification conversion across the fleet.</p>
      </header>
      <table>
        <thead>
          <tr>
            <th scope="col">Group</th>
            <th scope="col">Environment</th>
            <th scope="col">Members</th>
            <th scope="col">Trade agents</th>
            <th scope="col">Notifications (24h)</th>
            <th scope="col">Fills (24h)</th>
            <th scope="col">Fill %</th>
            <th scope="col">P&L (24h)</th>
          </tr>
        </thead>
        <tbody>
          {copyGroupSummaries.map((group) => {
            const search = `?environment=${group.environment.toLowerCase()}`;
            const fillRate = group.notifications24h
              ? ((group.fills24h / group.notifications24h) * 100).toFixed(1)
              : '0.0';
            return (
              <tr key={group.id}>
                <th scope="row">
                  <Link to={`/copy-groups/${group.id}${search}`}>{group.name}</Link>
                </th>
                <td>{group.environment}</td>
                <td>{group.members}</td>
                <td>{group.tradeAgents}</td>
                <td>{group.notifications24h.toLocaleString()}</td>
                <td>{group.fills24h.toLocaleString()}</td>
                <td>{fillRate}%</td>
                <td>${group.pnl24h.toLocaleString()}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

export function CopyGroupDetailLayout() {
  const { groupId = '' } = useParams();
  const { data: detail, isError } = useQuery({
    queryKey: ['copyGroups', 'detail', groupId],
    queryFn: () => fetchCopyGroupDetail(groupId),
    enabled: Boolean(groupId),
    staleTime: 60 * 1000,
  });

  if (!detail || isError) {
    return (
      <div className="copy-groups__empty">
        <h2>Group not found</h2>
        <p>The requested copy group is not registered. Please return to the catalogue.</p>
        <Link to="/copy-groups">Back to list</Link>
      </div>
    );
  }

  const { group, members, routes, performance } = detail;

  const contextValue: CopyGroupDetailContext = {
    group,
    members,
    routes,
    performance,
  };

  const search = `?environment=${group.environment.toLowerCase()}`;
  const tabs = [
    { id: 'overview', label: 'Overview', path: `/copy-groups/${group.id}/overview` },
    { id: 'membership', label: 'Membership', path: `/copy-groups/${group.id}/membership` },
    { id: 'routing', label: 'Routing', path: `/copy-groups/${group.id}/routing` },
    { id: 'performance', label: 'Performance', path: `/copy-groups/${group.id}/performance` },
  ];

  const fillRate = group.notifications24h
    ? ((group.fills24h / group.notifications24h) * 100).toFixed(1)
    : '0.0';

  return (
    <div className="copy-groups-detail">
      <header className="copy-groups-detail__header">
        <div>
          <h1>{group.name}</h1>
          <p>
            {group.environment} · {group.members} members · {group.tradeAgents} trade agents
          </p>
        </div>
        <div className="copy-groups-detail__stats">
          <div>
            <span>Notifications</span>
            <strong>{group.notifications24h.toLocaleString()}</strong>
          </div>
          <div>
            <span>Fills</span>
            <strong>{group.fills24h.toLocaleString()}</strong>
          </div>
          <div>
            <span>Fill rate</span>
            <strong>{fillRate}%</strong>
          </div>
          <div>
            <span>P&L (24h)</span>
            <strong>${group.pnl24h.toLocaleString()}</strong>
          </div>
        </div>
      </header>

      <nav className="copy-groups-detail__tabs" aria-label="Copy group tabs">
        {tabs.map((tab) => (
          <NavLink
            key={tab.id}
            to={{ pathname: tab.path, search }}
            className={({ isActive }) =>
              `copy-groups-detail__tab ${isActive ? 'copy-groups-detail__tab--active' : ''}`
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

export function CopyGroupOverview() {
  const { group } = useCopyGroupDetailContext();

  return (
    <section className="copy-groups-detail__panel" aria-label="Copy group overview">
      <article>
        <h2>Operational summary</h2>
        <p>
          The group is {group.status} with {group.tradeAgents} trade agents synchronized across{' '}
          {group.members} downstream accounts. Notification fan-out and fills are tracked to ensure
          healthy conversion.
        </p>
      </article>
      <article>
        <h3>Last incident</h3>
        <p>No incidents recorded in the last 48 hours.</p>
      </article>
      <article>
        <h3>Recent changes</h3>
        <ul>
          <li>
            Routing weights adjusted {format(new Date('2024-04-22T06:00:00Z'), 'MMM d HH:mm')}.
          </li>
          <li>Latency guard tightened to 2.2s.</li>
        </ul>
      </article>
    </section>
  );
}

export function CopyGroupMembership() {
  const { members } = useCopyGroupDetailContext();

  return (
    <section className="copy-groups-detail__panel" aria-label="Copy group membership">
      <table>
        <thead>
          <tr>
            <th scope="col">Name</th>
            <th scope="col">Role</th>
            <th scope="col">Status</th>
            <th scope="col">7d P&L</th>
          </tr>
        </thead>
        <tbody>
          {members.map((member) => (
            <tr key={member.id}>
              <th scope="row">{member.name}</th>
              <td>{member.role}</td>
              <td>{member.status}</td>
              <td>${member.pnl7d.toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

export function CopyGroupRouting() {
  const { routes } = useCopyGroupDetailContext();

  return (
    <section className="copy-groups-detail__panel" aria-label="Routing destinations">
      <table>
        <thead>
          <tr>
            <th scope="col">Destination</th>
            <th scope="col">Weight</th>
            <th scope="col">Status</th>
          </tr>
        </thead>
        <tbody>
          {routes.map((route) => (
            <tr key={route.id}>
              <th scope="row">{route.destination}</th>
              <td>{route.weight}%</td>
              <td
                className={`copy-groups-detail__status copy-groups-detail__status--${route.status}`}
              >
                {route.status}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

export function CopyGroupPerformance() {
  const { performance } = useCopyGroupDetailContext();

  return (
    <section className="copy-groups-detail__panel" aria-label="Copy group performance">
      <table>
        <thead>
          <tr>
            <th scope="col">Trade agent</th>
            <th scope="col">Notifications</th>
            <th scope="col">Fills</th>
            <th scope="col">Fill %</th>
            <th scope="col">P&L</th>
            <th scope="col">Win rate</th>
            <th scope="col">Latency</th>
          </tr>
        </thead>
        <tbody>
          {performance.map((row) => {
            const fillRate = row.notifications
              ? ((row.fills / row.notifications) * 100).toFixed(1)
              : '0.0';
            return (
              <tr key={row.agentId}>
                <th scope="row">{row.agentName}</th>
                <td>{row.notifications.toLocaleString()}</td>
                <td>{row.fills.toLocaleString()}</td>
                <td>{fillRate}%</td>
                <td>${row.pnl.toLocaleString()}</td>
                <td>{row.winRate}%</td>
                <td>{row.latencyMs} ms</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </section>
  );
}
