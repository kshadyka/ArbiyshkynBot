using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ArbiyshkynBot.Infrastructure.Exchanges.Xt;

public class XtRestClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly ILogger<XtRestClient> _logger;

    public XtRestClient(HttpClient httpClient, string apiKey, string apiSecret, ILogger<XtRestClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri("https://sapi.xt.com/"); // Официальный эндпоинт XT
    }

    public async Task<string?> GetSymbolRulesAsync(string symbol)
    {
        try
        {
            var response = await _httpClient.GetAsync($"v4/public/symbol?symbol={symbol}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            
            _logger.LogError("Failed to fetch symbol rules for {Symbol}. StatusCode: {Code}", symbol, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching symbol rules for {Symbol}", symbol);
        }

        return null;
    }

    public async Task<string?> PlaceSpotOrderAsync(string symbol, string side, string type, decimal quantity, decimal price, string clientOrderId)
    {
        var endpoint = "v4/order";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        
        var payload = new Dictionary<string, object>
        {
            { "symbol", symbol },
            { "side", side.ToLower() },
            { "type", type.ToLower() },
            { "bizType", "SPOT" },
            { "quantity", quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            { "price", price.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            { "clientOrderId", clientOrderId }
        };

        var jsonBody = JsonSerializer.Serialize(payload);
        var signature = SignRequest(_apiSecret, jsonBody);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("xt-app-id", _apiKey);
        request.Headers.Add("xt-access-key", _apiKey);
        request.Headers.Add("xt-signature", signature);
        request.Headers.Add("xt-timestamp", timestamp);

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogError("Error placing order on XT.com: {Error}", errorContent);
        throw new InvalidOperationException($"Order placement failed: {errorContent}");
    }

    private string SignRequest(string secretKey, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}