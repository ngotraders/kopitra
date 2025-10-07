import type { Meta, StoryObj } from '@storybook/react';
import { expect, fn, within } from '@storybook/test';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthContext, type AuthContextValue } from '../contexts/auth-context.ts';
import { withQueryClient } from '../test/withQueryClient.tsx';
import { RequireAuth } from './RequireAuth.tsx';

function createAuthContextValue(overrides: Partial<AuthContextValue>): AuthContextValue {
  return {
    user: { id: 'storybook', name: 'Story Auth', email: 'auth@example.com', roles: [] },
    isAuthenticated: true,
    isLoading: false,
    hasRole: () => true,
    hasAnyRole: () => true,
    signIn: async () => undefined,
    signOut: () => undefined,
    ...overrides,
  };
}

const meta: Meta<typeof RequireAuth> = {
  component: RequireAuth,
  title: 'Routes/RequireAuth',
  decorators: [
    withQueryClient,
    (Story, { parameters }) => {
      const authValue: AuthContextValue = parameters?.auth ?? createAuthContextValue({});
      return (
        <MemoryRouter initialEntries={['/protected']}>
          <AuthContext.Provider value={authValue}>
            <Routes>
              <Route element={<Story />}>
                <Route path="/protected" element={<div>Protected Area</div>} />
              </Route>
              <Route path="/login" element={<div>Login Page</div>} />
            </Routes>
          </AuthContext.Provider>
        </MemoryRouter>
      );
    },
  ],
  parameters: {
    auth: createAuthContextValue({}),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.findByText('Protected Area')).resolves.toBeInTheDocument();
  },
};

export const RedirectsToLogin: Story = {
  parameters: {
    auth: createAuthContextValue({
      isAuthenticated: false,
      hasRole: () => false,
      hasAnyRole: () => false,
    }),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.findByText('Login Page')).resolves.toBeInTheDocument();
  },
};

export const LoadingState: Story = {
  parameters: {
    auth: createAuthContextValue({
      isAuthenticated: false,
      isLoading: true,
      hasRole: () => false,
      hasAnyRole: () => false,
      signIn: fn(),
      signOut: fn(),
    }),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.findByRole('status')).resolves.toHaveTextContent(/verifying session/i);
  },
};
