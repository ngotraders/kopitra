import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { AuthProvider } from '../contexts';
import { RequireRoles } from './RequireRoles.tsx';

describe('RequireRoles', () => {
  it('redirects unauthorized users to the fallback route', () => {
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AuthProvider
          user={{ id: 'user-1', name: 'Operator', email: 'op@example.com', roles: ['operator'] }}
        >
          <Routes>
            <Route element={<RequireRoles roles={['admin']} />}>
              <Route path="/admin/users" element={<div>Admin Users</div>} />
            </Route>
            <Route path="/operations/overview" element={<div>Operations Overview</div>} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>,
    );

    expect(screen.getByText(/Operations Overview/)).toBeInTheDocument();
  });

  it('renders protected content for authorized users', () => {
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AuthProvider
          user={{ id: 'user-2', name: 'Admin', email: 'admin@example.com', roles: ['admin'] }}
        >
          <Routes>
            <Route element={<RequireRoles roles={['admin']} />}>
              <Route path="/admin/users" element={<div>Admin Users</div>} />
            </Route>
            <Route path="/operations/overview" element={<div>Operations Overview</div>} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>,
    );

    expect(screen.getByText(/Admin Users/)).toBeInTheDocument();
  });
});
