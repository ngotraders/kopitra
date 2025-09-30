import type { Activity, NavigationItem, StatMetric } from '../types/dashboard.ts';

export const navigationItems: NavigationItem[] = [
  { id: 'overview', label: 'Overview', href: '#' },
  { id: 'signals', label: 'Signals', href: '#signals', badge: '12' },
  { id: 'accounts', label: 'Accounts', href: '#accounts' },
  { id: 'compliance', label: 'Compliance', href: '#compliance', badge: '2' },
  { id: 'settings', label: 'Settings', href: '#settings' },
];

export const statMetrics: StatMetric[] = [
  {
    id: 'copy-rate',
    label: 'Copy Success Rate',
    value: '98.6%',
    delta: 2.1,
    description: 'Successful downstream fills in the last 24h.',
  },
  {
    id: 'latency',
    label: 'Median Latency',
    value: '184 ms',
    delta: -8.5,
    description: 'Median replication latency across all accounts.',
  },
  {
    id: 'risk-flags',
    label: 'Risk Flags',
    value: '4',
    delta: 1.0,
    description: 'Open guardrails requiring review.',
  },
  {
    id: 'sandbox-usage',
    label: 'Sandbox Usage',
    value: '32%',
    delta: 4.6,
    description: 'Signals routed through paper trading environments.',
  },
];

export const activities: Activity[] = [
  {
    id: 'act-1',
    timestamp: '2024-04-22T09:15:00Z',
    user: 'Alex Morgan',
    action: 'Promoted strategy "Momentum" to production',
    status: 'success',
    target: 'Strategy Desk',
  },
  {
    id: 'act-2',
    timestamp: '2024-04-22T08:52:00Z',
    user: 'Jordan Mills',
    action: 'Paused replication for EU accounts',
    status: 'warning',
    target: 'Account Group EU-22',
  },
  {
    id: 'act-3',
    timestamp: '2024-04-22T08:20:00Z',
    user: 'Samira Lee',
    action: 'Updated broker credentials',
    status: 'success',
    target: 'Prime Broker API',
  },
  {
    id: 'act-4',
    timestamp: '2024-04-22T07:55:00Z',
    user: 'Compliance Bot',
    action: 'Flagged 3 trades breaching exposure limit',
    status: 'error',
    target: 'Risk Guardrail',
  },
  {
    id: 'act-5',
    timestamp: '2024-04-22T07:12:00Z',
    user: 'Ops Automations',
    action: 'Rolled over futures contracts',
    status: 'success',
    target: 'CME Desk',
  },
];
