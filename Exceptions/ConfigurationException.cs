// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

namespace DotNetWorkflowEngine.Exceptions;

/// <summary>
/// Exception thrown when configuration-related errors occur.
/// </summary>
public class ConfigurationException : WorkflowException
{
    /// <summary>
    /// Gets the configuration key that caused the exception.
    /// </summary>
    public string? ConfigurationKey { get; }

    /// <summary>
    /// Gets the configuration value that caused the exception.
    /// </summary>
    public string? ConfigurationValue { get; }

    /// <summary>
    /// Initializes a new instance of the ConfigurationException class.
    /// </summary>
    public ConfigurationException(string message) : base(message, "CONFIGURATION_ERROR")
    {
    }

    /// <summary>
    /// Initializes a new instance with configuration key information.
    /// </summary>
    public ConfigurationException(string message, string configurationKey) : base(message, "CONFIGURATION_ERROR")
    {
        ConfigurationKey = configurationKey;
    }

    /// <summary>
    /// Initializes a new instance with configuration key and value information.
    /// </summary>
    public ConfigurationException(string message, string configurationKey, string configurationValue) : base(message, "CONFIGURATION_ERROR")
    {
        ConfigurationKey = configurationKey;
        ConfigurationValue = configurationValue;
    }

    /// <summary>
    /// Initializes a new instance with inner exception.
    /// </summary>
    public ConfigurationException(string message, Exception innerException) : base(message, "CONFIGURATION_ERROR", null, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance with complete information.
    /// </summary>
    public ConfigurationException(string message, string errorCode, string configurationKey, string? configurationValue = null, Exception? innerException = null) : base(message, errorCode, null, innerException)
    {
        ConfigurationKey = configurationKey;
        ConfigurationValue = configurationValue;
    }
}