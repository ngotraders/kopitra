import { copyTradePerformanceAggregates } from '../data/console.ts';
import type { CopyTradePerformanceAggregate } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchCopyTradePerformanceAggregates(): Promise<
  CopyTradePerformanceAggregate[]
> {
  return clone(copyTradePerformanceAggregates);
}
