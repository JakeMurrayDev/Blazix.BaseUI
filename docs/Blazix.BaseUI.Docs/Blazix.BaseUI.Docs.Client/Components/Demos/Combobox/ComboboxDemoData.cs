using Blazix.BaseUI.Combobox;

namespace Blazix.BaseUI.Docs.Client.Components.Demos.Combobox;

internal sealed record ComboboxDemoFruit(string Value, string Label);

internal sealed record ComboboxDemoLanguage(string Id, string Value);

internal sealed record ComboboxDemoCountry(string Code, string Value, string Label, string Continent);

internal sealed record ComboboxDemoProduce(string Id, string Label, string Group);

internal sealed record ComboboxDemoUser(string Id, string Name, string Username, string Email, string Title);

internal sealed record ComboboxDemoLabel(string Id, string Value, bool Creatable = false);

internal sealed record ComboboxDemoVirtualItem(string Id, string Name);

internal static class ComboboxDemoData
{
    public static readonly IReadOnlyList<ComboboxDemoFruit> Fruits =
    [
        new("apple", "Apple"),
        new("banana", "Banana"),
        new("orange", "Orange"),
        new("pineapple", "Pineapple"),
        new("grape", "Grape"),
        new("mango", "Mango"),
        new("strawberry", "Strawberry"),
        new("blueberry", "Blueberry"),
        new("raspberry", "Raspberry"),
        new("blackberry", "Blackberry"),
        new("cherry", "Cherry"),
        new("peach", "Peach"),
        new("pear", "Pear"),
        new("plum", "Plum"),
        new("kiwi", "Kiwi"),
        new("watermelon", "Watermelon"),
        new("cantaloupe", "Cantaloupe"),
        new("honeydew", "Honeydew"),
        new("papaya", "Papaya"),
        new("guava", "Guava"),
        new("lychee", "Lychee"),
        new("pomegranate", "Pomegranate"),
        new("apricot", "Apricot"),
        new("grapefruit", "Grapefruit"),
        new("passionfruit", "Passionfruit"),
    ];

    public static readonly IReadOnlyList<ComboboxDemoLanguage> Languages =
    [
        new("js", "JavaScript"),
        new("ts", "TypeScript"),
        new("py", "Python"),
        new("java", "Java"),
        new("cpp", "C++"),
        new("cs", "C#"),
        new("php", "PHP"),
        new("ruby", "Ruby"),
        new("go", "Go"),
        new("rust", "Rust"),
        new("swift", "Swift"),
    ];

    public static readonly IReadOnlyList<ComboboxDemoCountry> Countries =
    [
        new("af", "afghanistan", "Afghanistan", "Asia"),
        new("al", "albania", "Albania", "Europe"),
        new("dz", "algeria", "Algeria", "Africa"),
        new("ad", "andorra", "Andorra", "Europe"),
        new("ao", "angola", "Angola", "Africa"),
        new("ar", "argentina", "Argentina", "South America"),
        new("am", "armenia", "Armenia", "Asia"),
        new("au", "australia", "Australia", "Oceania"),
        new("at", "austria", "Austria", "Europe"),
        new("az", "azerbaijan", "Azerbaijan", "Asia"),
        new("bs", "bahamas", "Bahamas", "North America"),
        new("bd", "bangladesh", "Bangladesh", "Asia"),
        new("be", "belgium", "Belgium", "Europe"),
        new("br", "brazil", "Brazil", "South America"),
        new("ca", "canada", "Canada", "North America"),
        new("cl", "chile", "Chile", "South America"),
        new("cn", "china", "China", "Asia"),
        new("co", "colombia", "Colombia", "South America"),
        new("dk", "denmark", "Denmark", "Europe"),
        new("eg", "egypt", "Egypt", "Africa"),
        new("fr", "france", "France", "Europe"),
        new("de", "germany", "Germany", "Europe"),
        new("in", "india", "India", "Asia"),
        new("id", "indonesia", "Indonesia", "Asia"),
        new("ie", "ireland", "Ireland", "Europe"),
        new("it", "italy", "Italy", "Europe"),
        new("jp", "japan", "Japan", "Asia"),
        new("mx", "mexico", "Mexico", "North America"),
        new("nl", "netherlands", "Netherlands", "Europe"),
        new("nz", "new-zealand", "New Zealand", "Oceania"),
        new("ng", "nigeria", "Nigeria", "Africa"),
        new("no", "norway", "Norway", "Europe"),
        new("ph", "philippines", "Philippines", "Asia"),
        new("za", "south-africa", "South Africa", "Africa"),
        new("es", "spain", "Spain", "Europe"),
        new("se", "sweden", "Sweden", "Europe"),
        new("gb", "united-kingdom", "United Kingdom", "Europe"),
        new("us", "united-states", "United States", "North America"),
    ];

