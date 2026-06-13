namespace Blazix.BaseUI.Docs.Client.Data;

public sealed record ComponentPart(
    string Name,
    string Description,
    IReadOnlyList<ApiRow> Parameters,
    IReadOnlyList<ApiRow> DataAttributes,
    IReadOnlyList<ApiRow> CssVariables);
