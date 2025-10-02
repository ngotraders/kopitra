import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { expect, userEvent, within } from '@storybook/test';
import { navigationItems } from '../../data/console.ts';
import { Sidebar } from './Sidebar';

const meta: Meta<typeof Sidebar> = {
  component: Sidebar,
  title: 'Layout/Sidebar',
  args: {
    items: navigationItems,
    initialPath: '/dashboard/activity',
  },
  argTypes: {
    initialPath: {
      control: false,
      table: { disable: true },
    },
  },
  render: ({ items, initialPath }) => {
    const path = initialPath ?? '/dashboard/activity';
    return (
      <MemoryRouter initialEntries={[path]}>
        <Sidebar items={items} />
      </MemoryRouter>
    );
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const tradeAgentsLink = canvas.getByRole('link', { name: /trade agents/i });
    await userEvent.click(tradeAgentsLink);
    expect(tradeAgentsLink).toHaveAttribute('aria-current', 'page');
  },
};

export const ComplianceFocused: Story = {
  args: {
    initialPath: '/trade-agents',
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const activeLink = canvas.getByRole('link', { name: /trade agents/i });
    expect(activeLink).toHaveClass('sidebar__item--active');
  },
};
