import { expectManagementOk, managementRequest } from './config.ts';
import type { TradeCommand } from './enqueueExpertAdvisorTradeOrder.ts';

export interface CopyTradeGroupMemberReadModel {
  memberId: string;
  role: 'leader' | 'follower';
  riskStrategy: string;
  allocation: number;
  updatedAt: string;
  updatedBy: string;
}

export interface CopyTradeGroupReadModel {
  tenantId: string;
  groupId: string;
  name: string;
  description?: string | null;
  createdBy: string;
  createdAt: string;
  members: CopyTradeGroupMemberReadModel[];
}

export interface CreateCopyTradeGroupInput {
  groupId: string;
  name: string;
  description?: string;
  requestedBy: string;
}

export async function createCopyTradeGroup(
  input: CreateCopyTradeGroupInput,
): Promise<CopyTradeGroupReadModel> {
  const response = await managementRequest('/admin/copy-trade/groups', {
    method: 'POST',
    body: JSON.stringify({
      groupId: input.groupId,
      name: input.name,
      description: input.description,
      requestedBy: input.requestedBy,
    }),
  });
  await expectManagementOk(response);
  return (await response.json()) as CopyTradeGroupReadModel;
}

export interface UpsertCopyTradeGroupMemberInput {
  groupId: string;
  memberId: string;
  role: string;
  riskStrategy: string;
  allocation: number;
  requestedBy: string;
}

export async function upsertCopyTradeGroupMember(
  input: UpsertCopyTradeGroupMemberInput,
): Promise<CopyTradeGroupReadModel> {
  const response = await managementRequest(
    `/admin/copy-trade/groups/${encodeURIComponent(input.groupId)}/members/${encodeURIComponent(input.memberId)}`,
    {
      method: 'PUT',
      body: JSON.stringify({
        role: input.role,
        riskStrategy: input.riskStrategy,
        allocation: input.allocation,
        requestedBy: input.requestedBy,
      }),
    },
  );
  await expectManagementOk(response);
  return (await response.json()) as CopyTradeGroupReadModel;
}

export async function getCopyTradeGroup(groupId: string): Promise<CopyTradeGroupReadModel> {
  const response = await managementRequest(
    `/admin/copy-trade/groups/${encodeURIComponent(groupId)}`,
  );
  await expectManagementOk(response);
  return (await response.json()) as CopyTradeGroupReadModel;
}

export interface ExecuteCopyTradeOrderInput {
  groupId: string;
  sourceAccount: string;
  initiatedBy?: string;
  command: TradeCommand;
}

export async function executeCopyTradeOrder(input: ExecuteCopyTradeOrderInput): Promise<void> {
  const body: Record<string, unknown> = {
    sourceAccount: input.sourceAccount,
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
  if (input.initiatedBy) body.initiatedBy = input.initiatedBy;

  const response = await managementRequest(
    `/admin/copy-trade/groups/${encodeURIComponent(input.groupId)}/orders`,
    {
      method: 'POST',
      body: JSON.stringify(body),
    },
  );
  await expectManagementOk(response);
}
