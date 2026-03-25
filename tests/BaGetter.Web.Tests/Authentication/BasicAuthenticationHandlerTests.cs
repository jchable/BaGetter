using System;
using System.Security.Claims;
using System.Text;
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

public class BasicAuthenticationHandlerTests
{
    private readonly Mock<IApiKeyService> _apiKeyService;
    private readonly UrlEncoder _encoder;
    private readonly Mock<HttpContext> _httpContext;
    private readonly Mock<HttpRequest> _httpRequest;
    private readonly Mock<HttpResponse> _httpResponse;
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<IOptionsMonitor<BasicAuthenticationOptions>> _options;

    public BasicAuthenticationHandlerTests()
    {
        _options = new Mock<IOptionsMonitor<BasicAuthenticationOptions>>();
        _options.Setup(x => x.Get(It.IsAny<string>())).Returns(new BasicAuthenticationOptions());

        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger<BasicAuthenticationHandler>>());

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
    public async Task HandleAuthenticateAsync_NoAuthorizationHeader_ReturnsNoResult()
    {
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary());
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.True(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_BearerScheme_ReturnsNoResult()
    {
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "Authorization", new StringValues("Bearer some-token") }
        });
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.True(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_MalformedBase64_ReturnsFail()
    {
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "Authorization", new StringValues("Basic !!!not-base64!!!") }
        });
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_EmptyPassword_ReturnsFail()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:"));
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "Authorization", new StringValues($"Basic {encoded}") }
        });
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_InvalidPassword_ReturnsFail()
    {
        _apiKeyService.Setup(s => s.IsValidAsync("wrong-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:wrong-key"));
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "Authorization", new StringValues($"Basic {encoded}") }
        });
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid credentials", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ValidPassword_ReturnsSuccessWithReaderRole()
    {
        _apiKeyService.Setup(s => s.IsValidAsync("valid-api-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("anyuser:valid-api-key"));
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "Authorization", new StringValues($"Basic {encoded}") }
        });
        var handler = CreateHandler();

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.True(result.Principal.IsInRole(Roles.Reader));
        Assert.False(result.Principal.IsInRole(Roles.Publisher));
    }

    private BasicAuthenticationHandler CreateHandler()
    {
        var handler = new BasicAuthenticationHandler(
            _options.Object,
            _loggerFactory.Object,
            _encoder,
            _apiKeyService.Object);

        handler.InitializeAsync(
            new AuthenticationScheme(AuthenticationConstants.BasicScheme, null, typeof(BasicAuthenticationHandler)),
            _httpContext.Object).GetAwaiter().GetResult();

        return handler;
    }
}
