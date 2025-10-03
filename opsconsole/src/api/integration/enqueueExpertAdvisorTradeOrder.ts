import { expectManagementOk, managementRequest } from './config.ts';

export interface TradeCommand {
  commandType: string;
  instrument: string;
  orderType?: string;
  side?: string;
  volume?: number;
  price?: number;
  stopLoss?: number;
  takeProfit?: number;
  timeInForce?: string;
  positionId?: string;
  clientOrderId?: string;
  metadata?: Record<string, unknown>;
}

export interface EnqueueExpertAdvisorTradeOrderInput {
  expertAdvisorId: string;
  sessionId: string;
  accountId: string;
  command: TradeCommand;
}

export async function enqueueExpertAdvisorTradeOrder(
  input: EnqueueExpertAdvisorTradeOrderInput,
): Promise<void> {
  const body: Record<string, unknown> = {
    accountId: input.accountId,
    commandType: input.command.commandType,
    instrument: input.command.instrument,
  };

  if (input.command.orderType) body.orderType = input.command.orderType;
  if (input.command.side) body.side = input.command.side;
  if (input.command.volume !== undefined) body.volume = input.command.volume;
  if (input.command.price !== undefined) body.price = input.command.price;
  if (input.command.stopLoss !== undefined) body.stopLoss = input.command.stopLoss;
  if (input.command.takeProfit !== undefined) body.takeProfit = input.command.takeProfit;
  if (input.command.timeInForce) body.timeInForce = input.command.timeInForce;
  if (input.command.positionId) body.positionId = input.command.positionId;
  if (input.command.clientOrderId) body.clientOrderId = input.command.clientOrderId;
  if (input.command.metadata) body.metadata = input.command.metadata;

  const response = await managementRequest(
    `/admin/experts/${encodeURIComponent(input.expertAdvisorId)}/sessions/${encodeURIComponent(input.sessionId)}/trade-orders`,
    {
      method: 'POST',
      body: JSON.stringify(body),
    },
  );
  await expectManagementOk(response);
}
