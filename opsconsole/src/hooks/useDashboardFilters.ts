import { useCallback, useEffect, useMemo, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { DashboardEnvironmentFilter, DashboardTimeframe } from '../types/console.ts';

const DEFAULT_TIMEFRAME: DashboardTimeframe = '24h';
const DEFAULT_ENVIRONMENT: DashboardEnvironmentFilter = 'production';

function isTimeframe(value: string | null): value is DashboardTimeframe {
  return value === '24h' || value === '7d' || value === '30d';
}

function isEnvironment(value: string | null): value is DashboardEnvironmentFilter {
  return value === 'production' || value === 'sandbox' || value === 'all';
}

export function useDashboardFilters() {
  const [searchParams, setSearchParams] = useSearchParams();
  const latestParamsRef = useRef(new URLSearchParams(searchParams));

  useEffect(() => {
    latestParamsRef.current = new URLSearchParams(searchParams);
  }, [searchParams]);

  const timeframe = isTimeframe(searchParams.get('timeframe'))
    ? (searchParams.get('timeframe') as DashboardTimeframe)
    : DEFAULT_TIMEFRAME;
  const environment = isEnvironment(searchParams.get('environment'))
    ? (searchParams.get('environment') as DashboardEnvironmentFilter)
    : DEFAULT_ENVIRONMENT;

  const updateParams = useCallback(
    (mutator: (params: URLSearchParams) => void) => {
      const nextParams = new URLSearchParams(latestParamsRef.current);
      mutator(nextParams);
      latestParamsRef.current = nextParams;
      setSearchParams(nextParams, { replace: true });
    },
    [setSearchParams],
  );

  const setTimeframe = useCallback(
    (nextTimeframe: DashboardTimeframe) => {
      updateParams((params) => {
        params.set('timeframe', nextTimeframe);
      });
    },
    [updateParams],
  );

  const setEnvironment = useCallback(
    (nextEnvironment: DashboardEnvironmentFilter) => {
      updateParams((params) => {
        params.set('environment', nextEnvironment);
      });
    },
    [updateParams],
  );

  return useMemo(
    () => ({ timeframe, environment, setTimeframe, setEnvironment }),
    [environment, timeframe, setEnvironment, setTimeframe],
  );
}
