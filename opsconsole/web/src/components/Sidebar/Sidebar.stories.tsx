import type { Meta, StoryObj } from '@storybook/react';
import { expect, fn, userEvent, within } from '@storybook/test';
import { navigationItems } from '../../data/dashboard.ts';
import { Sidebar } from './Sidebar';

const meta: Meta<typeof Sidebar> = {
  component: Sidebar,
  title: 'Layout/Sidebar',
  args: {
    items: navigationItems,
    activeId: 'overview',
    onSelect: fn(),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const signalsButton = canvas.getByRole('button', { name: /signals/i });
    await userEvent.click(signalsButton);
    expect(args.onSelect).toHaveBeenCalledWith(
      expect.objectContaining({ id: 'signals' }),
    );
  },
};

export const ComplianceFocused: Story = {
  args: {
    activeId: 'compliance',
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByRole('button', { name: /compliance/i })).toHaveClass(
      'sidebar__item--active',
    );
  },
};
