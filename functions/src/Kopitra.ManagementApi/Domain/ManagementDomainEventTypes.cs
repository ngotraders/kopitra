using System;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;

namespace Kopitra.ManagementApi.Domain;

public static class ManagementDomainEventTypes
{
    public static readonly Type[] All =
    [
        typeof(ExpertAdvisorRegistered),
        typeof(ExpertAdvisorApproved),
        typeof(ExpertAdvisorStatusChanged),
        typeof(CopyTradeGroupCreated),
        typeof(CopyTradeGroupMemberUpserted),
        typeof(CopyTradeGroupMemberRemoved),
        typeof(AdminUserProvisioned),
        typeof(AdminUserRolesUpdated),
        typeof(AdminUserNotificationSettingsUpdated)
    ];
}
