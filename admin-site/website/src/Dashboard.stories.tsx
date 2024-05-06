import type { Meta, StoryObj } from '@storybook/react';
import { fn } from '@storybook/test';
import { AdminContext, Resource, ThemeProvider } from 'react-admin'
import fakeDataProvider from 'ra-data-fakerest';
import { i18nProvider } from './i18nProvider';
import { Dashboard } from './Dashboard';

const meta = {
    title: 'pages/Dashboard',
    component: Dashboard,
    parameters: {
        layout: 'centered',
    },
    tags: ['autodocs'],
    argTypes: {
        backgroundColor: { control: 'color' },
    },
    args: { onClick: fn() },
    decorators: [Story =>
        <ThemeProvider>
            <AdminContext
                i18nProvider={i18nProvider}
                dataProvider={fakeDataProvider({
                    accounts: [
                        { id: 1, key: 'MT4/OANDA Corporation/999999999/1', name: 'Name of this account', description: 'Description text', has_error: true },
                    ],
                }, true)}
            >
                <Resource name="Account" />
                <Story />
            </AdminContext>
        </ThemeProvider>]
} satisfies Meta<typeof Dashboard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
    args: {
        primary: true,
        label: 'Dashboard',
    },
};
