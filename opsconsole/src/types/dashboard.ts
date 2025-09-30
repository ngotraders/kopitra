export type ActivityStatus = 'success' | 'warning' | 'error';

export interface Activity {
  id: string;
  timestamp: string;
  user: string;
  action: string;
  status: ActivityStatus;
  target: string;
}

export interface StatMetric {
  id: string;
  label: string;
  value: string;
  delta: number;
  description: string;
}

export interface NavigationItem {
  id: string;
  label: string;
  href: string;
  badge?: string;
}
