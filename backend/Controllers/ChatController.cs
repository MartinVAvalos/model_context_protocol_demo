using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly GeminiService _gemini;

    public ChatController(GeminiService gemini) => _gemini = gemini;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "message is required" });

        try
        {
            var answer = await _gemini.GenerateAsync(request.History, request.Message);
            return Ok(new { answer });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("GEMINI_API_KEY"))
        {
            return StatusCode(503, new { error = "Gemini API key is not configured on the server." });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = $"AI service error: {ex.Message}" });
        }
    }
}

/// <summary>Request body for POST /api/chat.</summary>
public sealed record ChatRequest(
    string Message,
    IList<ChatMessage>? History = null);
