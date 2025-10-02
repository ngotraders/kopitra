import { formatDistanceToNow } from 'date-fns';
import { userActivity, users } from '../../data/console.ts';
import type { UserActivityEvent, UserRecord } from '../../types/console.ts';
import { Link, NavLink, Outlet, useOutletContext, useParams } from 'react-router-dom';
import './AdminUsers.css';

interface AdminUserContext {
  user: UserRecord;
  activity: UserActivityEvent[];
}

function useAdminUserContext() {
  return useOutletContext<AdminUserContext>();
}

export function AdminUsersList() {
  return (
    <div className="admin-users">
      <header className="admin-users__header">
        <h1>Users</h1>
        <p>Manage console access, permissions, and audit activity.</p>
      </header>
      <table>
        <thead>
          <tr>
            <th scope="col">Name</th>
            <th scope="col">Email</th>
            <th scope="col">Role</th>
            <th scope="col">Status</th>
            <th scope="col">Last active</th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={user.id}>
              <th scope="row">
                <Link to={`/admin/users/${user.id}?environment=sandbox`}>{user.name}</Link>
              </th>
              <td>{user.email}</td>
              <td>{user.role}</td>
              <td>{user.status}</td>
              <td>{formatDistanceToNow(new Date(user.lastActive), { addSuffix: true })}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function AdminUserDetailLayout() {
  const { userId = '' } = useParams();
  const user = users.find((item) => item.id === userId);

  if (!user) {
    return (
      <div className="admin-users__empty">
        <h2>User not found</h2>
        <p>The requested user record could not be located.</p>
        <Link to="/admin/users">Back to users</Link>
      </div>
    );
  }

  const contextValue: AdminUserContext = {
    user,
    activity: userActivity[user.id] ?? [],
  };

  const tabs = [
    { id: 'overview', label: 'Overview', path: `/admin/users/${user.id}/overview` },
    { id: 'permissions', label: 'Permissions', path: `/admin/users/${user.id}/permissions` },
    { id: 'activity', label: 'Activity', path: `/admin/users/${user.id}/activity` },
  ];

  return (
    <div className="admin-users-detail">
      <header className="admin-users-detail__header">
        <div>
          <h1>{user.name}</h1>
          <p>
            {user.role} · {user.status} · Last active{' '}
            {formatDistanceToNow(new Date(user.lastActive), { addSuffix: true })}
          </p>
        </div>
      </header>

      <nav className="admin-users-detail__tabs" aria-label="User tabs">
        {tabs.map((tab) => (
          <NavLink
            key={tab.id}
            to={{ pathname: tab.path, search: '?environment=sandbox' }}
            className={({ isActive }) =>
              `admin-users-detail__tab ${isActive ? 'admin-users-detail__tab--active' : ''}`
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

export function AdminUserOverview() {
  const { user } = useAdminUserContext();

  return (
    <section className="admin-users-detail__panel" aria-label="User overview">
      <dl className="admin-users-detail__grid">
        <div>
          <dt>Email</dt>
          <dd>{user.email}</dd>
        </div>
        <div>
          <dt>Role</dt>
          <dd>{user.role}</dd>
        </div>
        <div>
          <dt>Status</dt>
          <dd>{user.status}</dd>
        </div>
        <div>
          <dt>Last active</dt>
          <dd>{new Date(user.lastActive).toLocaleString()}</dd>
        </div>
      </dl>
      <p>
        Account records are synchronized with the central identity provider. Invite new users from
        the list view to maintain least-privilege access.
      </p>
    </section>
  );
}

export function AdminUserPermissions() {
  const { user } = useAdminUserContext();

  return (
    <section className="admin-users-detail__panel" aria-label="User permissions">
      <h2>Permission matrix</h2>
      <ul>
        <li>Role: {user.role}</li>
        <li>Copy group access: APAC Momentum, LATAM Swing</li>
        <li>Trade agent commands: Enabled</li>
      </ul>
    </section>
  );
}

export function AdminUserActivity() {
  const { activity } = useAdminUserContext();

  return (
    <section className="admin-users-detail__panel" aria-label="User activity">
      <table>
        <thead>
          <tr>
            <th scope="col">Timestamp</th>
            <th scope="col">Action</th>
            <th scope="col">IP</th>
          </tr>
        </thead>
        <tbody>
          {activity.map((event) => (
            <tr key={event.id}>
              <th scope="row">{new Date(event.timestamp).toLocaleString()}</th>
              <td>{event.action}</td>
              <td>{event.ip}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
