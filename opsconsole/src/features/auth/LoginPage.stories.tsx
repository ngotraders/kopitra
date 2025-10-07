import type { Meta, StoryObj } from '@storybook/react';
import { expect, fn, userEvent, within } from '@storybook/test';
import { MemoryRouter } from 'react-router-dom';
import { AuthContext, type AuthContextValue } from '../../contexts/auth-context.ts';
import { withQueryClient } from '../../test/withQueryClient.tsx';
import LoginPage from './LoginPage';

const meta: Meta<typeof LoginPage> = {
  component: LoginPage,
  title: 'Auth/LoginPage',
  decorators: [
    withQueryClient,
    (Story, { parameters }) => {
      const signInMock = parameters?.signInMock ?? fn();
      const contextValue: AuthContextValue = {
        user: { id: '', name: '', email: '', roles: [] },
        isAuthenticated: false,
        isLoading: false,
        hasRole: () => false,
        hasAnyRole: () => false,
        signIn: signInMock,
        signOut: () => undefined,
      };

      return (
        <MemoryRouter initialEntries={['/login']}>
          <AuthContext.Provider value={contextValue}>
            <Story />
          </AuthContext.Provider>
        </MemoryRouter>
      );
    },
  ],
  parameters: {
    signInMock: fn(),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement, parameters }) => {
    const canvas = within(canvasElement);
    const emailInput = await canvas.findByTestId('login-email');
    const passwordInput = await canvas.findByTestId('login-password');
    const submitButton = await canvas.findByTestId('login-submit');

    await userEvent.type(emailInput, 'operator@example.com');
    await userEvent.type(passwordInput, 'pw123456');
    await userEvent.click(submitButton);

    expect(parameters.signInMock).toHaveBeenCalledWith({
      email: 'operator@example.com',
      password: 'pw123456',
    });
  },
};
