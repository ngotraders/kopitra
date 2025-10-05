import type { CopyTradeFunnelStage } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchCopyTradeFunnel(): Promise<CopyTradeFunnelStage[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.copyTradeFunnelStages;
}
