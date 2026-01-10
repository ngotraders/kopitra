using System.Net;
using System.Text.Json;
using Kopitra.Api.Domain;
using Kopitra.Api.Models;
using EventFlow.EntityFramework;
using Microsoft.EntityFrameworkCore;
using WorkerHttpFake;

namespace Kopitra.Api.Tests;

[TestClass]
public class ExpertAdvisorFunctionsTests
{
    [TestMethod]
    public async Task CreateSession_ReturnsExpectedResponse()
    {
        // Arrange
        using var serviceProvider = TestServiceProvider.CreateProvider();
        var function = serviceProvider.GetRequiredService<Functions.ExpertAdvisorFunctions>();
        var contextProvider = serviceProvider.GetRequiredService<IDbContextProvider<KopitraDbContext>>();

        var request = new HttpRequestDataBuilder()
            .WithUrl("http://localhost/api/ea/sessions")
            .WithMethod(HttpMethod.Post)
            .WithBody(JsonSerializer.Serialize(
                new SessionCreateRequest()
                {
                    UserId = "user123",
                    AccountId = "account456"
                }))
            .Build();

        // Act
        var response = await function.CreateSession(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        
        // Verify data in database instead of relying on response body
        using (var context = contextProvider.CreateContext())
        {
            var sessions = await context.ExpertAdvisorSessions.ToListAsync();
            Assert.AreEqual(1, sessions.Count);
            var session = sessions.First();
            Assert.AreEqual("user123", session.UserId);
            Assert.AreEqual("account456", session.AccountId);
            Assert.AreEqual(1, session.State);
        }
    }

    [TestMethod]
    public async Task GetSessions_ReturnsExpectedResponse()
    {
        // Arrange
        using var serviceProvider = TestServiceProvider.CreateProvider();
        var function = serviceProvider.GetRequiredService<Functions.ExpertAdvisorFunctions>();

        var request = new HttpRequestDataBuilder()
            .WithUrl("http://localhost/api/ea/sessions")
            .WithMethod(HttpMethod.Get)
            .Build();

        // Act
        var response = await function.GetSessions(request, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        // Verify empty list
        var contextProvider = serviceProvider.GetRequiredService<IDbContextProvider<KopitraDbContext>>();
        using (var context = contextProvider.CreateContext())
        {
            var sessions = await context.ExpertAdvisorSessions.ToListAsync();
            Assert.AreEqual(0, sessions.Count);
        }
    }

    [TestMethod]
    public async Task AuthenticateSession_ReturnsOkResponse()
    {
        // Arrange
        using var serviceProvider = TestServiceProvider.CreateProvider();
        var function = serviceProvider.GetRequiredService<Functions.ExpertAdvisorFunctions>();
        var contextProvider = serviceProvider.GetRequiredService<IDbContextProvider<KopitraDbContext>>();

        // First create a session
        var createRequest = new HttpRequestDataBuilder()
            .WithUrl("http://localhost/api/ea/sessions")
            .WithMethod(HttpMethod.Post)
            .WithBody(JsonSerializer.Serialize(
                new SessionCreateRequest()
                {
                    UserId = "user123",
                    AccountId = "account456"
                }))
            .Build();

        var createResponse = await function.CreateSession(createRequest, CancellationToken.None).ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        
        // Get session ID from database
        string? sessionId;
        using (var context = contextProvider.CreateContext())
        {
            var session = await context.ExpertAdvisorSessions.FirstAsync();
            sessionId = session.Id;
        }
        
        // Then authenticate it
        var authRequest = new HttpRequestDataBuilder()
            .WithUrl($"http://localhost/api/ea/sessions/{sessionId}/authenticate")
            .WithMethod(HttpMethod.Post)
            .Build();

        // Act
        var response = await function.AuthenticateSession(authRequest, sessionId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using (var context = contextProvider.CreateContext())
        {
            var session = await context.ExpertAdvisorSessions.FirstAsync();
            Assert.AreEqual(2, session.State); // Authenticated state
            Assert.IsNotNull(session.JwtToken);
        }
    }

    [TestMethod]
    public async Task Heartbeat_ReturnsOkResponse()
    {
        // Arrange
        using var serviceProvider = TestServiceProvider.CreateProvider();
        var function = serviceProvider.GetRequiredService<Functions.ExpertAdvisorFunctions>();
        var contextProvider = serviceProvider.GetRequiredService<IDbContextProvider<KopitraDbContext>>();

        // First create a session
        var createRequest = new HttpRequestDataBuilder()
            .WithUrl("http://localhost/api/ea/sessions")
            .WithMethod(HttpMethod.Post)
            .WithBody(JsonSerializer.Serialize(
                new SessionCreateRequest()
                {
                    UserId = "user123",
                    AccountId = "account456"
                }))
            .Build();

        var createResponse = await function.CreateSession(createRequest, CancellationToken.None).ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        
        // Get session ID from database
        string? sessionId;
        using (var context = contextProvider.CreateContext())
        {
            var session = await context.ExpertAdvisorSessions.FirstAsync();
            sessionId = session.Id;
        }

        // Send heartbeat
        var heartbeatRequest = new HttpRequestDataBuilder()
            .WithUrl("http://localhost/api/ea/heartbeat")
            .WithMethod(HttpMethod.Post)
            .WithBody(JsonSerializer.Serialize(
                new HeartbeatRequest()
                {
                    SessionId = sessionId
                }))
            .Build();

        // Act
        var response = await function.Heartbeat(heartbeatRequest, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using (var context = contextProvider.CreateContext())
        {
            var session = await context.ExpertAdvisorSessions.FirstAsync();
            Assert.IsNotNull(session.LastHeartbeatAt);
        }
    }
}
