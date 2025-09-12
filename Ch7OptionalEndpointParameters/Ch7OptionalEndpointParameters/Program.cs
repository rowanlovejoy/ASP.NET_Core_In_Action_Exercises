var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


// By default, all endpoint handler parameters are required: if a source can't be bound from the request, an error will be thrown at runtime
// Route parameters can be declared optional by appending "?"; handler parameters can similarly be declared optional by marking them as nullable
// ASP.NET is designed around all code within a nullable-aware context; this is enabled by default in official project templates
// Disabling the nullable-aware context may result in some parameters being unexpectedly marked as optional
// Even with the nullable-aware context disabled, this endpoint requires the id to defined as int? to retain the intended behaviour of id being optional
app.MapGet("/stock/{id?}", (int? id) => $"Received {id.ToString() ?? "null"}");

// The query string is always optional and not a part of route matching. Nevertheless, if no route parameter is provided and the endpoint handler is expecting a value -- assuming a typically body-less HTTP method -- it will try to bind the query string; if binding fails, an error will be thrown at runtime
app.MapGet("/stock/2", (int? id) => $"Received {id.ToString() ?? "null"}");

// Parameters that are complex type and would default to body binding based on the HTTP method, as with this handler's parameter, can also be marked nullable to make them optional.
// If the body contains JSON null literal, the endpoint handler will receive null as its argument. (Is this because the EndpointMiddleware sees the JSON null value as "no value provided"? Or is de-serialising the JSON null to the C# null and, because the parameter is nullable, passing that value to the handler because its compatible?)
app.MapPost("/stock", (Product? product) => $"Received {product?.ToString() ?? "null"}");

// Handler parameters can also be made optional by giving them a default value. If a value cannot can be bound using the request, the handler will use the default value instead
app.MapGet("/stock/default", (int id = 0) => $"Received {id.ToString() ?? "null"}");

app.Run();

internal record Product(int Id, string Name);
