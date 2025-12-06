using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class AddNoteInput
{
    public string EntityType { get; set; } = ""; // Customer, SalesOrder, Invoice
    public string EntityId { get; set; } = "";
    public string NoteText { get; set; } = "";
    public string? Category { get; set; }
    public bool IsInternal { get; set; } = true;
}

public class AddNoteOutput
{
    public string NoteId { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string NoteText { get; set; } = "";
    public string? Category { get; set; }
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
}

public class AddNoteInputValidator : AbstractValidator<AddNoteInput>
{
    public AddNoteInputValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty()
            .Must(x => new[] { "Customer", "SalesOrder", "Invoice" }.Contains(x))
            .WithMessage("entity_type must be Customer, SalesOrder, or Invoice");
        
        RuleFor(x => x.EntityId).NotEmpty().WithMessage("entity_id is required");
        RuleFor(x => x.NoteText).NotEmpty().MaximumLength(4000).WithMessage("note_text is required (max 4000 chars)");
    }
}

public class AddNoteTool : ToolBase<AddNoteInput, AddNoteOutput>
{
    private readonly IWcfClient _wcfClient;
    
    public override string Name => "ax_add_note";
    public override string Description => "Add a note to a customer, sales order, or invoice";
    
    public AddNoteTool(
        ILogger<AddNoteTool> logger,
        IAuditService audit,
        AddNoteInputValidator validator,
        IWcfClient wcfClient)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
    }
    
    protected override async Task<AddNoteOutput> ExecuteCoreAsync(
        AddNoteInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var noteId = $"NOTE-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
        
        _logger.LogInformation("Adding note to {EntityType} {EntityId}: {NoteText}", 
            input.EntityType, input.EntityId, input.NoteText[..Math.Min(50, input.NoteText.Length)]);
        
        // In real implementation, would call WCF to add DocuRef
        
        return new AddNoteOutput
        {
            NoteId = noteId,
            EntityType = input.EntityType,
            EntityId = input.EntityId,
            NoteText = input.NoteText,
            Category = input.Category,
            IsInternal = input.IsInternal,
            CreatedAt = DateTime.Now,
            CreatedBy = context.UserId
        };
    }
}
