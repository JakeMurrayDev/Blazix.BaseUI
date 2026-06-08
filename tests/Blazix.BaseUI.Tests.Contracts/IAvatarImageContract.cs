namespace Blazix.BaseUI.Tests.Contracts;

public interface IAvatarImageContract
{
    Task RendersWhenLoaded();
    Task DoesNotRenderWhenNotLoaded();
    Task UpdatesStatusOnLoad();
    Task UpdatesStatusOnError();
    Task InvokesOnLoadingStatusChange();
    Task ForwardsAttributes();
    Task RequiresContext();
}
