using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Syncronex.Api.Features.Webhooks;

namespace Syncronex.Api.Infrastructure.ExternalServices;

public class AccountingClient
{
  private readonly HttpClient _httpClient;

  public AccountingClient(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public async Task<HttpResponseMessage> CreateInvoiceAsync(AccountingInvoicePayload payload, CancellationToken cancellationToken = default)
  {
    var response = await _httpClient.PostAsJsonAsync("/v3/invoices", payload, cancellationToken);

    response.EnsureSuccessStatusCode();

    return response;
  }
}
