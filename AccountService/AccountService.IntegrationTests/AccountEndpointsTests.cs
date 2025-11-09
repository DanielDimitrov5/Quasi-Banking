using System.Net;
using System.Net.Http.Json;
using AccountService.Models;
using FluentAssertions;

namespace AccountService.IntegrationTests;

public class AccountEndpointsTests : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactoryFixture _factory;

    public AccountEndpointsTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAccount_ValidRequest_ReturnsCreatedAccount()
    {
        // Arrange
        var request = new CreateAccountRequest(
            OwnerId: "user-123",
            OwnerEmail: "test@example.com",
            InitialDeposit: 1000m
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        account.Should().NotBeNull();
        account!.OwnerId.Should().Be("user-123");
        account.OwnerEmail.Should().Be("test@example.com");
        account.Balance.Should().Be(1000m);
        account.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAccount_ExistingAccount_ReturnsAccount()
    {
        // Arrange - Create an account first
        var createRequest = new CreateAccountRequest(
            OwnerId: "user-456",
            OwnerEmail: "get-test@example.com",
            InitialDeposit: 2000m
        );

        var createResponse = await _client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountResponse>();

        // Act
        var response = await _client.GetAsync($"/api/accounts/{createdAccount!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        account.Should().NotBeNull();
        account!.Id.Should().Be(createdAccount.Id);
        account.Balance.Should().Be(2000m);
    }

    [Fact]
    public async Task GetAccount_NonExistentAccount_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAccount_MultipleAccounts_EachHasUniqueId()
    {
        // Arrange
        var request1 = new CreateAccountRequest("user-1", "user1@example.com", 1000m);
        var request2 = new CreateAccountRequest("user-2", "user2@example.com", 2000m);

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/accounts", request1);
        var response2 = await _client.PostAsJsonAsync("/api/accounts", request2);

        var account1 = await response1.Content.ReadFromJsonAsync<AccountResponse>();
        var account2 = await response2.Content.ReadFromJsonAsync<AccountResponse>();

        // Assert
        account1!.Id.Should().NotBe(account2!.Id);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
