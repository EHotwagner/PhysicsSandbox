using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PhysicsSandbox.Integration.Tests;

/// <summary>
/// Lightweight MCP client for integration testing via HTTP/SSE transport.
/// Connects to the MCP server's SSE endpoint, performs the initialize handshake,
/// and provides a method to call tools with JSON arguments.
/// </summary>
public sealed class McpTestClient : IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private string? _messageEndpoint;
    private int _nextId = 1;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<int, TaskCompletionSource<JsonNode?>> _pending = new();
    private Task? _sseTask;

    public McpTestClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        // Start SSE listener
        var sseUrl = $"{_baseUrl}/sse";
        var request = new HttpRequestMessage(HttpMethod.Get, sseUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        var reader = new StreamReader(stream);

        // Read first SSE event to get the message endpoint
        var endpointTcs = new TaskCompletionSource<string>();
        _sseTask = Task.Run(async () =>
        {
            string? eventType = null;
            while (!_cts.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(_cts.Token);
                if (line == null) break;

                if (line.StartsWith("event:"))
                    eventType = line[6..].Trim();
                else if (line.StartsWith("data:"))
                {
                    var data = line[5..].Trim();
                    if (eventType == "endpoint")
                    {
                        endpointTcs.TrySetResult(data);
                    }
                    else if (eventType == "message")
                    {
                        try
                        {
                            var msg = JsonNode.Parse(data);
                            var id = msg?["id"]?.GetValue<int>();
                            if (id.HasValue && _pending.TryGetValue(id.Value, out var tcs))
                            {
                                _pending.Remove(id.Value);
                                if (msg?["error"] != null)
                                    tcs.SetResult(msg["error"]);
                                else
                                    tcs.SetResult(msg?["result"]);
                            }
                        }
                        catch { /* ignore parse errors */ }
                    }
                }
                else if (string.IsNullOrEmpty(line))
                {
                    eventType = null;
                }
            }
        }, _cts.Token);

        // Wait for endpoint
        var endpointPath = await endpointTcs.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        _messageEndpoint = endpointPath.StartsWith("http")
            ? endpointPath
            : $"{_baseUrl}{endpointPath}";

        // Send initialize
        await SendRequestAsync("initialize", new JsonObject
        {
            ["protocolVersion"] = "2024-11-05",
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject
            {
                ["name"] = "McpTestClient",
                ["version"] = "1.0"
            }
        }, ct);

        // Send initialized notification
        await SendNotificationAsync("notifications/initialized", null, ct);
    }

    public async Task<(string Status, string? Message)> CallToolAsync(
        string toolName,
        JsonObject? arguments = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await SendRequestAsync("tools/call", new JsonObject
            {
                ["name"] = toolName,
                ["arguments"] = arguments ?? new JsonObject()
            }, ct);

            if (result == null)
                return ("RPC_ERROR", "Null response");

            // Check if it's an error response
            if (result["code"] != null)
                return ("RPC_ERROR", result["message"]?.GetValue<string>() ?? "Unknown error");

            // MCP tool result has "content" array
            var content = result["content"]?.AsArray();
            if (content == null || content.Count == 0)
                return ("OK", null);

            var text = content[0]?["text"]?.GetValue<string>();
            var isError = result["isError"]?.GetValue<bool>() ?? false;
            return (isError ? "TOOL_ERROR" : "OK", text);
        }
        catch (TimeoutException)
        {
            return ("TIMEOUT", "Request timed out");
        }
        catch (Exception ex)
        {
            return ("EXCEPTION", ex.Message);
        }
    }

    private async Task<JsonNode?> SendRequestAsync(string method, JsonObject? parameters, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _nextId);
        var tcs = new TaskCompletionSource<JsonNode?>();
        _pending[id] = tcs;

        var payload = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method
        };
        if (parameters != null)
            payload["params"] = parameters;

        var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        await _http.PostAsync(_messageEndpoint!, content, ct);

        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
    }

    private async Task SendNotificationAsync(string method, JsonObject? parameters, CancellationToken ct)
    {
        var payload = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method
        };
        if (parameters != null)
            payload["params"] = parameters;

        var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        await _http.PostAsync(_messageEndpoint!, content, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        if (_sseTask != null)
        {
            try { await _sseTask.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch { /* ignore */ }
        }
        _cts.Dispose();
    }
}
