namespace Blazix.BaseUI.Tests.Contracts.Toast;

public interface IToastContract
{
    Task RepeatedPauseAndResumeDoesNotExtendToastTimeout();

    Task ReAddingAClosingToastClearsRetainedSwipeState();

    Task ShiftTabInsideViewportKeepsTimersPaused();
}
