import type { Meta, StoryObj } from '@storybook/react';
import { expect, userEvent, within } from '@storybook/test';
import { MemoryRouter } from 'react-router-dom';
import { DashboardStatistics } from './DashboardStatistics';

const meta: Meta<typeof DashboardStatistics> = {
  component: DashboardStatistics,
  title: 'Dashboard/DashboardStatistics',
  decorators: [
    (Story) => (
      <MemoryRouter initialEntries={['/dashboard/statistics']}>
        <Story />
      </MemoryRouter>
    ),
  ],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByRole('table')).toBeInTheDocument();
    const timeframeButton = canvas.getByRole('button', { name: /7 days/i });
    await userEvent.click(timeframeButton);
    expect(timeframeButton).toHaveAttribute('aria-pressed', 'true');
  },
};
