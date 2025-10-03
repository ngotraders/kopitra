import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { expect, within } from '@storybook/test';
import { NotFound } from './NotFound';

const meta: Meta<typeof NotFound> = {
  component: NotFound,
  title: 'Feedback/NotFound',
  decorators: [
    (Story) => (
      <MemoryRouter>
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
    expect(canvas.getByText(/page not found/i)).toBeVisible();
  },
};

export const CustomMessage: Story = {
  args: {
    message: 'Access denied or resource unavailable.',
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText(/access denied/i)).toBeVisible();
  },
};
