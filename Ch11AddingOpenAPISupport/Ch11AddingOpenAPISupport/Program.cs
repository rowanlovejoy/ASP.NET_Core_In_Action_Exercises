using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setup =>
{
    setup.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Fruitify",
        Description = "An API for managing fruit stock.",
        Version = "1.0"
    });
    var documentationFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    setup.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, documentationFile));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var fruitCollection = new ConcurrentDictionary<string, Fruit>();

#pragma warning disable ASPDEPR002 // Type or member is obsolete
app.MapGet("/fruit/{id}", (string id) =>
{
    return fruitCollection.TryGetValue(id, out var fruit)
        ? TypedResults.Ok(fruit)
        : Results.Problem(statusCode: (int)HttpStatusCode.NotFound);
})
    .WithName("GetFruit")
    .WithTags("fruit")
    .Produces<Fruit>()
    .Produces((int)HttpStatusCode.NotFound)
    .WithSummary("Fetch the fruit with the specified ID")
    .WithDescription("Returns the fruit matching the specified ID, or a 404 response if no matching fruit can be found.")
    .WithOpenApi();
#pragma warning restore ASPDEPR002 // Type or member is obsolete

#pragma warning disable ASPDEPR002 // Type or member is obsolete
app.MapPost("/fruit/{id}",
    [EndpointName("CreateFruit")]
[EndpointSummary("Creates a fruit.")]
[EndpointDescription("Creates a fruit with the specified ID, name, and stock count. Returns a problem details response if a fruit already exists with the specified ID.")]
[ProducesResponseType(typeof(Fruit), (int)HttpStatusCode.Created)]
[ProducesResponseType(typeof(HttpValidationProblemDetails), (int)HttpStatusCode.Conflict, "application/problem+json")]
(string id, Fruit fruit) =>
{
    return fruitCollection.TryAdd(id, fruit)
        ? TypedResults.Created($"/fruit/{id}", fruit)
        : Results.ValidationProblem(new Dictionary<string, string[]>
        {
            { "id", new[] { $"A fruit with the ID {id} already exists" } }
        });
})
    .WithOpenApi(operation =>
    {
        operation.Parameters?[0].Description = "The ID to assign to the created fruit.";
        return operation;
    });
#pragma warning restore ASPDEPR002 // Type or member is obsolete


var fruitHandler = new FruitHandler(fruitCollection);
app.MapPut("fruit/{id}", fruitHandler.UpdateFruit)
    .WithName("UpdateFruit");

app.Run();

internal record Fruit(string Name, int Stock);

internal class FruitHandler(ConcurrentDictionary<string, Fruit> fruitCollection)
{
    /// <summary>
    /// Updates the fruit with specified ID. Returns a 404 response if not fruit the specified ID exists.
    /// </summary>
    /// <param name="id">The ID of the fruit to update</param>
    /// <param name="updatedFruit">The updated fruit</param>
    /// <response code="200">Returns the updated fruit if the update was successful</response>
    /// <response code="404">If the no fruit exists with the specified ID</response>
    [Tags("Fruit")]
    [ProducesResponseType(typeof(Fruit), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), (int)HttpStatusCode.NotFound, "application/problem+json")]
    public IResult UpdateFruit(string id, Fruit updatedFruit)
    {
        if (fruitCollection.ContainsKey(id))
        {
            fruitCollection[id] = updatedFruit;
            return TypedResults.Ok(updatedFruit);
        }

        return Results.Problem(statusCode: (int)HttpStatusCode.NotFound);
    }
}