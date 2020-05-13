# ApprovalChainExample

The `ApprovalChainExample` class models a simple document approval workflow. It holds document metadata and exposes asynchronous methods that simulate the lifecycle of a document from initialization through submission, approval, and rejection. This class is intended as a demonstration of workflow orchestration using the `dotnet-workflow-engine` library.

## API

### `ApprovalChainExample()`
Constructor. Initializes a new instance of the `ApprovalChainExample` class. All string properties default to `null` and `Amount` defaults to `0`.

### `public string DocumentId { get; set; }`
Gets or sets the unique identifier for the document. Must be set before calling `InitializeWorkflow`.

### `public string Title { get; set; }`
Gets or sets the document title.

### `public decimal Amount { get; set; }`
Gets or sets the monetary amount associated with the document.

### `public string SubmittedBy { get; set; }`
Gets or sets the name of the user who submitted the document.

### `public string ApprovedBy { get; set; }`
Gets or sets the name of the user who approved or rejected the document.

### `public string Comments { get; set; }`
Gets or sets any comments attached to the approval or rejection action.

### `public async Task<ActionResult> InitializeWorkflow()`
Initializes the workflow for the current document.  
- **Parameters:** None.  
- **Returns:** An `ActionResult` indicating success or failure.  
- **Throws:** `InvalidOperationException` if `DocumentId` is null or empty. May also throw if the workflow engine encounters an error during initialization.

### `public async Task<ActionResult> SubmitForApproval()`
Submits the document for approval.  
- **Parameters:** None.  
- **Returns:** An `ActionResult` indicating success or failure.  
- **Throws:** `InvalidOperationException` if the workflow has not been initialized or if `SubmittedBy` is null or empty.

### `public async Task<ActionResult> ApproveDocument()`
Approves the submitted document.  
- **Parameters:** None.  
- **Returns:** An `ActionResult` indicating success or failure.  
- **Throws:** `InvalidOperationException` if the document has not been submitted for approval, or if it has already been approved or rejected.

### `public async Task<ActionResult> RejectDocument()`
Rejects the submitted document.  
- **Parameters:** None.  
- **Returns:** An `ActionResult` indicating success or failure.  
- **Throws:** `InvalidOperationException` if the document has not been submitted for approval, or if it has already been approved or rejected.

## Usage

### Example 1: Initialize and submit a document for approval

```csharp
var doc = new ApprovalChainExample
{
    DocumentId = "DOC-001",
    Title = "Purchase Order #1234",
    Amount = 1500.00m,
    SubmittedBy = "jdoe"
};

ActionResult initResult = await doc.InitializeWorkflow();
if (!initResult.IsSuccess)
{
    Console.WriteLine($"Initialization failed: {initResult.ErrorMessage}");
    return;
}

ActionResult submitResult = await doc.SubmitForApproval();
if (submitResult.IsSuccess)
{
    Console.WriteLine("Document submitted for approval.");
}
else
{
    Console.WriteLine($"Submission failed: {submitResult.ErrorMessage}");
}
```

### Example 2: Approve a previously submitted document

```csharp
var doc = new ApprovalChainExample
{
    DocumentId = "DOC-002",
    Title = "Contract Renewal",
    Amount = 5000.00m,
    SubmittedBy = "asmith"
};

await doc.InitializeWorkflow();
await doc.SubmitForApproval();

doc.ApprovedBy = "bwilson";
doc.Comments = "Approved with standard terms.";

ActionResult approveResult = await doc.ApproveDocument();
if (approveResult.IsSuccess)
{
    Console.WriteLine("Document approved.");
}
else
{
    Console.WriteLine($"Approval failed: {approveResult.ErrorMessage}");
}
```

## Notes

- The class is not thread-safe. Concurrent calls to workflow methods on the same instance may produce undefined behavior. Use external synchronization if multiple threads access the same object.
- Calling `SubmitForApproval`, `ApproveDocument`, or `RejectDocument` before `InitializeWorkflow` will throw an `InvalidOperationException`.
- Once a document has been approved or rejected, further calls to `ApproveDocument` or `RejectDocument` will throw an `InvalidOperationException`.
- The `Amount` property is not validated by the workflow methods; it is purely informational.
- The `ApprovedBy` and `Comments` properties can be set at any time, but they are typically populated before calling `ApproveDocument` or `RejectDocument`.
