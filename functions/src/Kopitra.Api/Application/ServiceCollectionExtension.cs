using EventFlow.EntityFramework;
using EventFlow.EntityFramework.Extensions;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Application;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddKopitra<TDbContextProvider>(this IServiceCollection services)
        where TDbContextProvider : class, IDbContextProvider<KopitraDbContext>
    {
        return services.AddEventFlow(ef =>
        {
            // Users
            ef.AddEvents(
                typeof(Domain.Users.Events.UserAdminImpersonationStartedEvent),
                typeof(Domain.Users.Events.UserDeactivatedEvent),
                typeof(Domain.Users.Events.UserInfoUpdatedEvent),
                typeof(Domain.Users.Events.UserLoginEvent),
                typeof(Domain.Users.Events.UserPermissionsChangedEvent),
                typeof(Domain.Users.Events.UserProviderRoleDisabledEvent),
                typeof(Domain.Users.Events.UserProviderRoleEnabledEvent),
                typeof(Domain.Users.Events.UserReactivatedEvent),
                typeof(Domain.Users.Events.UserRefreshTokenIssuedEvent),
                typeof(Domain.Users.Events.UserRefreshTokenRevokedEvent),
                typeof(Domain.Users.Events.UserRegisteredEvent),
                typeof(Domain.Users.Events.UserSettingsUpdatedEvent),
                typeof(Domain.Users.Events.UserSubscriberAccountCreatedEvent)
            );
            ef.AddCommands(
                typeof(Users.Commands.ChangeUserPermissionsCommand),
                typeof(Users.Commands.DeactivateUserCommand),
                typeof(Users.Commands.EnableProviderCommand),
                typeof(Users.Commands.IssueRefreshTokenCommand),
                typeof(Users.Commands.ReactivateUserCommand),
                typeof(Users.Commands.RecordUserLoginCommand),
                typeof(Users.Commands.RegisterUserCommand),
                typeof(Users.Commands.RevokeRefreshTokenCommand),
                typeof(Users.Commands.UpdateUserInfoCommand),
                typeof(Users.Commands.UpdateUserSettingsCommand)
            );
            ef.AddCommandHandlers(
                typeof(Users.Commands.ChangeUserPermissionsCommandHandler),
                typeof(Users.Commands.DeactivateUserCommandHandler),
                typeof(Users.Commands.EnableProviderCommandHandler),
                typeof(Users.Commands.IssueRefreshTokenCommandHandler),
                typeof(Users.Commands.ReactivateUserCommandHandler),
                typeof(Users.Commands.RecordUserLoginCommandHandler),
                typeof(Users.Commands.RegisterUserCommandHandler),
                typeof(Users.Commands.RevokeRefreshTokenCommandHandler),
                typeof(Users.Commands.UpdateUserInfoCommandHandler),
                typeof(Users.Commands.UpdateUserSettingsCommandHandler)
            );
            ef.UseEntityFrameworkReadModel<Users.Queries.UserReadModel, KopitraDbContext>();
            ef.UseEntityFrameworkReadModel<Users.Queries.UserSessionReadModel, KopitraDbContext, Users.Queries.UserSessionReadModelLocator>();
            ef.RegisterServices(rs => rs.AddTransient<Users.Queries.UserSessionReadModelLocator>());
            ef.AddQueryHandlers(
                typeof(Users.Queries.GetAllUsersQueryHandler),
                typeof(Users.Queries.GetUserByEmailQueryHandler),
                typeof(Users.Queries.GetUserByIdQueryHandler),
                typeof(Users.Queries.GetUsersByRoleQueryHandler),
                typeof(Users.Queries.GetUserSessionByRefreshTokenQueryHandler),
                typeof(Users.Queries.GetUserSessionBySessionIdQueryHandler)
            );

            // Accounts
            ef.AddEvents(
                typeof(Domain.Accounts.Events.AccountRegisteredEvent),
                typeof(Domain.Accounts.Events.AccountConnectionVerifiedEvent),
                typeof(Domain.Accounts.Events.AccountBalanceUpdatedEvent),
                typeof(Domain.Accounts.Events.AccountInfoUpdatedEvent),
                typeof(Domain.Accounts.Events.AccountDeletedEvent),
                typeof(Domain.Accounts.Events.AccountActivationInitiatedEvent),
                typeof(Domain.Accounts.Events.AccountActivationConfirmedEvent),
                typeof(Domain.Accounts.Events.ActivationCodeGeneratedEvent),
                typeof(Domain.Accounts.Events.ActivationCodeConfirmedEvent),
                typeof(Domain.Accounts.Events.ActivationCodeExpiredEvent)
            );
            ef.AddCommands(
                typeof(Accounts.Commands.RegisterAccountCommand),
                typeof(Accounts.Commands.VerifyAccountConnectionCommand),
                typeof(Accounts.Commands.UpdateAccountBalanceCommand),
                typeof(Accounts.Commands.UpdateAccountInfoCommand),
                typeof(Accounts.Commands.DeleteAccountCommand),
                typeof(Accounts.Commands.InitiateAccountActivationCommand),
                typeof(Accounts.Commands.ConfirmAccountActivationCommand),
                typeof(Accounts.Commands.GenerateActivationCodeCommand),
                typeof(Accounts.Commands.ConfirmActivationCodeCommand),
                typeof(Accounts.Commands.ExpireActivationCodeCommand)
            );
            ef.AddCommandHandlers(
                typeof(Accounts.Commands.RegisterAccountCommandHandler),
                typeof(Accounts.Commands.VerifyAccountConnectionCommandHandler),
                typeof(Accounts.Commands.UpdateAccountBalanceCommandHandler),
                typeof(Accounts.Commands.UpdateAccountInfoCommandHandler),
                typeof(Accounts.Commands.DeleteAccountCommandHandler),
                typeof(Accounts.Commands.InitiateAccountActivationCommandHandler),
                typeof(Accounts.Commands.ConfirmAccountActivationCommandHandler),
                typeof(Accounts.Commands.GenerateActivationCodeCommandHandler),
                typeof(Accounts.Commands.ConfirmActivationCodeCommandHandler),
                typeof(Accounts.Commands.ExpireActivationCodeCommandHandler)
            );
            ef.AddQueryHandlers(
                typeof(Accounts.Queries.GetAccountsByUserIdQueryHandler),
                typeof(Accounts.Queries.GetAccountByIdQueryHandler),
                typeof(Accounts.Queries.GetActivationCodeByIdQueryHandler),
                typeof(Accounts.Queries.GetActivationCodeByCodeQueryHandler)
            );

            // Expert Advisors
            ef.AddEvents(
                typeof(Domain.ExpertAdvisors.Events.SessionCreatedEvent),
                typeof(Domain.ExpertAdvisors.Events.SessionAuthenticatedEvent),
                typeof(Domain.ExpertAdvisors.Events.HeartbeatReceivedEvent),
                typeof(Domain.ExpertAdvisors.Events.SessionClosedEvent)
            );
            ef.AddCommands(
                typeof(ExpertAdvisors.Commands.CreateSessionCommand),
                typeof(ExpertAdvisors.Commands.AuthenticateSessionCommand),
                typeof(ExpertAdvisors.Commands.RecordHeartbeatCommand),
                typeof(ExpertAdvisors.Commands.CloseSessionCommand)
            );
            ef.AddCommandHandlers(
                typeof(ExpertAdvisors.Commands.CreateSessionCommandHandler),
                typeof(ExpertAdvisors.Commands.AuthenticateSessionCommandHandler),
                typeof(ExpertAdvisors.Commands.RecordHeartbeatCommandHandler),
                typeof(ExpertAdvisors.Commands.CloseSessionCommandHandler)
            );
            ef.UseEntityFrameworkReadModel<ExpertAdvisors.Queries.ExpertAdvisorSessionReadModel, KopitraDbContext>();
            ef.AddQueryHandlers(
                typeof(ExpertAdvisors.Queries.GetSessionByIdQueryHandler),
                typeof(ExpertAdvisors.Queries.GetAllSessionsQueryHandler)
            );

            ef.ConfigureEntityFramework(EntityFrameworkConfiguration.New);
            ef.UseEntityFrameworkEventStore<KopitraDbContext>();
            ef.AddDbContextProvider<KopitraDbContext, TDbContextProvider>();
        });
    }
}
