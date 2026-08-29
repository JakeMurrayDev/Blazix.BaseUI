namespace Blazix.BaseUI;

/// <summary>
/// Tracks which component instance currently owns a registered element id, so a clear issued by a
/// replaced instance cannot drop the incoming instance's registration.
/// </summary>
/// <remarks>
/// Blazor disposes a removed component <em>after</em> its replacement has initialized, the reverse
/// of React's cleanup ordering. Upstream guards the clear with id equality
/// (base-ui <c>#5340</c>), which is not sufficient here: a replacement that reuses the same
/// explicit id would still have its registration cleared. Ownership is therefore tracked by
/// instance.
/// </remarks>
internal sealed class RegisteredIdOwner
{
    private object? owner;

    /// <summary>
    /// Determines whether a registration write from <paramref name="instance"/> should be applied.
    /// </summary>
    /// <param name="instance">The component instance registering or clearing the id.</param>
    /// <param name="id">The id being registered, or <see langword="null"/> to clear it.</param>
    /// <returns>
    /// <see langword="true"/> when the write should be applied; <see langword="false"/> when a
    /// clear arrived from an instance that no longer owns the registration.
    /// </returns>
    public bool ShouldApply(object instance, string? id)
    {
        if (id is not null)
        {
            owner = instance;
            return true;
        }

        if (!ReferenceEquals(owner, instance))
        {
            return false;
        }

        owner = null;
        return true;
    }
}
