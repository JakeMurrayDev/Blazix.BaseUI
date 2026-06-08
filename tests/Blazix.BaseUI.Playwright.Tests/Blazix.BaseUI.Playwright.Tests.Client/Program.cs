using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton(TimeProvider.System);

await builder.Build().RunAsync();
