namespace Blazix.BaseUI.Utilities;

/// <summary>
/// Suppresses a specific slopwatch rule for the attributed code element.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public sealed class SlopwatchSuppressAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlopwatchSuppressAttribute"/> class.
    /// </summary>
    /// <param name="ruleId">The rule ID to suppress (e.g., "SW003").</param>
    /// <param name="justification">The reason for the suppression (must be at least 20 characters).</param>
    public SlopwatchSuppressAttribute(string ruleId, string justification)
    {
        RuleId = ruleId;
        Justification = justification;
    }

    /// <summary>
    /// Gets the rule ID being suppressed.
    /// </summary>
    public string RuleId { get; }

    /// <summary>
    /// Gets the justification for the suppression.
    /// </summary>
    public string Justification { get; }
}
