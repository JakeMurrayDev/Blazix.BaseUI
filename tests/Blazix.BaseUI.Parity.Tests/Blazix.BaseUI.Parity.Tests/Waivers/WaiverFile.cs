using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Blazix.BaseUI.Parity.Tests.Capture;
using Blazix.BaseUI.Parity.Tests.Diff;

namespace Blazix.BaseUI.Parity.Tests.Waivers;

/// <summary>Strictly loads the reviewed waiver registry.</summary>
/// <remarks>
/// This offline layer validates repository link shape and future expiry without making a network
/// request. A deferred defect remains <see cref="WaiverIssuePolicyStatus.Unverified"/> and blocks in
/// <see cref="WaiverMatcher"/> unless the caller injects an
/// <see cref="IWaiverIssuePolicyValidator"/> that confirms the issue is open, owned, documents at
/// least two attempts, and has acceptance criteria. The policy supplies no numeric definition of a
/// "short" expiry, so this loader does not invent one.
/// </remarks>
public static partial class WaiverFile
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "fixture",
        "leg",
        "step",
        "nodePath",
        "kind",
        "property",
        "propertyMatch",
        "reason",
        "disposition",
        "docLink",
        "expires"
    };

    /// <summary>Loads the standard waiver file using today's UTC date as review date.</summary>
    /// <returns>The validated waivers in file order.</returns>
    public static IReadOnlyList<Waiver> Load()
        => Load(Infrastructure.ParityPaths.Waivers, DateOnly.FromDateTime(DateTime.UtcNow));

    /// <summary>Loads and validates a waiver file.</summary>
    /// <param name="path">The JSON file path.</param>
    /// <param name="reviewDate">The date on which expiry is evaluated.</param>
    /// <returns>The validated waivers in file order.</returns>
    public static IReadOnlyList<Waiver> Load(string path, DateOnly reviewDate)
        => Load(path, reviewDate, issuePolicyValidator: null);

    /// <summary>Loads and validates a waiver file with optional live issue-policy evidence.</summary>
    /// <param name="path">The JSON file path.</param>
    /// <param name="reviewDate">The date on which expiry is evaluated.</param>
    /// <param name="issuePolicyValidator">
    /// The validator for deferred issue state, or <see langword="null"/> for offline loading.
    /// </param>
    /// <returns>The validated waivers in file order.</returns>
    public static IReadOnlyList<Waiver> Load(
        string path,
        DateOnly reviewDate,
        IWaiverIssuePolicyValidator? issuePolicyValidator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        JsonDocument document;

        var json = File.ReadAllText(path);

        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new FormatException("Waiver file contains invalid JSON.", exception);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw Failure(-1, "$", "must be a JSON array");
            }

            var waivers = new List<Waiver>();
            var index = 0;

            foreach (var element in document.RootElement.EnumerateArray())
            {
                waivers.Add(Read(element, index, reviewDate, issuePolicyValidator));
                index++;
            }

            var duplicate = waivers
                .Select((waiver, waiverIndex) => new { Waiver = waiver, Index = waiverIndex })
                .GroupBy(item => new MatchIdentity(item.Waiver), MatchIdentityComparer.Instance)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
            {
                var duplicateIndex = duplicate.Skip(1).First().Index;
                throw Failure(duplicateIndex, "fixture", "duplicates an earlier waiver identity");
            }

            return waivers;
        }
    }

    private static Waiver Read(
        JsonElement element,
        int index,
        DateOnly reviewDate,
        IWaiverIssuePolicyValidator? issuePolicyValidator)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Failure(index, "$", "must be a JSON object");
        }

        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var property in element.EnumerateObject())
        {
            if (!AllowedFields.Contains(property.Name))
            {
                throw Failure(index, property.Name, "is unknown");
            }

            if (!properties.TryAdd(property.Name, property.Value))
            {
                throw Failure(index, property.Name, "appears more than once");
            }
        }

        var fixture = RequiredString(properties, index, "fixture");
        var leg = RequiredEnum<ParityLeg>(properties, index, "leg");
        var step = RequiredString(properties, index, "step");
        var nodePath = RequiredIdentityString(properties, index, "nodePath");
        var kind = RequiredEnum<FindingKind>(properties, index, "kind");
        var propertyValue = RequiredIdentityString(properties, index, "property");
        var propertyMatch = OptionalPropertyMatch(properties, index);
        var reason = RequiredString(properties, index, "reason");
        var disposition = RequiredDisposition(properties, index);
        var docLink = RequiredString(properties, index, "docLink");
        var expires = RequiredDate(properties, index, "expires");

        ValidateNoWildcard(index, "fixture", fixture);
        ValidateNoWildcard(index, "step", step);

        if (expires <= reviewDate)
        {
            throw Failure(index, "expires", $"must be later than {reviewDate:yyyy-MM-dd}");
        }

        if (ComparatorRegistry.NonWaivableKinds.Contains(kind))
        {
            throw Failure(index, "kind", $"'{kind}' is nonwaivable");
        }

        var waiver = new Waiver
        {
            Fixture = fixture,
            Leg = leg,
            Step = step,
            NodePath = nodePath,
            Kind = kind,
            Property = propertyValue,
            PropertyMatch = propertyMatch,
            Reason = reason,
            Disposition = disposition,
            DocLink = docLink,
            Expires = expires
        };

        ValidatePolicy(waiver, index);
        return ValidateIssuePolicy(waiver, index, issuePolicyValidator);
    }

    internal static void ValidatePolicy(Waiver waiver, int index)
    {
        ArgumentNullException.ThrowIfNull(waiver);

        ValidateRuntimeString(index, "fixture", waiver.Fixture, allowEmpty: false);
        ValidateRuntimeString(index, "step", waiver.Step, allowEmpty: false);
        ValidateRuntimeString(index, "nodePath", waiver.NodePath, allowEmpty: true);
        ValidateRuntimeString(index, "property", waiver.Property, allowEmpty: true);
        ValidateRuntimeString(index, "reason", waiver.Reason, allowEmpty: false);
        ValidateRuntimeString(index, "docLink", waiver.DocLink, allowEmpty: false);
        ValidateNoWildcard(index, "fixture", waiver.Fixture);
        ValidateNoWildcard(index, "step", waiver.Step);
        ValidateNoWildcard(index, "nodePath", waiver.NodePath);
        ValidateNoWildcard(index, "property", waiver.Property);

        if (!Infrastructure.FixtureExecution.IsExecutionId(waiver.Fixture))
        {
            throw Failure(
                index,
                "fixture",
                "must be one safe fixture id followed by exactly one '@light' or '@dark' suffix");
        }

        if (!Enum.IsDefined(waiver.Leg))
        {
            throw Failure(index, "leg", $"has invalid value '{waiver.Leg}'");
        }

        if (!Enum.IsDefined(waiver.Kind))
        {
            throw Failure(index, "kind", $"has invalid value '{waiver.Kind}'");
        }

        if (!Enum.IsDefined(waiver.PropertyMatch))
        {
            throw Failure(
                index,
                "propertyMatch",
                $"has invalid value '{waiver.PropertyMatch}'");
        }

        if (!Enum.IsDefined(waiver.Disposition))
        {
            throw Failure(
                index,
                "disposition",
                $"has invalid value '{waiver.Disposition}'");
        }

        if (!Enum.IsDefined(waiver.IssuePolicyStatus))
        {
            throw Failure(
                index,
                "docLink",
                $"has invalid issue policy status '{waiver.IssuePolicyStatus}'");
        }

        if (waiver.Disposition == WaiverDisposition.AcceptedLimitation &&
            waiver.IssuePolicyStatus != WaiverIssuePolicyStatus.NotRequired)
        {
            throw Failure(index, "docLink", "accepted limitations must not carry issue validation");
        }

        if (waiver.Disposition == WaiverDisposition.DeferredDefect &&
            waiver.IssuePolicyStatus == WaiverIssuePolicyStatus.NotRequired)
        {
            throw Failure(index, "docLink", "deferred defects require issue validation state");
        }

        if (ComparatorRegistry.NonWaivableKinds.Contains(waiver.Kind))
        {
            throw Failure(index, "kind", $"'{waiver.Kind}' is nonwaivable");
        }

        ValidateLink(index, waiver.Disposition, waiver.DocLink);
        ValidatePropertyMatch(
            index,
            waiver.Kind,
            waiver.Property,
            waiver.PropertyMatch,
            waiver.Reason,
            waiver.Disposition,
            waiver.DocLink);
    }

    private static Waiver ValidateIssuePolicy(
        Waiver waiver,
        int index,
        IWaiverIssuePolicyValidator? issuePolicyValidator)
    {
        if (waiver.Disposition != WaiverDisposition.DeferredDefect ||
            issuePolicyValidator is null)
        {
            return waiver;
        }

        try
        {
            return waiver.VerifyIssuePolicy(issuePolicyValidator);
        }
        catch (InvalidOperationException exception)
        {
            throw Failure(index, "docLink", exception.Message);
        }
    }

    private static string RequiredString(
        IReadOnlyDictionary<string, JsonElement> properties,
        int index,
        string field)
    {
        if (!properties.TryGetValue(field, out var element))
        {
            throw Failure(index, field, "is required");
        }

        if (element.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(element.GetString()))
        {
            throw Failure(index, field, "must be a non-whitespace string");
        }

        return element.GetString()!;
    }

    private static string RequiredIdentityString(
        IReadOnlyDictionary<string, JsonElement> properties,
        int index,
        string field)
    {
        if (!properties.TryGetValue(field, out var element))
        {
            throw Failure(index, field, "is required");
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw Failure(index, field, "must be a string");
        }

        var value = element.GetString()!;
        if (value.Length > 0 && string.IsNullOrWhiteSpace(value))
        {
            throw Failure(index, field, "must not contain only whitespace");
        }

        if (string.Equals(value, "*", StringComparison.Ordinal))
        {
            throw Failure(index, field, "must not use a wildcard");
        }

        return value;
    }

    private static TEnum RequiredEnum<TEnum>(
        IReadOnlyDictionary<string, JsonElement> properties,
        int index,
        string field)
        where TEnum : struct, Enum
    {
        var value = RequiredString(properties, index, field);
        if (!Enum.TryParse<TEnum>(value, ignoreCase: false, out var result) ||
            !Enum.IsDefined(result))
        {
            throw Failure(index, field, $"has invalid value '{value}'");
        }

        return result;
    }

    private static WaiverPropertyMatch OptionalPropertyMatch(
        IReadOnlyDictionary<string, JsonElement> properties,
        int index)
    {
        if (!properties.ContainsKey("propertyMatch"))
        {
            return WaiverPropertyMatch.Exact;
        }

        return RequiredString(properties, index, "propertyMatch") switch
        {
            "exact" => WaiverPropertyMatch.Exact,
            "prefix" => WaiverPropertyMatch.Prefix,
            var value => throw Failure(index, "propertyMatch", $"has invalid value '{value}'")
        };
    }

    private static WaiverDisposition RequiredDisposition(
        IReadOnlyDictionary<string, JsonElement> properties,
        int index)
        => RequiredString(properties, index, "disposition") switch
        {
            "accepted-limitation" => WaiverDisposition.AcceptedLimitation,
            "deferred-defect" => WaiverDisposition.DeferredDefect,
            var value => throw Failure(index, "disposition", $"has invalid value '{value}'")
        };

    private static DateOnly RequiredDate(
        IReadOnlyDictionary<string, JsonElement> properties,
        int index,
        string field)
    {
        var value = RequiredString(properties, index, field);
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            throw Failure(index, field, "must be an ISO date in yyyy-MM-dd form");
        }

        return result;
    }

    private static void ValidateLink(int index, WaiverDisposition disposition, string docLink)
    {
        if (disposition == WaiverDisposition.AcceptedLimitation)
        {
            var normalized = docLink.Replace('\\', '/');
            var isDocumentation =
                normalized.StartsWith("docs/audits/", StringComparison.Ordinal) ||
                normalized.StartsWith("docs/superpowers/specs/", StringComparison.Ordinal);

            if (!isDocumentation ||
                !normalized.EndsWith(".md", StringComparison.Ordinal) ||
                normalized.Split('/').Contains("..", StringComparer.Ordinal) ||
                Uri.TryCreate(docLink, UriKind.Absolute, out _))
            {
                throw Failure(
                    index,
                    "docLink",
                    "must be a repository-relative Markdown audit or spec");
            }

            return;
        }

        if (!IssueLinkRegex().IsMatch(docLink))
        {
            throw Failure(
                index,
                "docLink",
                "must be an issue URL for JakeMurrayDev/Blazix.BaseUI");
        }
    }

    private static void ValidateRuntimeString(
        int index,
        string field,
        string? value,
        bool allowEmpty)
    {
        if (value is null || (!allowEmpty && string.IsNullOrWhiteSpace(value)))
        {
            throw Failure(index, field, "must be a non-whitespace string");
        }

        if (allowEmpty && value.Length > 0 && string.IsNullOrWhiteSpace(value))
        {
            throw Failure(index, field, "must not contain only whitespace");
        }
    }

    private static void ValidateNoWildcard(int index, string field, string value)
    {
        if (string.Equals(value, "*", StringComparison.Ordinal))
        {
            throw Failure(index, field, "must not use a wildcard");
        }
    }

    private static void ValidatePropertyMatch(
        int index,
        FindingKind kind,
        string property,
        WaiverPropertyMatch propertyMatch,
        string reason,
        WaiverDisposition disposition,
        string docLink)
    {
        if (propertyMatch == WaiverPropertyMatch.Exact)
        {
            return;
        }

        if (kind != FindingKind.Console)
        {
            throw Failure(index, "propertyMatch", "prefix is allowed only for Console");
        }

        if (!ConsolePrefixRegex().IsMatch(property))
        {
            throw Failure(
                index,
                "property",
                "must contain a console level and a non-empty stable semantic stem");
        }

        if (disposition != WaiverDisposition.DeferredDefect)
        {
            throw Failure(index, "disposition", "Console prefix waivers must be deferred-defect");
        }

        if (!IssueLinkRegex().IsMatch(docLink))
        {
            throw Failure(index, "docLink", "Console prefix waivers must link a tracking issue");
        }

        var marker = reason.IndexOf("volatile suffix:", StringComparison.OrdinalIgnoreCase);
        if (marker < 0 ||
            string.IsNullOrWhiteSpace(reason[(marker + "volatile suffix:".Length)..]))
        {
            throw Failure(
                index,
                "reason",
                "must name the observed token after 'volatile suffix:'");
        }
    }

    private static FormatException Failure(int index, string field, string detail)
        => new(index < 0
            ? $"Waiver file field '{field}' {detail}."
            : $"Waiver entry {index} field '{field}' {detail}.");

    [GeneratedRegex(
        @"^https://github\.com/JakeMurrayDev/Blazix\.BaseUI/issues/[1-9][0-9]*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex IssueLinkRegex();

    [GeneratedRegex(@"^(error|warning):\s+\S(.*\S)?$", RegexOptions.CultureInvariant)]
    private static partial Regex ConsolePrefixRegex();

    private sealed record MatchIdentity(
        string Fixture,
        ParityLeg Leg,
        string Step,
        string NodePath,
        FindingKind Kind,
        string Property)
    {
        internal MatchIdentity(Waiver waiver)
            : this(
                waiver.Fixture,
                waiver.Leg,
                waiver.Step,
                waiver.NodePath,
                waiver.Kind,
                waiver.Property)
        {
        }
    }

    private sealed class MatchIdentityComparer : IEqualityComparer<MatchIdentity>
    {
        internal static MatchIdentityComparer Instance { get; } = new();

        public bool Equals(MatchIdentity? left, MatchIdentity? right)
            => left is not null &&
               right is not null &&
               left.Leg == right.Leg &&
               left.Kind == right.Kind &&
               string.Equals(left.Fixture, right.Fixture, StringComparison.Ordinal) &&
               string.Equals(left.Step, right.Step, StringComparison.Ordinal) &&
               string.Equals(left.NodePath, right.NodePath, StringComparison.Ordinal) &&
               string.Equals(left.Property, right.Property, StringComparison.Ordinal);

        public int GetHashCode(MatchIdentity item)
        {
            var hash = new HashCode();
            hash.Add(item.Fixture, StringComparer.Ordinal);
            hash.Add(item.Leg);
            hash.Add(item.Step, StringComparer.Ordinal);
            hash.Add(item.NodePath, StringComparer.Ordinal);
            hash.Add(item.Kind);
            hash.Add(item.Property, StringComparer.Ordinal);
            return hash.ToHashCode();
        }
    }
}
