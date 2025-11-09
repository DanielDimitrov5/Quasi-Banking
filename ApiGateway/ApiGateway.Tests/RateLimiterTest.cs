using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ApiGateway.Tests;

public class RateLimitingTests : IClassFixture<ApiGatewayFactory>
{
    private readonly ApiGatewayFactory _factory;

    public RateLimitingTests(ApiGatewayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RateLimiter_BlocksRequests_WhenLimitExceeded()
    {
        var client = _factory.CreateClient();

        // Simulate the max allowed requests (adjust as per your RateLimitingMiddleware limit)
        var maxRequests = 100;

        for (int i = 0; i < maxRequests; i++)
        {
            var response = await client.GetAsync("/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Next request should be blocked or receive 429 Too Many Requests
        var blockedResponse = await client.GetAsync("/health");
        blockedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
