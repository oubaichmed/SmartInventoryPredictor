using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SmartInventoryPredictor.Client;
using SmartInventoryPredictor.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

 builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:7025/")  
});

// Add services
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SignalRService>();

builder.Services.AddLogging();

var host = builder.Build();

try
{
    var authService = host.Services.GetRequiredService<AuthService>();
    await authService.InitializeAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Auth initialization failed: {ex.Message}");
}

await host.RunAsync();