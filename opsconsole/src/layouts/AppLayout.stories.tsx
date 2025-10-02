import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { expect, within } from '@storybook/test';
import { AppLayout } from './AppLayout';

const meta: Meta<typeof AppLayout> = {
  component: AppLayout,
  title: 'Layout/AppLayout',
  args: {
    onSignOut: () => undefined,
  },
  render: (args) => (
    <MemoryRouter initialEntries={['/dashboard/activity']}>
      <Routes>
        <Route element={<AppLayout {...args} />}>
          <Route path="/dashboard/activity" element={<div>Dashboard Activity</div>} />
        </Route>
      </Routes>
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
