import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { QueryClientProvider } from '@tanstack/react-query';
import { AuthContext, type AuthContextValue } from '../contexts/auth-context.ts';
import { createTestQueryClient } from '../test/queryClient.ts';
import { RequireAuth } from './RequireAuth.tsx';

describe('RequireAuth', () => {
  it('redirects unauthenticated users to the login page', () => {
    const authContext: AuthContextValue = {
      user: { id: '', name: '', email: '', roles: [] },
      isAuthenticated: false,
      isLoading: false,
      hasRole: () => false,
      hasAnyRole: () => false,
      signIn: vi.fn(),
      signOut: vi.fn(),
    };

    render(
      <QueryClientProvider client={createTestQueryClient()}>
        <MemoryRouter initialEntries={['/protected']}>
          <AuthContext.Provider value={authContext}>
            <Routes>
              <Route element={<RequireAuth />}>
                <Route path="/protected" element={<div>Protected</div>} />
              </Route>
              <Route path="/login" element={<div>Login</div>} />
            </Routes>
          </AuthContext.Provider>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByText('Login')).toBeInTheDocument();
  });

  it('shows a loading indicator while verifying authentication', () => {
    const authContext: AuthContextValue = {
      user: { id: '', name: '', email: '', roles: [] },
      isAuthenticated: false,
      isLoading: true,
      hasRole: () => false,
      hasAnyRole: () => false,
      signIn: vi.fn(),
      signOut: vi.fn(),
    };

    render(
      <QueryClientProvider client={createTestQueryClient()}>
        <MemoryRouter initialEntries={['/protected']}>
          <AuthContext.Provider value={authContext}>
            <Routes>
              <Route element={<RequireAuth />}>
                <Route path="/protected" element={<div>Protected</div>} />
              </Route>
            </Routes>
          </AuthContext.Provider>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    expect(screen.getByRole('status')).toHaveTextContent(/verifying session/i);
  });
});
