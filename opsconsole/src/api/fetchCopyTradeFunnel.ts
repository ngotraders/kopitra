import { copyTradeFunnelStages } from '../data/console.ts';
import type { CopyTradeFunnelStage } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchCopyTradeFunnel(): Promise<CopyTradeFunnelStage[]> {
  return clone(copyTradeFunnelStages);
}
