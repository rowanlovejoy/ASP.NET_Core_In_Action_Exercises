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

// Create a group that adds the fruit prefix
var fruitGroup = app.MapGroup("/fruit");

// Created a nested group that adds the "id" router parameter; it will be available to all endpoints nested in this group, if their path prefixes don't specify it
// NB Changes to groups, paths, and route parameters don't seem to be applied by hot-reload; you have to restart the entire app to have them take effect
var fruitIdGroup = fruitGroup.MapGroup("/{id}")
    .AddEndpointFilterFactory(ValidationHelper.ValidateIdFactory);

fruitGroup.MapGet("/fruit", () => fruitCollection);

// The name of the parameter in the handler must match the name of the route parameter, otherwise an exception occurs when trying to match the request to a endpoint
fruitIdGroup.MapGet("/", (string id) =>
{
    return fruitCollection.TryGetValue(id, out var fruit)
        ? TypedResults.Ok(fruit)
        // Generates a standard ProblemDetails response with a 404 status code
        : Results.Problem(statusCode: 404);
});

// Thee fruit parameters will be retrieved by deserializing the JSON body of the request
// This happens because there's no route parameter that could fulfil them; they're complex types
// Only a single parameter can be bound to the request body
fruitIdGroup.MapPost("/", (Fruit fruit, string id) =>
{
    return fruitCollection.TryAdd(id, fruit)
        ? TypedResults.Created($"/fruit/{id}", fruit)
        // Generates a ProblemDetails response with validation errors in a standard format
        : Results.ValidationProblem(new Dictionary<string, string[]>()
        {
            { "id", ["A fruit with this ID already exists." ] },
        });
});

fruitIdGroup.MapPut("/", (Fruit fruit, string id) =>
{
    fruitCollection[id] = fruit;
    return Results.NoContent();
});

fruitIdGroup.MapDelete("/", (string id) =>
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

// It's also possible to implement a filter as a class implementing the IEndpointFilter endpoint
// This is standard filter, not filter factory, however; there's no class-based equivalent for the filer factory
internal static class ValidationHelper
{
    internal static EndpointFilterDelegate ValidateIdFactory(EndpointFilterFactoryContext factoryContext, EndpointFilterDelegate next)
    {
        // The factory function itself is executed once on startup for each endpoint its registered on; when handling requests, only the filter fuctions returned from the factory will be executed
        var idIndex = Array.FindIndex(
            factoryContext.MethodInfo.GetParameters(),
            parameter => parameter.Name == "id" && parameter.ParameterType == typeof(string));

        return int.IsPositive(idIndex) ? idFilter : next;

        async ValueTask<object?> idFilter(EndpointFilterInvocationContext invocationContext)
        {
            var id = invocationContext.GetArgument<string>(idIndex);

            if (string.IsNullOrWhiteSpace(id) || !id.StartsWith('f'))
            {
                var problem = new Dictionary<string, string[]>()
                {
                    { "id", ["Invalid ID format. ID must start with 'f'"] },
                };

                return Results.ValidationProblem(problem);
            }

            return await next(invocationContext);
        }
    }
}
