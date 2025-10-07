import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { QueryClientProvider } from '@tanstack/react-query';
import { AuthContext, type AuthContextValue } from '../../contexts/auth-context.ts';
import { createTestQueryClient } from '../../test/queryClient.ts';
import LoginPage from './LoginPage';

describe('LoginPage', () => {
  it('invokes signIn with trimmed credentials', async () => {
    const signIn = vi.fn().mockResolvedValue(undefined);
    const authContext: AuthContextValue = {
      user: { id: '', name: '', email: '', roles: [] },
      isAuthenticated: false,
      isLoading: false,
      hasRole: () => false,
      hasAnyRole: () => false,
      signIn,
      signOut: vi.fn(),
    };

    const queryClient = createTestQueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/login']}>
          <AuthContext.Provider value={authContext}>
            <LoginPage />
          </AuthContext.Provider>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await userEvent.type(screen.getByTestId('login-userId'), ' operator-7 ');
    await userEvent.type(screen.getByTestId('login-email'), ' operator@example.com ');
    await userEvent.click(screen.getByTestId('login-submit'));

    expect(signIn).toHaveBeenCalledWith({ userId: 'operator-7', email: 'operator@example.com' });
  });

  it('shows a validation error when inputs are missing', async () => {
    const signIn = vi.fn().mockResolvedValue(undefined);
    const authContext: AuthContextValue = {
      user: { id: '', name: '', email: '', roles: [] },
      isAuthenticated: false,
      isLoading: false,
      hasRole: () => false,
      hasAnyRole: () => false,
      signIn,
      signOut: vi.fn(),
    };

    const queryClient = createTestQueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/login']}>
          <AuthContext.Provider value={authContext}>
            <LoginPage />
          </AuthContext.Provider>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await userEvent.click(screen.getByTestId('login-submit'));

    const error = await screen.findByTestId('login-error');
    expect(error).toHaveTextContent(/enter your operator id/i);
    expect(signIn).not.toHaveBeenCalled();
  });
});