    public static readonly IReadOnlyList<ComboboxDemoProduce> Produce =
    [
        new("fruit-apple", "Apple", "Fruits"),
        new("fruit-banana", "Banana", "Fruits"),
        new("fruit-mango", "Mango", "Fruits"),
        new("fruit-kiwi", "Kiwi", "Fruits"),
        new("fruit-grape", "Grape", "Fruits"),
        new("fruit-orange", "Orange", "Fruits"),
        new("fruit-strawberry", "Strawberry", "Fruits"),
        new("fruit-watermelon", "Watermelon", "Fruits"),
        new("veg-broccoli", "Broccoli", "Vegetables"),
        new("veg-carrot", "Carrot", "Vegetables"),
        new("veg-cauliflower", "Cauliflower", "Vegetables"),
        new("veg-cucumber", "Cucumber", "Vegetables"),
        new("veg-kale", "Kale", "Vegetables"),
        new("veg-pepper", "Bell pepper", "Vegetables"),
        new("veg-spinach", "Spinach", "Vegetables"),
        new("veg-zucchini", "Zucchini", "Vegetables"),
    ];

    public static readonly IReadOnlyList<ComboboxOptionGroup<ComboboxDemoProduce>> GroupedProduce =
    [
        new(Produce.Where(item => item.Group == "Fruits").ToList(), "Fruits"),
        new(Produce.Where(item => item.Group == "Vegetables").ToList(), "Vegetables"),
    ];

    public static readonly IReadOnlyList<ComboboxDemoUser> Users =
    [
        new("leslie-alexander", "Leslie Alexander", "leslie", "leslie.alexander@example.com", "Product Manager"),
        new("michael-foster", "Michael Foster", "michael", "michael.foster@example.com", "Staff Engineer"),
        new("dries-vincent", "Dries Vincent", "dries", "dries.vincent@example.com", "Design Engineer"),
        new("lindsay-walton", "Lindsay Walton", "lindsay", "lindsay.walton@example.com", "Frontend Engineer"),
        new("courtney-henry", "Courtney Henry", "courtney", "courtney.henry@example.com", "Accessibility Lead"),
        new("tom-cook", "Tom Cook", "tom", "tom.cook@example.com", "Infrastructure Engineer"),
        new("whitney-francis", "Whitney Francis", "whitney", "whitney.francis@example.com", "QA Engineer"),
        new("leonard-krasner", "Leonard Krasner", "leonard", "leonard.krasner@example.com", "Developer Advocate"),
    ];

    public static readonly IReadOnlyList<ComboboxDemoLabel> Labels =
    [
        new("bug", "bug"),
        new("docs", "docs"),
        new("feature", "feature"),
        new("help-wanted", "help wanted"),
        new("internal", "internal"),
        new("performance", "performance"),
    ];

    public static readonly IReadOnlyList<ComboboxDemoVirtualItem> VirtualizedItems =
        Enumerable.Range(1, 10000)
            .Select(index => new ComboboxDemoVirtualItem(index.ToString(), $"Item {index:0000}"))
            .ToList();

    public static string GetFruitLabel(ComboboxDemoFruit? fruit) => fruit?.Label ?? string.Empty;

    public static string GetFruitValue(ComboboxDemoFruit? fruit) => fruit?.Value ?? string.Empty;

    public static string GetLanguageValue(ComboboxDemoLanguage? language) => language?.Value ?? string.Empty;

    public static string GetCountryLabel(ComboboxDemoCountry? country) => country?.Label ?? string.Empty;

    public static string GetCountryValue(ComboboxDemoCountry? country) => country?.Value ?? string.Empty;

    public static string GetProduceLabel(ComboboxDemoProduce? produce) => produce?.Label ?? string.Empty;

    public static string GetUserLabel(ComboboxDemoUser? user) => user?.Name ?? string.Empty;

    public static string GetUserValue(ComboboxDemoUser? user) => user?.Id ?? string.Empty;

    public static string GetLabelValue(ComboboxDemoLabel? label) => label?.Value ?? string.Empty;

    public static string GetVirtualItemValue(ComboboxDemoVirtualItem? item) => item?.Name ?? string.Empty;

    public static bool EqualFruit(ComboboxDemoFruit left, ComboboxDemoFruit right) => left.Value == right.Value;

    public static bool EqualLanguage(ComboboxDemoLanguage left, ComboboxDemoLanguage right) => left.Id == right.Id;

    public static bool EqualCountry(ComboboxDemoCountry left, ComboboxDemoCountry right) => left.Code == right.Code;

    public static bool EqualProduce(ComboboxDemoProduce left, ComboboxDemoProduce right) => left.Id == right.Id;

    public static bool EqualUser(ComboboxDemoUser left, ComboboxDemoUser right) => left.Id == right.Id;

    public static bool EqualLabel(ComboboxDemoLabel left, ComboboxDemoLabel right) => left.Id == right.Id;

    public static bool Contains(string text, string query) =>
        text.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    public static IReadOnlyList<ComboboxDemoUser> SearchUsers(string query)
    {
        var trimmed = query.Trim();
        if (trimmed.Length == 0)
        {
            return [];
        }

        return Users
            .Where(user =>
                Contains(user.Name, trimmed) ||
                Contains(user.Username, trimmed) ||
                Contains(user.Email, trimmed) ||
                Contains(user.Title, trimmed))
            .ToList();
    }
}
