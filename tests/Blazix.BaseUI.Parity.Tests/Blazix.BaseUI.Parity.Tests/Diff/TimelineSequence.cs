using Blazix.BaseUI.Parity.Tests.Capture;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Reduces a recorded timeline to the ordered signature the two legs are compared on.
/// </summary>
/// <remarks>
/// <para>
/// The timestamps are dropped outright. A Blazor Server leg runs every state change through
/// a circuit round trip, so it reaches each phase later than React does and, on a loaded
/// machine, by a different amount each run. What has to match is the order of the phases,
/// not when they happened; how long a run took is compared separately, against the duration
/// that leg's own CSS declares.
/// </para>
/// <para>
/// The attribute allowlist is applied to attribute mutations and to nothing else. Applied to
/// every event it would drop the transition and lifecycle events too — their <c>attr</c> is
/// null or a CSS property name rather than an attribute — and the signature of an animation
/// would be empty, which is the one shape that makes every timeline compare equal.
/// </para>
/// </remarks>
public static class TimelineSequence
{
    /// <summary>
    /// The attribute mutations that carry a phase. Every other attribute a component writes
    /// during an animation — an <c>aria-*</c> flip, an id, a class rewrite — is noise that
    /// would swamp the phases and differ between the two legs for reasons that are not
    /// animation.
    /// </summary>
    private static readonly string[] TrackedAttributes =
    [
        "data-open", "data-closed", "data-starting-style", "data-ending-style",
        "data-side", "data-align", "hidden", "inert", "style"
    ];

    /// <summary>The kind whose <c>attr</c> is an attribute name.</summary>
    private const string AttributeKind = "attribute";

    /// <summary>
    /// The kind whose identity lives in <c>from</c>: <c>capture.js</c> cannot compute a path
    /// for a node that has already left the tree, so it records the departing tag instead.
    /// </summary>
    private const string RemovedKind = "removed";

    /// <summary>The kind emitted when a node is inserted into the observed tree.</summary>
    private const string AddedKind = "added";

    /// <summary>
    /// How a value that was not there at all is written, so that it cannot be read as an
    /// empty one. A marker set to the empty string and the same marker removed are the two
    /// halves of an open/close pair, and both write nothing into a signature that renders
    /// them the same way.
    /// </summary>
    private const string Absent = "<absent>";

    /// <summary>
    /// Reduces a timeline to its comparable signature.
    /// </summary>
    /// <param name="timeline">The events recorded for one step on one leg.</param>
    /// <returns>
    /// One signature per surviving event, in the order they were recorded.
    /// </returns>
    public static IReadOnlyList<string> Normalize(IReadOnlyList<TimelineEvent> timeline)
    {
        var animatedPaths = timeline
            .Where(IsAnimationLifecycle)
            .Select(recorded => recorded.Path)
            .ToHashSet(StringComparer.Ordinal);
        var retained = timeline.Where(recorded => ShouldRetain(recorded, timeline, animatedPaths)).ToArray();
        var signatures = new List<string>(timeline.Count);

        for (var index = 0; index < retained.Length;)
        {
            var runEnd = index + 1;

            while (runEnd < retained.Length &&
                   retained[runEnd].T == retained[index].T &&
                   IsOpenClosedPhase(retained[runEnd - 1]) &&
                   IsOpenClosedPhase(retained[runEnd]))
            {
                runEnd++;
            }

            if (runEnd > index + 1 && IsOpenClosedPhase(retained[index]))
            {
                var phaseSignatures = retained[index..runEnd]
                    .Select(Signature)
                    .ToArray();
                var sourceEquivalentSignatures = phaseSignatures
                    .Where((signature, phaseIndex) =>
                        phaseIndex == 0 ||
                        !string.Equals(
                            phaseSignatures[phaseIndex - 1],
                            signature,
                            StringComparison.Ordinal))
                    .Order(StringComparer.Ordinal);

                foreach (var signature in sourceEquivalentSignatures)
                {
                    signatures.Add(signature);
                }

                index = runEnd;
                continue;
            }

            var recorded = retained[index];
            AddSignature(
                signatures,
                Signature(recorded),
                string.Equals(recorded.Kind, AttributeKind, StringComparison.Ordinal));
            index++;
        }

        return signatures;
    }

