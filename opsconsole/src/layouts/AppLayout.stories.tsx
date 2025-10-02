import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { expect, within } from '@storybook/test';
import { AppLayout } from './AppLayout';
import { AuthProvider } from '../contexts';
import { withQueryClient } from '../test/withQueryClient';

const meta: Meta<typeof AppLayout> = {
  component: AppLayout,
  title: 'Layout/AppLayout',
  args: {
    onSignOut: () => undefined,
  },
  decorators: [withQueryClient],
  render: (args) => (
    <MemoryRouter initialEntries={['/dashboard/activity']}>
      <AuthProvider>
        <Routes>
          <Route element={<AppLayout {...args} />}>
            <Route path="/dashboard/activity" element={<div>Dashboard Activity</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  ),
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/dashboard activity/i)).toBeInTheDocument();
    expect(canvas.getByText(/tradeagentea console/i)).toBeVisible();
  },
};

export const WithToast: Story = {
  args: {
    onSignOut: () => undefined,
  },
  render: (args) => (
    <MemoryRouter
      initialEntries={[
        {
          pathname: '/dashboard/activity',
          state: {
            toast: {
              intent: 'error',
              title: 'Access denied',
              description: 'You do not have permission to view that route.',
            },
          },
        },
      ]}
    >
      <AuthProvider>
        <Routes>
          <Route element={<AppLayout {...args} />}>
            <Route path="/dashboard/activity" element={<div>Dashboard Activity</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const toast = canvas.getByRole('status');
    expect(toast).toHaveTextContent(/access denied/i);
  },
};
