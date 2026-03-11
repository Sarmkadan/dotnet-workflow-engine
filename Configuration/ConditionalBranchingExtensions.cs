// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetWorkflowEngine.Configuration;

/// <summary>
/// <see cref="IServiceCollection"/> extension methods for registering conditional
/// branching support in the workflow engine.
/// </summary>
public static class ConditionalBranchingExtensions
{
    /// <summary>
    /// Registers <see cref="ConditionalBranchingService"/> and its supporting
    /// configuration into the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Optional delegate to customise <see cref="ConditionalBranchingOptions"/>.
    /// When omitted, default options are used.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddConditionalBranching(opts =>
    /// {
    ///     opts.ThrowOnNoBranchSelected    = true;
    ///     opts.ValidateExpressionsOnLoad  = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddConditionalBranching(
        this IServiceCollection services,
        Action<ConditionalBranchingOptions>? configure = null)
    {
        var options = new ConditionalBranchingOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<ConditionalBranchingService>();

        return services;
    }
}

/// <summary>
/// Configuration options that govern how the conditional branching service behaves
/// at runtime and during workflow load.
/// </summary>
public class ConditionalBranchingOptions
{
    /// <summary>
    /// Gets or sets whether to propagate an exception when a transition expression
    /// fails to evaluate.
    /// <para>
    /// When <see langword="false"/> (the default), evaluation errors are recorded in
    /// <see cref="Models.BranchingResult.EvaluationErrors"/> and the failing transition is
    /// treated as non-matching so that other branches can still be followed.
    /// </para>
    /// </summary>
    public bool ThrowOnExpressionError { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to throw a <see cref="Exceptions.WorkflowException"/> when
    /// branch resolution yields no selected transitions and no default transition is defined.
    /// <para>
    /// When <see langword="false"/> (the default), the workflow silently stops advancing
    /// from that activity, which is appropriate for terminal activities with no successors.
    /// Set to <see langword="true"/> in strict-routing scenarios where every activity must
    /// lead somewhere.
    /// </para>
    /// </summary>
    public bool ThrowOnNoBranchSelected { get; set; } = false;

    /// <summary>
    /// Gets or sets whether <see cref="ConditionalBranchingService.ValidateTransitionExpressions"/>
    /// is called automatically when a workflow is loaded or published.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool ValidateExpressionsOnLoad { get; set; } = true;
}