    /// <summary>Applies only normalizations proven to be renderer or placement noise.</summary>
    private static bool ShouldRetain(
        TimelineEvent recorded,
        IReadOnlyList<TimelineEvent> timeline,
        IReadOnlySet<string> animatedPaths)
    {
        var isAttribute = string.Equals(recorded.Kind, AttributeKind, StringComparison.Ordinal);

        if (isAttribute && !TrackedAttributes.Contains(recorded.Attr, StringComparer.Ordinal))
        {
            return false;
        }

        if (isAttribute &&
            IsPlacementAttribute(recorded.Attr) &&
            string.Equals(recorded.From, recorded.To, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(recorded.Kind, AddedKind, StringComparison.Ordinal) ||
            animatedPaths.Contains(recorded.Path))
        {
            return true;
        }

        return !timeline.Any(candidate =>
            candidate.T == recorded.T &&
            string.Equals(candidate.Kind, AddedKind, StringComparison.Ordinal) &&
            IsAncestorPath(candidate.Path, recorded.Path));
    }

    /// <summary>Reports whether an insertion path is a strict ancestor of another path.</summary>
    private static bool IsAncestorPath(string ancestor, string descendant)
        => descendant.StartsWith(ancestor + " > ", StringComparison.Ordinal);

    /// <summary>Reports whether the event identifies a real CSS run on a captured node.</summary>
    private static bool IsAnimationLifecycle(TimelineEvent recorded)
        => recorded.Kind.StartsWith("transition", StringComparison.Ordinal) ||
           recorded.Kind.StartsWith("animation", StringComparison.Ordinal);

    /// <summary>Reports whether an attribute is transient placement output.</summary>
    private static bool IsPlacementAttribute(string? attribute)
        => string.Equals(attribute, "data-side", StringComparison.Ordinal) ||
           string.Equals(attribute, "data-align", StringComparison.Ordinal);

    /// <summary>Reports whether an event is one of the paired public open/closed markers.</summary>
    private static bool IsOpenClosedPhase(TimelineEvent recorded)
        => string.Equals(recorded.Kind, AttributeKind, StringComparison.Ordinal) &&
           (string.Equals(recorded.Attr, "data-open", StringComparison.Ordinal) ||
            string.Equals(recorded.Attr, "data-closed", StringComparison.Ordinal));

    /// <summary>Adds a signature while retaining the existing adjacent-attribute deduplication.</summary>
    private static void AddSignature(List<string> signatures, string signature, bool isAttribute)
    {
        // Consecutive duplicates are collapsed for attribute writes only. The same value
        // written to the same attribute twice in a row is a redundant write and not a
        // phase, whereas every removal in a step normalizes to the same signature — a
        // removal carries no path — so collapsing those would report parity between a
        // dialog that unmounted two nodes and one that unmounted one.
        if (isAttribute &&
            signatures.Count > 0 &&
            string.Equals(signatures[^1], signature, StringComparison.Ordinal))
        {
            return;
        }

        signatures.Add(signature);
    }

    /// <summary>
    /// Writes one event as the text it is compared and diffed as.
    /// </summary>
    /// <param name="recorded">The event.</param>
    /// <returns>The signature.</returns>
    private static string Signature(TimelineEvent recorded)
        => $"{recorded.Kind}:{recorded.Path}:{recorded.Attr}:{Identity(recorded)}";

    /// <summary>
    /// Reads the value that says which node or state an event is about.
    /// </summary>
    /// <remarks>
    /// It is <c>to</c> for every kind but a removal, which has no <c>to</c> and no path: its
    /// only identity is the tag <c>capture.js</c> writes into <c>from</c>. Without that a
    /// dialog dropping its backdrop and one dropping its popup normalize identically.
    /// </remarks>
    /// <param name="recorded">The event.</param>
    /// <returns>The identity, or <see cref="Absent"/> when the event carries none.</returns>
    private static string Identity(TimelineEvent recorded)
    {
        var value = string.Equals(recorded.Kind, RemovedKind, StringComparison.Ordinal)
            ? recorded.From
            : recorded.To;

        if (value is null)
        {
            return Absent;
        }

        return string.Equals(recorded.Kind, AttributeKind, StringComparison.Ordinal) &&
               string.Equals(recorded.Attr, "style", StringComparison.Ordinal) &&
               TryCanonicalizeStyle(value, out var canonical)
            ? canonical
            : value;
    }

    /// <summary>Canonicalizes only valid inline declaration lists with unique properties.</summary>
    private static bool TryCanonicalizeStyle(string style, out string canonical)
    {
        if (!TrySplitStyle(style, out var rawDeclarations))
        {
            canonical = style;
            return false;
        }

        var declarations = new List<(string Property, string Serialized)>(rawDeclarations.Count);

        foreach (var raw in rawDeclarations)
        {
            if (!TrySplitDeclaration(raw, out var property, out var value))
            {
                canonical = style;
                return false;
            }

            declarations.Add((property, $"{property}:{value}"));
        }

        if (declarations
            .GroupBy(declaration => declaration.Property, CssPropertyComparer.Instance)
            .Any(group => group.Count() > 1))
        {
            canonical = string.Join(';', declarations.Select(declaration => declaration.Serialized));
            return true;
        }

        canonical = string.Join(
            ';',
            declarations
                .OrderBy(declaration => declaration.Property, CssPropertyComparer.Instance)
                .Select(declaration => declaration.Serialized));
        return true;
    }

    /// <summary>Splits a declaration list without treating quoted or functional semicolons as separators.</summary>
    private static bool TrySplitStyle(string style, out List<string> declarations)
    {
        declarations = [];
        var start = 0;
        var quote = '\0';
        var escaped = false;
        var parentheses = 0;

        for (var index = 0; index < style.Length; index++)
        {
            var character = style[index];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (quote != '\0' && character == '\\')
            {
                escaped = true;
                continue;
            }

            if (character is '\'' or '"')
            {
                if (quote == '\0')
                {
                    quote = character;
                }
                else if (quote == character)
                {
                    quote = '\0';
                }

                continue;
            }

            if (quote != '\0')
            {
                continue;
            }

            if (character == '(')
            {
                parentheses++;
            }
            else if (character == ')')
            {
                parentheses--;

                if (parentheses < 0)
                {
                    return false;
                }
            }
            else if (character == ';' && parentheses == 0)
            {
                AddDeclaration(style[start..index], declarations);
                start = index + 1;
            }
        }

        if (quote != '\0' || parentheses != 0)
        {
            return false;
        }

        AddDeclaration(style[start..], declarations);
        return true;
    }

    /// <summary>Adds a nonempty, whitespace-trimmed declaration.</summary>
    private static void AddDeclaration(string raw, ICollection<string> declarations)
    {
        var declaration = raw.Trim();

        if (declaration.Length > 0)
        {
            declarations.Add(declaration);
        }
    }

    /// <summary>Splits one declaration at its first top-level colon.</summary>
    private static bool TrySplitDeclaration(string declaration, out string property, out string value)
    {
        var quote = '\0';
        var escaped = false;
        var parentheses = 0;

        for (var index = 0; index < declaration.Length; index++)
        {
            var character = declaration[index];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (quote != '\0' && character == '\\')
            {
                escaped = true;
                continue;
            }

            if (character is '\'' or '"')
            {
                if (quote == '\0')
                {
                    quote = character;
                }
                else if (quote == character)
                {
                    quote = '\0';
                }

                continue;
            }

            if (quote != '\0')
            {
                continue;
            }

            if (character == '(')
            {
                parentheses++;
            }
            else if (character == ')')
            {
                parentheses--;
            }
            else if (character == ':' && parentheses == 0)
            {
                property = declaration[..index].Trim();
                value = declaration[(index + 1)..].Trim();
                return property.Length > 0 && value.Length > 0;
            }
        }

        property = string.Empty;
        value = string.Empty;
        return false;
    }

    /// <summary>Compares CSS property names while keeping custom properties case-sensitive.</summary>
    private sealed class CssPropertyComparer : IEqualityComparer<string>, IComparer<string>
    {
        internal static CssPropertyComparer Instance { get; } = new();

        public bool Equals(string? x, string? y)
            => ComparerFor(x, y).Equals(x, y);

        public int GetHashCode(string obj)
            => ComparerFor(obj, obj).GetHashCode(obj);

        public int Compare(string? x, string? y)
            => ComparerFor(x, y).Compare(x, y);

        private static StringComparer ComparerFor(string? x, string? y)
            => x?.StartsWith("--", StringComparison.Ordinal) == true ||
               y?.StartsWith("--", StringComparison.Ordinal) == true
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;
    }
}
