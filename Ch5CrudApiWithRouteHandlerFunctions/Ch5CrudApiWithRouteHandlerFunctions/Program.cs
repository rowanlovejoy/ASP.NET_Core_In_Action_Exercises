using System.Collections.Concurrent;
using System.Net.Mime;

// CHANGES THE ENVIRONMENT
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

var builder = WebApplication.CreateBuilder(args);
// Add a service that implements IProblemDetails to support creating ProblemDetails responses when an exception is thrown
builder.Services.AddProblemDetails();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // When used without a path and when an IProblemDetails service has been injected, exceptions caught by this middleware will be converted into ProblemDetails responses
    app.UseExceptionHandler();
}

// Converts any error responses without a body to ProblemDetails responses
// Exceptions will still be caught by exception handler middleware added by UseExceptionHandler (and converted into ProblemDetails responses since IProblemDetails has been injected)
app.UseStatusCodePages();

var fruitCollection = new ConcurrentDictionary<string, Fruit>();

app.MapGet("/fruit", () => fruitCollection);

// The name of the parameter in the handler must match the name of the route parameter, otherwise an exception occurs when trying to match the request to a endpoint
app.MapGet("/fruit/{id}", (string id) =>
{
    return fruitCollection.TryGetValue(id, out var fruit)
        ? TypedResults.Ok(fruit)
        // Generates a standard ProblemDetails response with a 404 status code
        : Results.Problem(statusCode: 404);
});

app.MapPost("/fruit/{id}", (string id, Fruit fruit) =>
{
    return fruitCollection.TryAdd(id, fruit)
        ? TypedResults.Created($"/fruit/{id}", fruit)
        // Generates a ProblemDetails response with validation errors in a standard format
        : Results.ValidationProblem(new Dictionary<string, string[]>()
        {
            { "id", ["A fruit with this ID already exists." ] },
        });
});

app.MapPut("/fruit/{id}", (string id, Fruit fruit) =>
{
    fruitCollection[id] = fruit;
    return Results.NoContent();
});

app.MapDelete("/fruit/{id}", (string id) =>
{
    return fruitCollection.TryRemove(id, out var fruit)
        ? Results.NoContent()
        : Results.BadRequest(new { id = $"No fruit exists with the {id}" });
});

// Endpoint with a manually defined response, i.e., created without TypedResults or Results
// The framework knows to inject HttpResponse rather than attempting to deserialize it from the route or the request body
app.MapGet("/teapot", (HttpResponse response) =>
{
    // Doesn't have a constant for 418 :(
    response.StatusCode = 418;
    response.ContentType = MediaTypeNames.Text.Plain;
    return response.WriteAsync("I'm a teapot.");
});

app.MapGet("/error", () =>
{
    throw new Exception("Test exception");
});

app.MapGet("/missing", () =>
{
    return Results.NotFound();
});

app.Run();

internal record Fruit(string Name, int Stock)
{
    public static readonly Dictionary<string, Fruit> All = [];
}

// Thee fruit parameters will be retrieved by deserializing the JSON body of the request
// This happens because there's no route parameter that could fulfil them; they're complex types
// Only a single parameter can be bound to the request body
//internal class Handlers
//{
//    public void ReplaceFruit(string id, Fruit fruit)
//    {
//        Fruit.All[id] = fruit;
//    }

//    public static void AddFruit(string id, Fruit fruit)
//    {
//        Fruit.All.Add(id, fruit);
//    }
//}