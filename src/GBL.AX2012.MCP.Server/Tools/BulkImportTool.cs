using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class BulkImportInput
{
    public string Type { get; set; } = ""; // "customers", "orders", "items"
    public string Format { get; set; } = "csv"; // "csv", "json"
    public string Data { get; set; } = ""; // CSV/JSON data as string
    public bool ValidateOnly { get; set; } = false;
    public int BatchSize { get; set; } = 100;
}

public class BulkImportError
{
    public int Row { get; set; }
    public string Error { get; set; } = "";
    public string? Data { get; set; }
}

public class BulkImportOutput
{
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<BulkImportError> Errors { get; set; } = new();
    public bool Completed { get; set; }
}

public class BulkImportInputValidator : AbstractValidator<BulkImportInput>
{
    private static readonly string[] ValidTypes = { "customers", "orders", "items" };
    private static readonly string[] ValidFormats = { "csv", "json" };
    
    public BulkImportInputValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("type is required")
            .Must(t => ValidTypes.Contains(t.ToLower()))
            .WithMessage($"type must be one of: {string.Join(", ", ValidTypes)}");
        
        RuleFor(x => x.Format)
            .NotEmpty()
            .WithMessage("format is required")
            .Must(f => ValidFormats.Contains(f.ToLower()))
            .WithMessage($"format must be one of: {string.Join(", ", ValidFormats)}");
        
        RuleFor(x => x.Data)
            .NotEmpty()
            .WithMessage("data is required");
        
        RuleFor(x => x.BatchSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("batch_size must be between 1 and 1000");
    }
}

public class BulkImportTool : ToolBase<BulkImportInput, BulkImportOutput>
{
    private readonly IAifClient _aifClient;
    private readonly IWcfClient _wcfClient;
    
    public override string Name => "ax_bulk_import";
    public override string Description => "Import bulk data (customers, orders, items) from CSV/JSON";
    
    public BulkImportTool(
        ILogger<BulkImportTool> logger,
        IAuditService audit,
        BulkImportInputValidator validator,
        IAifClient aifClient,
        IWcfClient wcfClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
        _wcfClient = wcfClient;
    }
    
    protected override async Task<BulkImportOutput> ExecuteCoreAsync(
        BulkImportInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        var output = new BulkImportOutput();
        var errors = new List<BulkImportError>();
        
        try
        {
            // Parse data based on format
            var records = ParseData(input.Data, input.Format, input.Type);
            output.Total = records.Count;
            
            _logger.LogInformation("Starting bulk import: {Type}, {Count} records, ValidateOnly: {ValidateOnly}",
                input.Type, records.Count, input.ValidateOnly);
            
            if (input.ValidateOnly)
            {
                // Only validate, don't import
                for (int i = 0; i < records.Count; i++)
                {
                    var validationError = await ValidateRecordAsync(records[i], input.Type, cancellationToken);
                    if (validationError != null)
                    {
                        errors.Add(new BulkImportError
                        {
                            Row = i + 1,
                            Error = validationError,
                            Data = JsonSerializer.Serialize(records[i])
                        });
                    }
                    output.Processed++;
                }
            }
            else
            {
                // Process in batches
                for (int batchStart = 0; batchStart < records.Count; batchStart += input.BatchSize)
                {
                    var batch = records.Skip(batchStart).Take(input.BatchSize).ToList();
                    
                    for (int i = 0; i < batch.Count; i++)
                    {
                        var recordIndex = batchStart + i;
                        try
                        {
                            await ImportRecordAsync(batch[i], input.Type, context, cancellationToken);
                            output.Successful++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add(new BulkImportError
                            {
                                Row = recordIndex + 1,
                                Error = ex.Message,
                                Data = JsonSerializer.Serialize(batch[i])
                            });
                            output.Failed++;
                        }
                        output.Processed++;
                    }
                }
            }
            
            output.Errors = errors;
            output.Completed = true;
            
            _logger.LogInformation("Bulk import completed: {Processed}/{Total}, Success: {Successful}, Failed: {Failed}",
                output.Processed, output.Total, output.Successful, output.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk import");
            throw;
        }
        
        return output;
    }
    
    private List<Dictionary<string, object>> ParseData(string data, string format, string type)
    {
        if (format.ToLower() == "csv")
        {
            return ParseCsv(data);
        }
        else
        {
            return ParseJson(data);
        }
    }
    
    private List<Dictionary<string, object>> ParseCsv(string csvData)
    {
        var records = new List<Dictionary<string, object>>();
        var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
        {
            throw new ArgumentException("CSV must have at least a header row and one data row");
        }
        
        var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
        
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',').Select(v => v.Trim().Trim('"')).ToArray();
            var record = new Dictionary<string, object>();
            
            for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
            {
                record[headers[j]] = values[j];
            }
            
            records.Add(record);
        }
        
        return records;
    }
    
    private List<Dictionary<string, object>> ParseJson(string jsonData)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(jsonData);
        
        if (json.ValueKind == JsonValueKind.Array)
        {
            return json.EnumerateArray()
                .Select(item => JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText())!)
                .ToList();
        }
        else
        {
            throw new ArgumentException("JSON must be an array of objects");
        }
    }
    
    private async Task<string?> ValidateRecordAsync(Dictionary<string, object> record, string type, CancellationToken cancellationToken)
    {
        // Basic validation - in production, validate against AX schema
        if (type == "customers")
        {
            if (!record.ContainsKey("customer_account") || string.IsNullOrEmpty(record["customer_account"]?.ToString()))
            {
                return "CUSTOMER_ACCOUNT_REQUIRED";
            }
        }
        else if (type == "orders")
        {
            if (!record.ContainsKey("customer_account") || string.IsNullOrEmpty(record["customer_account"]?.ToString()))
            {
                return "CUSTOMER_ACCOUNT_REQUIRED";
            }
        }
        
        return null;
    }
    
    private async Task ImportRecordAsync(Dictionary<string, object> record, string type, ToolContext context, CancellationToken cancellationToken)
    {
        // Simplified import - in production, use appropriate AX services
        if (type == "customers")
        {
            // Would call customer creation service
            _logger.LogDebug("Importing customer: {Account}", record.GetValueOrDefault("customer_account"));
        }
        else if (type == "orders")
        {
            // Would call order creation service
            _logger.LogDebug("Importing order for customer: {Account}", record.GetValueOrDefault("customer_account"));
        }
        
        await Task.CompletedTask;
    }
}

