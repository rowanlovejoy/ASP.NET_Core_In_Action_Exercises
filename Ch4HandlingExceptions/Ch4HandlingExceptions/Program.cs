using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler();
//}

app.UseExceptionHandler("/error");

app.MapGet("/", () => "Hello, World!");
app.MapGet("/broken", () =>
{
    throw new Exception("test");
});

app.MapGet("/error", (HttpContext httpContext) =>
{
    throw new Exception("Another error");

    var feature = httpContext.Features.Get<IExceptionHandlerPathFeature>();

    var message = $"You got an error: {feature?.Error.Message}";

    return TypedResults.Text(message);
});

app.Run();
