using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Syncronex.Api.Features.Webhooks;

namespace Syncronex.Api.Infrastructure.ExternalServices;

public class CrmClient
{
  private readonly HttpClient _httpClient;

  public CrmClient(HttpClient httpClient)
  {
    _httpClient = httpClient;
    // Note: The BaseAddress will be configured centrally in Program.cs
  }

  public async Task<HttpResponseMessage> UpsertContactAsync(CrmUpsertPayload payload, CancellationToken cancellationToken = default)
  {
    // Simulating a POST request to the CRM API endpoint
    var response = await _httpClient.PostAsJsonAsync("/v1/contacts/upsert", payload, cancellationToken);

    // EnsureSuccessStatusCode throws an exception for HTTP 5xx or 4xx responses.
    // This exception is what triggers the Polly Retry/Circuit Breaker policies!
    response.EnsureSuccessStatusCode();

    return response;
  }
}
