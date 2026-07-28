using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ChatServer;

/// <summary>A single turn in the conversation history sent from the client.</summary>
public sealed record ChatMessage(string Role, string Text);

/// <summary>
/// Calls the Gemini generateContent API.
/// When the MCP server is reachable the service acts as an MCP client:
///   1. Fetches the available tools from the MCP server.
///   2. Passes those tool definitions to Gemini.
///   3. Runs the agentic loop — when Gemini requests a tool call the service
///      executes it against the MCP server and feeds the result back until
///      Gemini returns a plain-text answer.
/// If the MCP server is unavailable it falls back to answering without tools.
/// </summary>
public sealed class GeminiService : IAsyncDisposable
{
    private const string GeminiEndpoint =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    private readonly HttpClient _http;
    private readonly string _mcpServerUrl;
    private McpClient? _mcpClient;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public GeminiService(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient();
        _mcpServerUrl = config["MCP_SERVER_URL"]
            ?? Environment.GetEnvironmentVariable("MCP_SERVER_URL")
            ?? "http://mcp-server:5000";
    }

    // ─── Public entry point ───────────────────────────────────────────────────

    public async Task<string> GenerateAsync(IList<ChatMessage>? history, string message)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("GEMINI_API_KEY is not set.");

        var mcp = await GetMcpClientAsync();
        return mcp is not null
            ? await GenerateWithToolsAsync(apiKey, history, message, mcp)
            : await GenerateDirectAsync(apiKey, history, message);
    }

    // ─── Direct Gemini (no tools — MCP server unavailable) ───────────────────

    private async Task<string> GenerateDirectAsync(
        string apiKey, IList<ChatMessage>? history, string message)
    {
        var body = new JsonObject { ["contents"] = BuildContents(history, message) };
        return await CallGeminiForTextAsync(apiKey, body);
    }

    // ─── Agentic loop (Gemini + MCP tools) ───────────────────────────────────

    private async Task<string> GenerateWithToolsAsync(
        string apiKey,
        IList<ChatMessage>? history,
        string message,
        McpClient mcp)
    {
        var mcpTools  = await mcp.ListToolsAsync();
        var toolsNode = BuildToolDeclarations(mcpTools);
        var contents  = BuildContents(history, message);

        const int maxIterations = 10;
        for (var i = 0; i < maxIterations; i++)
        {
            // ── Ask Gemini ──────────────────────────────────────────────────
            var reqBody = new JsonObject
            {
                ["contents"] = contents.DeepClone(),
                ["tools"]    = toolsNode.DeepClone(),
            };

            var httpResp = await _http.PostAsync(
                $"{GeminiEndpoint}?key={apiKey}",
                JsonContent.Create(reqBody));

            if (!httpResp.IsSuccessStatusCode)
            {
                var err = await httpResp.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Gemini error {(int)httpResp.StatusCode}: {err}",
                    null, httpResp.StatusCode);
            }

            using var doc       = JsonDocument.Parse(await httpResp.Content.ReadAsStringAsync());
            var contentEl = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content");
            var firstPart = contentEl.GetProperty("parts")[0];

            // ── If no function call — return the final text answer ──────────
            if (!firstPart.TryGetProperty("functionCall", out var funcCallEl))
                return firstPart.GetProperty("text").GetString() ?? string.Empty;

            // ── Execute the requested tool via MCP server ───────────────────
            var funcName = funcCallEl.GetProperty("name").GetString()!;
            var funcArgs = funcCallEl.TryGetProperty("args", out var argsEl)
                ? ParseArgs(argsEl)
                : new Dictionary<string, object?>();

            string toolOutput;
            try
            {
                var result = await mcp.CallToolAsync(funcName, funcArgs);
                toolOutput = ExtractText(result);
            }
            catch (Exception ex)
            {
                toolOutput = $"Tool error: {ex.Message}";
            }

            // ── Append model turn + tool response to the conversation ───────
            contents.Add(JsonNode.Parse(contentEl.GetRawText())!);
            contents.Add(new JsonObject
            {
                ["role"]  = "user",
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["functionResponse"] = new JsonObject
                        {
                            ["name"]     = funcName,
                            ["response"] = new JsonObject { ["content"] = toolOutput },
                        },
                    },
                },
            });
        }

        throw new InvalidOperationException("Exceeded maximum tool-call iterations.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Build the Gemini 'contents' array from history + current message.</summary>
    private static JsonArray BuildContents(IList<ChatMessage>? history, string message)
    {
        var contents = new JsonArray();
        foreach (var m in history ?? [])
        {
            contents.Add(new JsonObject
            {
                ["role"]  = m.Role == "assistant" ? "model" : "user",
                ["parts"] = new JsonArray { new JsonObject { ["text"] = m.Text } },
            });
        }
        contents.Add(new JsonObject
        {
            ["role"]  = "user",
            ["parts"] = new JsonArray { new JsonObject { ["text"] = message } },
        });
        return contents;
    }

    /// <summary>
    /// Convert MCP tool definitions to Gemini's function_declarations format.
    /// The MCP SDK exposes each tool's JSON Schema via ProtocolTool.InputSchema.
    /// Nullable union types like ["string","null"] are collapsed to "string" because
    /// Gemini only accepts a single type string per property.
    /// </summary>
    private static JsonArray BuildToolDeclarations(IList<McpClientTool> tools)
    {
        var declarations = new JsonArray();
        foreach (var tool in tools)
        {
            var decl = new JsonObject
            {
                ["name"]        = tool.Name,
                ["description"] = tool.Description ?? string.Empty,
            };
            if (tool.ProtocolTool.InputSchema.ValueKind != JsonValueKind.Undefined)
            {
                var schema = JsonNode.Parse(tool.ProtocolTool.InputSchema.GetRawText());
                SanitizeSchemaForGemini(schema);
                decl["parameters"] = schema;
            }
            declarations.Add(decl);
        }
        return new JsonArray { new JsonObject { ["function_declarations"] = declarations } };
    }

    /// <summary>
    /// Mutates a JSON Schema node in-place so it is accepted by Gemini's API.
    /// - Collapses ["TYPE","null"] union types to just "TYPE".
    /// - Removes "default" fields (not part of Gemini's schema spec).
    /// </summary>
    private static void SanitizeSchemaForGemini(JsonNode? node)
    {
        if (node is JsonArray arr)
        {
            foreach (var item in arr)
                SanitizeSchemaForGemini(item);
            return;
        }

        if (node is not JsonObject obj) return;

        // ["TYPE","null"]  →  "TYPE"
        if (obj["type"] is JsonArray typeArr)
        {
            var nonNull = typeArr
                .OfType<JsonValue>()
                .Select(v => v.GetValue<string>())
                .FirstOrDefault(t => t != "null") ?? "string";
            obj["type"] = nonNull;
        }

        // Remove "default" — Gemini does not support it
        obj.Remove("default");

        // Recurse into nested properties
        foreach (var key in obj.Select(p => p.Key).ToArray())
            SanitizeSchemaForGemini(obj[key]);
    }

    /// <summary>Convert a Gemini args JsonElement to a string-keyed argument dictionary.</summary>
    private static Dictionary<string, object?> ParseArgs(JsonElement args) =>
        args.EnumerateObject().ToDictionary(
            p => p.Name,
            p => p.Value.ValueKind switch
            {
                JsonValueKind.String => (object?)p.Value.GetString(),
                JsonValueKind.Number when p.Value.TryGetInt32(out var n) => n,
                JsonValueKind.Number => p.Value.GetDouble(),
                JsonValueKind.True   => true,
                JsonValueKind.False  => false,
                JsonValueKind.Null   => null,
                _                   => (object?)p.Value.GetRawText(),
            });

    /// <summary>Extract plain text from an MCP CallToolResult.</summary>
    private static string ExtractText(CallToolResult result)
    {
        // Serialize generically so this works across SDK versions.
        var json = JsonSerializer.Serialize(result.Content);
        using var doc = JsonDocument.Parse(json);
        return string.Concat(doc.RootElement.EnumerateArray()
            .Select(item => item.TryGetProperty("text", out var t)
                ? t.GetString() ?? ""
                : ""));
    }

    private async Task<string> CallGeminiForTextAsync(string apiKey, JsonObject body)
    {
        var resp = await _http.PostAsync(
            $"{GeminiEndpoint}?key={apiKey}", JsonContent.Create(body));
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStreamAsync());
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    // ─── MCP client lifecycle ─────────────────────────────────────────────────

    private async Task<McpClient?> GetMcpClientAsync()
    {
        if (_mcpClient is not null) return _mcpClient;

        await _lock.WaitAsync();
        try
        {
            if (_mcpClient is not null) return _mcpClient;

            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(_mcpServerUrl),
            });
            _mcpClient = await McpClient.CreateAsync(transport);
            return _mcpClient;
        }
        catch
        {
            // MCP server not yet available — answer without tools this request.
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_mcpClient is IAsyncDisposable ad)
            await ad.DisposeAsync();
        else if (_mcpClient is IDisposable d)
            d.Dispose();
        _lock.Dispose();
    }
}

