namespace Blazix.BaseUI.Tests.Contracts;

public interface IAvatarFallbackContract
{
    Task RendersWhenImageFails();
    Task DoesNotRenderWhenImageLoaded();
    Task RendersAsSpanByDefault();
    Task DoesNotShowBeforeDelayElapsed();
    Task ShowsAfterDelayElapsed();
    Task ShowsImmediatelyWhenNoDelay();
    Task ReceivesCorrectState();
    Task RequiresContext();
}
