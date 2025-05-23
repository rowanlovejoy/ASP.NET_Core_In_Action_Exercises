using System.Collections.Concurrent;
using System.Net.Mime;

// CHANGES THE ENVIRONMENT
//Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

var builder = WebApplication.CreateBuilder(args);
// Add a service that implements IProblemDetails to support creating ProblemDetails responses when an exception is thrown
builder.Services.AddProblemDetails();
builder.Services.AddHttpLogging((options) =>
{
});

var app = builder.Build();

//Development middleware
if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
}

// Non-development middleware
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
}).AddEndpointFilter(ValidationHelper.ValidateId).AddEndpointFilter(async (context, next) =>
{
    // Filter is "re-executed" for the outgoing response only if the filter has code after the next(context) call; there's no mechanism that automatically reinvokes the filter with the response
    app.Logger.LogInformation("Executing logging filter...");
    var result = await next(context);
    app.Logger.LogInformation("Result from handler: {result}", result);
    return result;
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
}).AddEndpointFilter(ValidationHelper.ValidateId);

app.MapPut("/fruit/{id}", (string id, Fruit fruit) =>
{
    fruitCollection[id] = fruit;
    return Results.NoContent();
}).AddEndpointFilter(ValidationHelper.ValidateId);

app.MapDelete("/fruit/{id}", (string id) =>
{
    return fruitCollection.TryRemove(id, out var fruit)
        ? Results.NoContent()
        : Results.BadRequest(new { id = $"No fruit exists with the {id}" });
}).AddEndpointFilter(ValidationHelper.ValidateId);

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

internal static class ValidationHelper
{
    internal static async ValueTask<object?> ValidateId(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var id = context.GetArgument<string>(0);

        if (string.IsNullOrWhiteSpace(id) || !id.StartsWith('f'))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>()
            {
                { "id", ["Invalid ID format. ID must start with 'f'"] },
            });
        }

        return await next(context);
    }
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