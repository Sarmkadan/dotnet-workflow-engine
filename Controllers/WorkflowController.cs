// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Utilities;
using DotNetWorkflowEngine.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkflowEngine.Controllers;

/// <summary>
/// REST API endpoints for workflow management. Provides CRUD operations for
/// workflow definitions, validation, and metadata retrieval. All endpoints
/// require authentication via JWT bearer token.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly WorkflowDefinitionService _workflowService;
    private readonly WorkflowValidator _validator;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        WorkflowDefinitionService workflowService,
        WorkflowValidator validator,
        ILogger<WorkflowController> logger)
    {
        _workflowService = workflowService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all workflow definitions with optional filtering and pagination.
    /// Returns 200 OK with array of workflows, or 500 on server error.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Workflow>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllWorkflows(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        [FromQuery] string? status = null)
    {
        try
        {
            _logger.LogInformation("Retrieving workflows: skip={Skip}, take={Take}, status={Status}", skip, take, status);

            IEnumerable<Workflow> workflows = _workflowService.GetAllWorkflows();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WorkflowStatus>(status, ignoreCase: true, out var parsedStatus))
                workflows = workflows.Where(w => w.Status == parsedStatus);

            var result = workflows.Skip(skip).Take(take).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflows");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a specific workflow definition by ID. Returns 200 OK with the
    /// workflow, 404 if not found, or 500 on server error.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Workflow), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWorkflow(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Workflow ID cannot be empty" });

            _logger.LogInformation("Retrieving workflow: {WorkflowId}", id);

            var workflow = await Task.FromResult(_workflowService.GetWorkflow(id));

            if (workflow == null)
                return NotFound(new { error = $"Workflow '{id}' not found" });

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new workflow definition. The request body should contain a valid
    /// workflow configuration. Returns 201 Created with the created workflow,
    /// 400 if validation fails, or 500 on server error.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Workflow), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateWorkflow([FromBody] Workflow workflow)
    {
        try
        {
            if (workflow == null)
                return BadRequest(new { error = "Workflow definition is required" });

            if (string.IsNullOrWhiteSpace(workflow.Id))
                workflow.Id = Guid.NewGuid().ToString();

            // Validate the workflow before creation
            var validationResult = _validator.Validate(workflow);
            if (!validationResult.IsValid)
                return BadRequest(new { error = "Workflow validation failed", details = validationResult.Errors });

            if (_workflowService.GetWorkflow(workflow.Id) != null)
                return Conflict(new { error = $"Workflow '{workflow.Id}' already exists" });

            _logger.LogInformation("Creating workflow: {WorkflowName}", workflow.Name);

            _workflowService.AddWorkflow(workflow);
            var createdWorkflow = await Task.FromResult(workflow);

            return CreatedAtAction(nameof(GetWorkflow), new { id = createdWorkflow.Id }, createdWorkflow);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Workflow validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing workflow definition. Returns 200 OK with the updated
    /// workflow, 404 if not found, 400 if validation fails, or 500 on error.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Workflow), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateWorkflow(string id, [FromBody] Workflow workflow)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id) || workflow == null)
                return BadRequest(new { error = "Workflow ID and definition are required" });

            workflow.Id = id;

            if (_workflowService.GetWorkflow(id) == null)
                return NotFound(new { error = $"Workflow '{id}' not found" });

            var validationResult = _validator.Validate(workflow);
            if (!validationResult.IsValid)
                return BadRequest(new { error = "Workflow validation failed", details = validationResult.Errors });

            _logger.LogInformation("Updating workflow: {WorkflowId}", id);

            _workflowService.AddWorkflow(workflow);
            var updatedWorkflow = await Task.FromResult(workflow);

            return Ok(updatedWorkflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a workflow definition by ID. Returns 204 No Content on success,
    /// 404 if not found, or 500 on server error. Note: cannot delete workflows
    /// that have active running instances.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteWorkflow(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Workflow ID cannot be empty" });

            _logger.LogInformation("Deleting workflow: {WorkflowId}", id);

            if (_workflowService.GetWorkflow(id) == null)
                return NotFound(new { error = $"Workflow '{id}' not found" });

            _workflowService.DeleteWorkflow(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validates a workflow definition without persisting it. Useful for pre-flight
    /// checks before creation. Returns validation result with detailed error messages.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(WorkflowValidator.ValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateWorkflow([FromBody] Workflow workflow)
    {
        try
        {
            if (workflow == null)
                return BadRequest(new { error = "Workflow definition is required" });

            _logger.LogInformation("Validating workflow: {WorkflowName}", workflow.Name);

            var result = _validator.Validate(workflow);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports a workflow definition to JSON format. Returns 200 OK with the JSON definition,
    /// 404 if workflow not found, or 500 on server error.
    /// </summary>
    [HttpGet("{id}/definition")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWorkflowDefinition(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Workflow ID cannot be empty" });

            _logger.LogInformation("Exporting workflow definition: {WorkflowId}", id);

            var jsonDefinition = await Task.FromResult(_workflowService.ExportWorkflowToJson(id));

            return Ok(jsonDefinition);
        }
        catch (WorkflowException ex) when (ex.ErrorCode == "WORKFLOW_NOT_FOUND")
        {
            _logger.LogWarning("Workflow not found during export: {WorkflowId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting workflow definition {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Imports a workflow definition from JSON format. Returns 201 Created with the imported workflow,
    /// 400 if validation fails, 409 if workflow already exists and overwrite is false, or 500 on server error.
    /// </summary>
    [HttpPost("{id}/definition")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Workflow), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ImportWorkflowDefinition(string id, [FromBody] string jsonDefinition, [FromQuery] string name, [FromQuery] bool overwrite = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Workflow ID cannot be empty" });

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { error = "Workflow name query parameter is required" });

            if (string.IsNullOrWhiteSpace(jsonDefinition))
                return BadRequest(new { error = "JSON definition is required in request body" });

            _logger.LogInformation("Importing workflow definition: {WorkflowId}", id);

            var workflow = await Task.FromResult(_workflowService.ImportWorkflowFromJson(id, name, jsonDefinition, overwrite));

            return CreatedAtAction(nameof(GetWorkflow), new { id = workflow.Id }, workflow);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Workflow import validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message, details = ex.Errors });
        }
        catch (WorkflowException ex) when (ex.ErrorCode == "WORKFLOW_EXISTS")
        {
            _logger.LogWarning("Workflow already exists during import: {WorkflowId}", id);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing workflow definition {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validates a workflow JSON definition without importing it. Useful for pre-flight
    /// checks before actual import. Returns validation result with detailed error messages.
    /// </summary>
    [HttpPost("validate-definition")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateWorkflowDefinition([FromBody] string jsonDefinition)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonDefinition))
                return BadRequest(new { error = "JSON definition is required" });

            _logger.LogInformation("Validating workflow JSON definition");

            var isValid = _workflowService.ValidateWorkflowJson(jsonDefinition, out var errors);

            if (isValid)
                return Ok(new { valid = true });
            else
                return BadRequest(new { valid = false, errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow JSON definition");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
