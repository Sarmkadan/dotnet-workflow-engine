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
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Formatters;

namespace DotNetWorkflowEngine.Controllers;

/// <summary>
/// REST API endpoints for audit trail management. Provides read-only access to
/// all audit log entries for compliance, debugging, and monitoring purposes.
/// Audit logs are immutable - no delete/update operations are supported.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly AuditService _auditService;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<AuditController> _logger;
    private readonly CsvOutputFormatter _csvFormatter;

    public AuditController(AuditService auditService, IAuditRepository auditRepository, ILogger<AuditController> logger, CsvOutputFormatter csvFormatter)
    {
        _auditService = auditService;
        _auditRepository = auditRepository;
        _logger = logger;
        _csvFormatter = csvFormatter;
    }

    /// <summary>
    /// Retrieves audit log entries with advanced filtering and pagination.
    /// Supports filtering by workflow ID, instance ID, action type, and date range.
    /// Results are sorted by timestamp in descending order (newest first).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuditLogEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? workflowId = null,
        [FromQuery] string? instanceId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? executedBy = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        try
        {
            // Validate pagination parameters
            if (skip < 0 || take < 1 || take > 1000)
            {
                return BadRequest(new
                {
                    error = "Invalid pagination parameters",
                    details = "skip must be >= 0, take must be between 1 and 1000"
                });
            }

            // Validate date range if provided
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                return BadRequest(new
                {
                    error = "Invalid date range",
                    details = "fromDate must be less than or equal to toDate"
                });
            }

            _logger.LogInformation(
                "Retrieving audit logs: workflowId={WorkflowId}, instanceId={InstanceId}, action={Action}, executedBy={ExecutedBy}, fromDate={FromDate}, toDate={ToDate}, skip={Skip}, take={Take}",
                workflowId, instanceId, action, executedBy, fromDate, toDate, skip, take);

            var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(
                workflowId: workflowId,
                instanceId: instanceId,
                eventType: action, // map action to eventType
                actor: executedBy, // map executedBy to actor
                fromDate: fromDate,
                toDate: toDate,
                skip: skip,
                take: take
            );

            Response.Headers.Add("X-Total-Count", total.ToString());
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves audit log entries for a specific workflow.
    /// Returns all audit entries related to the workflow and its instances.
    /// </summary>
    [HttpGet("workflow/{workflowId}")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWorkflowAuditLog(
        string workflowId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                return BadRequest(new { error = "Workflow ID cannot be empty" });

            if (skip < 0 || take < 1 || take > 1000)
                return BadRequest(new { error = "Invalid pagination parameters" });

            _logger.LogInformation("Retrieving audit log for workflow: {WorkflowId}", workflowId);

            var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(
                workflowId: workflowId,
                skip: skip,
                take: take
            );

            if (logs.Count == 0)
            {
                _logger.LogWarning("No audit logs found for workflow {WorkflowId}", workflowId);
                return NotFound(new { error = $"No audit logs found for workflow '{workflowId}'" });
            }
            Response.Headers.Add("X-Total-Count", total.ToString());
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log for workflow {WorkflowId}", workflowId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves audit log entries for a specific workflow instance.
    /// Shows all operations and state changes for that instance.
    /// </summary>
    [HttpGet("instance/{instanceId}")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInstanceAuditLog(
        string instanceId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return BadRequest(new { error = "Instance ID cannot be empty" });

            if (skip < 0 || take < 1 || take > 1000)
                return BadRequest(new { error = "Invalid pagination parameters" });

            _logger.LogInformation("Retrieving audit log for instance: {InstanceId}", instanceId);

            var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(
                instanceId: instanceId,
                skip: skip,
                take: take
            );

            if (logs.Count == 0)
                return NotFound(new { error = $"No audit logs found for instance '{instanceId}'" });
            
            Response.Headers.Add("X-Total-Count", total.ToString());
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log for instance {InstanceId}", instanceId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a single audit log entry by ID. Useful for referencing
    /// a specific action or change documented in the audit trail.
    /// </summary>
    [HttpGet("{auditId}")]
    [ProducesResponseType(typeof(AuditLogEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuditLogEntry(string auditId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(auditId))
                return BadRequest(new { error = "Audit ID cannot be empty" });

            _logger.LogInformation("Retrieving audit log entry: {AuditId}", auditId);

            var entry = await _auditRepository.GetByIdAsync(auditId);

            if (entry == null)
                return NotFound(new { error = $"Audit log entry '{auditId}' not found" });

            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log entry {AuditId}", auditId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets summary statistics about audit log activity for monitoring and
    /// analytics purposes. Returns counts by action type and date distribution.
    /// </summary>
    [HttpGet("stats/summary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuditStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            _logger.LogInformation("Retrieving audit statistics");

            var (allLogs, totalEntries) = await _auditService.GetFilteredAuditLogsAsync(
                fromDate: fromDate,
                toDate: toDate,
                take: int.MaxValue // Retrieve all logs within the date range
            );

            var entriesByAction = allLogs.GroupBy(e => e.EventType)
                                         .ToDictionary(g => g.Key, g => g.Count());

            var entriesByDay = allLogs.GroupBy(e => e.Timestamp.Date)
                                      .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

            var stats = new
            {
                totalEntries = totalEntries,
                dateRange = new { from = fromDate, to = toDate },
                entriesByAction = entriesByAction,
                entriesByDay = entriesByDay
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports audit logs in the specified format (json, csv, xml).
    /// Useful for compliance reporting and external analysis.
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string format = "json",
        [FromQuery] string? workflowId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var validFormats = new[] { "json", "csv", "xml" };
            if (!validFormats.Contains(format.ToLowerInvariant()))
                return BadRequest(new { error = $"Invalid format. Supported: {string.Join(", ", validFormats)}" });

            _logger.LogInformation("Exporting audit logs in {Format}", format);

            var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(
                workflowId: workflowId,
                fromDate: fromDate,
                toDate: toDate,
                take: int.MaxValue // Get all filtered logs for export
            );

            byte[] exportData;
            string contentType;
            string fileName;

            switch (format.ToLowerInvariant())
            {
                case "csv":
                    var csvString = await _csvFormatter.FormatAsync(logs.OrderBy(e => e.Timestamp));
                    exportData = System.Text.Encoding.UTF8.GetBytes(csvString);
                    contentType = "text/csv";
                    fileName = $"audit-export-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.csv";
                    break;
                case "json":
                    exportData = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(logs, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    contentType = "application/json";
                    fileName = $"audit-export-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";
                    break;
                default:
                    return BadRequest(new { error = $"Unsupported export format: {format}" });
            }

            return File(exportData, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }


}
