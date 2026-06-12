namespace Blazix.BaseUI.Docs.Client.Data;

public sealed record ComponentDoc(
    string Slug,
    string Name,
    string ShortName,
    string Status,
    string Summary,
    string Usage,
    string Example,
    string[] Parts,
    string[] Keyboard,
    string[] Notes);
