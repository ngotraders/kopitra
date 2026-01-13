using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Queries
{
    /// <summary>
    /// Read model for Account queries
    /// </summary>
    public class AccountReadModel
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public int BrokerType { get; set; }
        public string BrokerName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string ServerName { get; set; } = null!;
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public decimal Balance { get; set; } = 0;
        public bool IsConnected { get; set; } = false;
        public int ConnectionStatus { get; set; } = (int)Domain.Accounts.ConnectionStatus.NotConnected;
        public DateTime? LastSyncedAt { get; set; }
        public DateTime? LastVerifiedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
