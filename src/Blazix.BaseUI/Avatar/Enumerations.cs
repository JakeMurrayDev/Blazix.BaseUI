namespace Blazix.BaseUI.Avatar;

/// <summary>
/// Represents the loading status of an avatar image.
/// </summary>
public enum ImageLoadingStatus
{
    /// <summary>
    /// No image loading has been initiated.
    /// </summary>
    Idle,

    /// <summary>
    /// The image is currently being loaded.
    /// </summary>
    Loading,

    /// <summary>
    /// The image has been successfully loaded.
    /// </summary>
    Loaded,

    /// <summary>
    /// The image failed to load.
    /// </summary>
    Error
}
