using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// AvatarFallback has an [Inject] TimeProvider property, so Blazor property injection throws at
// component instantiation unless TimeProvider is registered - regardless of whether Delay is set.
// The WebAssembly leg resolves from this container, not the server's.
builder.Services.AddSingleton(TimeProvider.System);

await builder.Build().RunAsync();
