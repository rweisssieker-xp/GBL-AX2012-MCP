using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class UpdateSalesOrderInput
{
    public string SalesId { get; set; } = "";
    public string? CustomerRef { get; set; }
    public DateTime? RequestedDelivery { get; set; }
    public string? Status { get; set; }
    public List<UpdateSalesLineInput>? Lines { get; set; }
    public string IdempotencyKey { get; set; } = "";
}

public class UpdateSalesLineInput
{
    public int LineNum { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Warehouse { get; set; }
    public bool? Cancel { get; set; }
}

public class UpdateSalesOrderOutput
{
    public bool Success { get; set; }
    public string SalesId { get; set; } = "";
    public string PreviousStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public int LinesUpdated { get; set; }
    public int LinesCancelled { get; set; }
    public List<string> Changes { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class UpdateSalesOrderInputValidator : AbstractValidator<UpdateSalesOrderInput>
{
    public UpdateSalesOrderInputValidator()
    {
        RuleFor(x => x.SalesId).NotEmpty().WithMessage("sales_id is required");
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("idempotency_key is required");
        
        When(x => x.Lines != null, () =>
        {
            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.LineNum).GreaterThan(0).WithMessage("line_num must be greater than 0");
            });
        });
    }
}

public class UpdateSalesOrderTool : ToolBase<UpdateSalesOrderInput, UpdateSalesOrderOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    private readonly IIdempotencyStore _idempotencyStore;
    
    public override string Name => "ax_update_salesorder";
    public override string Description => "Update an existing sales order (status, lines, delivery date)";
    
    public UpdateSalesOrderTool(
        ILogger<UpdateSalesOrderTool> logger,
        IAuditService audit,
        UpdateSalesOrderInputValidator validator,
        IWcfClient wcfClient,
        IAifClient aifClient,
        IIdempotencyStore idempotencyStore)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
        _idempotencyStore = idempotencyStore;
    }
    
    protected override async Task<UpdateSalesOrderOutput> ExecuteCoreAsync(
        UpdateSalesOrderInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Check idempotency
        var existing = await _idempotencyStore.GetAsync<UpdateSalesOrderOutput>(input.IdempotencyKey, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("Returning cached result for idempotency key {Key}", input.IdempotencyKey);
            return existing;
        }
        
        // Get current order
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId, cancellationToken);
        if (order == null)
        {
            throw new AxException("ORDER_NOT_FOUND", $"Sales order {input.SalesId} not found");
        }
        
        if (order.Status == "Invoiced" || order.Status == "Closed")
        {
            throw new AxException("ORDER_LOCKED", $"Cannot update order in status {order.Status}");
        }
        
        var changes = new List<string>();
        var warnings = new List<string>();
        var linesUpdated = 0;
        var linesCancelled = 0;
        var previousStatus = order.Status;
        var newStatus = input.Status ?? order.Status;
        
        // Update header fields
        if (input.CustomerRef != null && input.CustomerRef != order.CustomerRef)
        {
            changes.Add($"CustomerRef: {order.CustomerRef} → {input.CustomerRef}");
        }
        
        if (input.RequestedDelivery.HasValue)
        {
            changes.Add($"RequestedDelivery: {order.RequestedDelivery:d} → {input.RequestedDelivery:d}");
        }
        
        if (input.Status != null && input.Status != order.Status)
        {
            changes.Add($"Status: {order.Status} → {input.Status}");
        }
        
        // Update lines
        if (input.Lines != null)
        {
            foreach (var lineUpdate in input.Lines)
            {
                var line = order.Lines.FirstOrDefault(l => l.LineNum == lineUpdate.LineNum);
                if (line == null)
                {
                    warnings.Add($"Line {lineUpdate.LineNum} not found");
                    continue;
                }
                
                if (lineUpdate.Cancel == true)
                {
                    changes.Add($"Line {lineUpdate.LineNum}: Cancelled");
                    linesCancelled++;
                }
                else
                {
                    if (lineUpdate.Quantity.HasValue && lineUpdate.Quantity != line.Quantity)
                    {
                        if (lineUpdate.Quantity < line.DeliveredQty)
                        {
                            warnings.Add($"Line {lineUpdate.LineNum}: Cannot reduce below delivered qty {line.DeliveredQty}");
                        }
                        else
                        {
                            changes.Add($"Line {lineUpdate.LineNum} Qty: {line.Quantity} → {lineUpdate.Quantity}");
                            linesUpdated++;
                        }
                    }
                    
                    if (lineUpdate.UnitPrice.HasValue && lineUpdate.UnitPrice != line.UnitPrice)
                    {
                        changes.Add($"Line {lineUpdate.LineNum} Price: {line.UnitPrice} → {lineUpdate.UnitPrice}");
                        linesUpdated++;
                    }
                }
            }
        }
        
        // Call WCF to update
        if (changes.Any())
        {
            await _wcfClient.UpdateSalesOrderAsync(new UpdateSalesOrderRequest
            {
                SalesId = input.SalesId,
                CustomerRef = input.CustomerRef,
                RequestedDeliveryDate = input.RequestedDelivery
            }, cancellationToken);
        }
        
        var output = new UpdateSalesOrderOutput
        {
            Success = true,
            SalesId = input.SalesId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            LinesUpdated = linesUpdated,
            LinesCancelled = linesCancelled,
            Changes = changes,
            Warnings = warnings
        };
        
        // Store for idempotency
        await _idempotencyStore.SetAsync(input.IdempotencyKey, output, TimeSpan.FromDays(7), cancellationToken);
        
        _logger.LogInformation("Updated sales order {SalesId}: {Changes}", input.SalesId, string.Join(", ", changes));
        
        return output;
    }
}
