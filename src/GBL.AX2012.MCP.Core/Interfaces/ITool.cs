using System.Text.Json;
using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Core.Interfaces;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonElement InputSchema { get; }
    Task<ToolResponse> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken cancellationToken);
}
