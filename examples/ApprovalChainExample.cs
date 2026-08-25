namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Example: Multi-level document approval workflow.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ApprovalChainExample : ControllerBase
{
    private readonly IWorkflowDefinitionService _workflowService;
    private readonly IWorkflowExecutionService _executionService;

    // Properties for ToString representation
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;

    public ApprovalChainExample(
        IWorkflowDefinitionService workflowService,
        IWorkflowExecutionService executionService)
    {
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(executionService);
        _workflowService = workflowService;
        _executionService = executionService;
    }

    public override string ToString()
    {
        return $"ApprovalChainExample {{ DocumentId = {DocumentId}, Title = {Title}, Amount = {Amount}, SubmittedBy = {SubmittedBy}, ApprovedBy = {ApprovedBy}, Comments = {Comments} }}";
    }

    // ... rest of the code remains the same ...
