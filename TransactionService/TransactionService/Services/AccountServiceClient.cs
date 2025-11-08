using System.Text.Json;

namespace TransactionService.Services;

public class AccountServiceClient : IAccountServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AccountServiceClient> _logger;

    public AccountServiceClient(HttpClient httpClient, ILogger<AccountServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AccountDto?> GetAccountAsync(string accountId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/accounts/{accountId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Account {accountId} not found");
                    return null;
                }
                
                _logger.LogError($"Failed to get account {accountId}. Status: {response.StatusCode}");
                throw new HttpRequestException($"Account service returned {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var account = JsonSerializer.Deserialize<AccountDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return account;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Network error calling Account Service for account {accountId}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error getting account {accountId}");
            throw;
        }
    }
}
