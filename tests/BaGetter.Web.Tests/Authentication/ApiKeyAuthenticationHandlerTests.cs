using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using BaGetter.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace BaGetter.Web.Tests;

public class ApiKeyAuthenticationHandlerTests
{
    private readonly Mock<IApiKeyService> _apiKeyService;
    private readonly UrlEncoder _encoder;
    private readonly Mock<HttpContext> _httpContext;
    private readonly Mock<HttpRequest> _httpRequest;
    private readonly Mock<HttpResponse> _httpResponse;
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>> _options;

    public ApiKeyAuthenticationHandlerTests()
    {
        _options = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        _options.Setup(x => x.Get(It.IsAny<string>())).Returns(new ApiKeyAuthenticationOptions());

        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger<ApiKeyAuthenticationHandler>>());

        _encoder = UrlEncoder.Default;
        _apiKeyService = new Mock<IApiKeyService>();

        _httpContext = new Mock<HttpContext>();
        _httpRequest = new Mock<HttpRequest>();
        _httpResponse = new Mock<HttpResponse>();
        _httpContext.SetupGet(x => x.Request).Returns(_httpRequest.Object);
        _httpContext.SetupGet(x => x.Response).Returns(_httpResponse.Object);
        _httpContext.SetupGet(x => x.RequestAborted).Returns(CancellationToken.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_NoApiKeyHeader_ReturnsNoResult()
    {
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary());
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.True(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_EmptyApiKey_ReturnsNoResult()
    {
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "X-NuGet-ApiKey", new StringValues("") }
        });
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.True(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_InvalidApiKey_ReturnsFailResult()
    {
        _apiKeyService.Setup(s => s.IsValidAsync("bad-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "X-NuGet-ApiKey", new StringValues("bad-key") }
        });
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid API key", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ValidApiKey_ReturnsSuccessWithPublisherRole()
    {
        _apiKeyService.Setup(s => s.IsValidAsync("valid-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "X-NuGet-ApiKey", new StringValues("valid-key") }
        });
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.True(result.Principal.IsInRole(Roles.Publisher));
    }

    private ApiKeyAuthenticationHandler CreateHandler()
    {
        var handler = new ApiKeyAuthenticationHandler(
            _options.Object,
            _loggerFactory.Object,
            _encoder,
            _apiKeyService.Object);

        handler.InitializeAsync(
            new AuthenticationScheme(AuthenticationConstants.ApiKeyScheme, null, typeof(ApiKeyAuthenticationHandler)),
            _httpContext.Object).GetAwaiter().GetResult();

        return handler;
    }
}
