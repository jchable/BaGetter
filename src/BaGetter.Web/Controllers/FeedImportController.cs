using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BaGetter.Web;

[ApiController]
[Route("admin/api/import")]
[Authorize(Policy = AuthenticationConstants.PolicyCanAdmin)]
public class FeedImportController : ControllerBase
{
    private readonly Channel<FeedImportJob> _jobChannel;
    private readonly FeedImportProgressTracker _tracker;
    private readonly FeedImportBackgroundService _backgroundService;

    public FeedImportController(
        Channel<FeedImportJob> jobChannel,
        FeedImportProgressTracker tracker,
        FeedImportBackgroundService backgroundService)
    {
        _jobChannel = jobChannel;
        _tracker = tracker;
        _backgroundService = backgroundService;
    }

    [HttpPost("start")]
    public IActionResult Start([FromBody] FeedImportRequest request)
    {
        if (_tracker.IsRunning)
        {
            return Conflict(new { error = "An import is already in progress." });
        }

        if (string.IsNullOrWhiteSpace(request?.FeedUrl))
        {
            return BadRequest(new { error = "Feed URL is required." });
        }

        if (!Uri.TryCreate(request.FeedUrl, UriKind.Absolute, out var feedUri))
        {
            return BadRequest(new { error = "Invalid feed URL." });
        }

        var options = new FeedImportOptions
        {
            FeedUrl = feedUri,
            Legacy = request.Legacy,
            AuthType = request.AuthType,
            ApiKey = request.ApiKey,
            Username = request.Username,
            Password = request.Password,
        };

        var job = new FeedImportJob { Options = options };

        if (!_jobChannel.Writer.TryWrite(job))
        {
            return Conflict(new { error = "Import queue is full. Please wait for the current import to complete." });
        }

        return Accepted(new { message = "Import started." });
    }

    [HttpPost("cancel")]
    public IActionResult Cancel()
    {
        if (!_tracker.IsRunning)
        {
            return BadRequest(new { error = "No import is currently running." });
        }

        _backgroundService.CancelCurrentImport();
        return Ok(new { message = "Import cancellation requested." });
    }

    [HttpGet("progress")]
    public async Task Progress(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no"; // Disable nginx buffering

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await _tracker.ReadProgressAsync(async progress =>
        {
            var json = JsonSerializer.Serialize(progress, jsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }, cancellationToken);
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        var progress = _tracker.GetCurrentProgress();
        if (progress == null)
        {
            return Ok(new { isRunning = false });
        }

        return Ok(new
        {
            isRunning = _tracker.IsRunning,
            progress
        });
    }
}

public class FeedImportRequest
{
    public string FeedUrl { get; set; }
    public bool Legacy { get; set; }
    public MirrorAuthenticationType AuthType { get; set; }
    public string ApiKey { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}
