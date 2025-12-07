using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class GetReservationQueueInput
{
    public string ItemId { get; set; } = "";
    public string? WarehouseId { get; set; }
    public int MaxResults { get; set; } = 20;
}

public class GetReservationQueueOutput
{
    public string ItemId { get; set; } = "";
    public string? WarehouseId { get; set; }
    public int TotalEntries { get; set; }
    public decimal TotalReservedQty { get; set; }
    public decimal TotalPendingQty { get; set; }
    public List<ReservationQueueEntryOutput> Entries { get; set; } = new();
}

public class ReservationQueueEntryOutput
{
    public string SalesId { get; set; } = "";
    public int LineNum { get; set; }
    public string CustomerAccount { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public decimal ReservedQty { get; set; }
    public decimal PendingQty { get; set; }
    public DateTime RequestedDate { get; set; }
    public DateTime OrderDate { get; set; }
    public int Priority { get; set; }
}

public class GetReservationQueueInputValidator : AbstractValidator<GetReservationQueueInput>
{
    public GetReservationQueueInputValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("ItemId is required");
            
        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 100)
            .WithMessage("MaxResults must be between 1 and 100");
    }
}

public class GetReservationQueueTool : ToolBase<GetReservationQueueInput, GetReservationQueueOutput>
{
    private readonly IAifClient _aifClient;
    private readonly GetReservationQueueInputValidator _validator;
    
    public override string Name => "ax_get_reservation_queue";
    public override string Description => "Get the reservation queue for an item - shows who is waiting for stock";
    
    public GetReservationQueueTool(
        ILogger<GetReservationQueueTool> logger,
        IAuditService auditService,
        IAifClient aifClient,
        GetReservationQueueInputValidator validator)
        : base(logger, auditService)
    {
        _aifClient = aifClient;
        _validator = validator;
    }
    
    protected override async Task<GetReservationQueueOutput> ExecuteCoreAsync(
        GetReservationQueueInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
        {
            throw new FluentValidation.ValidationException(validation.Errors);
        }
        
        _logger.LogInformation("Getting reservation queue for item {ItemId}", input.ItemId);
        
        var entries = await _aifClient.GetReservationQueueAsync(
            input.ItemId, 
            input.WarehouseId, 
            cancellationToken);
        
        var entryList = entries.Take(input.MaxResults).ToList();
        
        _logger.LogInformation("Found {Count} reservation queue entries for {ItemId}", 
            entryList.Count, input.ItemId);
        
        return new GetReservationQueueOutput
        {
            ItemId = input.ItemId,
            WarehouseId = input.WarehouseId,
            TotalEntries = entryList.Count,
            TotalReservedQty = entryList.Sum(e => e.ReservedQty),
            TotalPendingQty = entryList.Sum(e => e.PendingQty),
            Entries = entryList.Select(e => new ReservationQueueEntryOutput
            {
                SalesId = e.SalesId,
                LineNum = e.LineNum,
                CustomerAccount = e.CustomerAccount,
                CustomerName = e.CustomerName,
                ReservedQty = e.ReservedQty,
                PendingQty = e.PendingQty,
                RequestedDate = e.RequestedDate,
                OrderDate = e.OrderDate,
                Priority = e.Priority
            }).ToList()
        };
    }
}
