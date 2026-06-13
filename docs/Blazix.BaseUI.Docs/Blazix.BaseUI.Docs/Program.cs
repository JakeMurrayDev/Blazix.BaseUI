using Blazix.BaseUI.Docs.Client.Pages;
using Blazix.BaseUI.Docs.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapGet("/components/{slug}.md", async (string slug, IWebHostEnvironment environment, CancellationToken cancellationToken) =>
{
    if (slug.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '-')))
    {
        return Results.BadRequest("Invalid component slug.");
    }

    var markdownPath = Path.Combine(environment.ContentRootPath, "Content", "Components", $"{slug}.md");
    if (!File.Exists(markdownPath))
    {
        return Results.NotFound();
    }

    var markdown = await File.ReadAllTextAsync(markdownPath, cancellationToken);
    return Results.Text(markdown, "text/markdown; charset=utf-8");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blazix.BaseUI.Docs.Client._Imports).Assembly);

app.Run();
