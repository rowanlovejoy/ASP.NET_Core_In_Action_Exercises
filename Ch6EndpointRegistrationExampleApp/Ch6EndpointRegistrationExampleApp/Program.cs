var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddRazorPages();

// 
var app = builder.Build();

app.MapGet("/test", () => "Hello World!");

app.MapHealthChecks("/health");

app.MapRazorPages();

app.Run();
