namespace BlazorBaseUI;

/// <summary>
/// Manages the transition state machine for popup/panel Root components.
/// Encapsulates the <see cref="TransitionStatus"/> + <c>IsMounted</c> lifecycle
/// that is common across Dialog, Popover, Tooltip, PreviewCard, Menu, Select,
/// and NavigationMenu Root components.
/// <para>
/// Maps to the combined behavior of React's <c>useTransitionStatus</c> +
/// <c>useOpenStateTransitions</c> hooks.
/// </para>
/// </summary>
internal sealed class TransitionLifecycleManager
{
    private readonly TransitionStatus restingState;

    /// <summary>
    /// Initializes a new instance of <see cref="TransitionLifecycleManager"/>.
    /// </summary>
    /// <param name="useIdleRestingState">
    /// When <see langword="true"/>, the resting state after transitions is
    /// <see cref="TransitionStatus.Idle"/> (Dialog pattern).
    /// When <see langword="false"/>, the resting state is
    /// <see cref="TransitionStatus.Undefined"/> (standard popup pattern).
    /// </param>
    public TransitionLifecycleManager(bool useIdleRestingState = false)
    {
        restingState = useIdleRestingState ? TransitionStatus.Idle : TransitionStatus.Undefined;
    }

    /// <summary>
    /// Gets the current transition status.
    /// </summary>
    public TransitionStatus TransitionStatus { get; private set; } = TransitionStatus.Undefined;

    /// <summary>
    /// Gets whether the component is currently mounted in the DOM.
    /// </summary>
    public bool IsMounted { get; private set; }

    /// <summary>
    /// Begins an opening transition. Sets status to <see cref="TransitionStatus.Starting"/>
    /// and marks the component as mounted.
    /// </summary>
    public void BeginOpen()
    {
        TransitionStatus = TransitionStatus.Starting;
        IsMounted = true;
    }

    /// <summary>
    /// Begins a closing transition. Sets status to <see cref="TransitionStatus.Ending"/>.
    /// The component remains mounted to allow exit animations.
    /// </summary>
    public void BeginClose()
    {
        TransitionStatus = TransitionStatus.Ending;
    }

    /// <summary>
    /// Handles the starting style applied callback from JavaScript.
    /// Transitions from <see cref="TransitionStatus.Starting"/> to the resting state.
    /// </summary>
    /// <returns><see langword="true"/> if the transition was applied; <see langword="false"/> if the current status was not <see cref="TransitionStatus.Starting"/>.</returns>
    public bool HandleStartingStyleApplied()
    {
        if (TransitionStatus != TransitionStatus.Starting)
        {
            return false;
        }

        TransitionStatus = restingState;
        return true;
    }

    /// <summary>
    /// Handles the transition end callback from JavaScript.
    /// Transitions to the resting state. When closing (<paramref name="open"/> is <see langword="false"/>)
    /// and <paramref name="preventUnmount"/> is <see langword="false"/>, the component is unmounted.
    /// </summary>
    /// <param name="open">Whether the component is open after the transition.</param>
    /// <param name="preventUnmount">When <see langword="true"/>, the component remains mounted even when closing.</param>
    /// <returns><see langword="true"/> if the component was unmounted.</returns>
    public bool HandleTransitionEnd(bool open, bool preventUnmount = false)
    {
        TransitionStatus = restingState;

        if (open)
        {
            IsMounted = true;
            return false;
        }

        if (!preventUnmount)
        {
            IsMounted = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Immediately resets the transition state to <see cref="TransitionStatus.Undefined"/>
    /// and unmounts the component. Used when a component must be forcibly removed
    /// without waiting for animations.
    /// </summary>
    public void ForceUnmount()
    {
        TransitionStatus = TransitionStatus.Undefined;
        IsMounted = false;
    }
}
