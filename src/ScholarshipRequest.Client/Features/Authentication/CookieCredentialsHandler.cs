using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace ScholarshipRequest.Client.Features.Authentication;

public sealed class CookieCredentialsHandler(HttpMessageHandler innerHandler)
    : DelegatingHandler(innerHandler)
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        return base.SendAsync(request, cancellationToken);
    }
}
