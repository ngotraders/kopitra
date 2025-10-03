import { expectManagementOk, managementRequest } from './config.ts';

export interface ApproveExpertAdvisorSessionInput {
  expertAdvisorId: string;
  sessionId: string;
  accountId: string;
  authKeyFingerprint: string;
  approvedBy?: string;
  expiresAt?: string;
}

export async function approveExpertAdvisorSession(
  input: ApproveExpertAdvisorSessionInput,
): Promise<void> {
  const response = await managementRequest(
    `/admin/experts/${encodeURIComponent(input.expertAdvisorId)}/sessions/${encodeURIComponent(input.sessionId)}/approve`,
    {
      method: 'POST',
      body: JSON.stringify({
        accountId: input.accountId,
        authKeyFingerprint: input.authKeyFingerprint,
        approvedBy: input.approvedBy,
        expiresAt: input.expiresAt,
      }),
    },
  );
  await expectManagementOk(response);
}
