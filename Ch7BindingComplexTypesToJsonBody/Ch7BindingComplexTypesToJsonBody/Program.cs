using Microsoft.AspNetCore.Mvc;
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
// This behaviour is automatic for complex types; for simple types, use [FromBody] attribute to force binding to the body
// Only one parameter can be bound to the body; if there are multiple complex type parameters, an exception will occur at runtime
// Can also use [FromBody] to force binding body binding for request methods where a body usually isn't included -- GET, DELETE, HEAD, etc. -- though this is discouraged because it's unusual and counter to the HTTP spec
// This behaviour is JSON-specific; the endpoint won't run for requests with non-JSON bodies and a 415 (unsupported media type) response will be returned
app.MapPost("/product", (Product product) => $"Received body product {product}");

// Uses [FromBody] to force binding a parameter -- e.g., a simple type -- to the request body  
app.MapPost("/product/name", ([FromBody] string name) =>
$"Received body name {name}");

app.Run();

// A complex type is the opposite of a simple type: it doesn't implement an appropriate TryParse method
internal record Product(int Id, string Name, int Stock);