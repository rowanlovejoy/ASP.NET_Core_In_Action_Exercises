using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Minimal APIs use the System.Text.Json library for serialising and de-serialising JSON. This can't be replaced, but can be customised, for example to reduce its strictness.
// Book says to use ConfigureRouteHandlerJsonOptions, but it seems to method has been renamed -- see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-8.0#configure-json-serialization-options-globally
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.AllowTrailingCommas = true;
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

// Because Product is a complex type, it will be bound to the JSON body of the request
// This behaviour is automatic; for simple types, use [FromBody] attribute to force binding to the body
// Can also use [FromBody] to force binding body binding for request methods where a body usually isn't included -- GET, DELETE, HEAD, etc. -- though this is discouraged because it's unusual and counter to the HTTP spec
app.MapPost("/product", (Product product) => $"Received {product}");

app.Run();

// A complex type is the opposite of a simple type: it doesn't implement an appropriate TryParse method
internal record Product(int Id, string Name, int Stock);