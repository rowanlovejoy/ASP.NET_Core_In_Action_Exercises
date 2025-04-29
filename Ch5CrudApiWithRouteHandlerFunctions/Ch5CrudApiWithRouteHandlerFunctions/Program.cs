using System.Collections.Concurrent;
using System.Net.Mime;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var fruitCollection = new ConcurrentDictionary<string, Fruit>();

// Lambda handler
app.MapGet("/fruit", () => fruitCollection);

//var getFruit = (string id) =>
//{
//    return Fruit.All[id];
//};

// The name of the parameter in the handler must match the name of the route param, otherwise an exception occurs when tring to match the request to a endpoint
app.MapGet("/fruit/{id}", (string id) =>
{
    return fruitCollection.TryGetValue(id, out var fruit)
        ? TypedResults.Ok(fruit)
        // Can't use TypedResults.NotFound here or it complains the about the handler signature -- why?
        : Results.NotFound();
});

app.MapPost("/fruit/{id}", (string id, Fruit fruit) =>
{
    return fruitCollection.TryAdd(id, fruit)
        ? TypedResults.Created($"/fruit/{id}", fruit)
        // Can't use TypedResults.NotFound here, either. Maybe TypedResults works only if you're returning a value (but we're returning an error object)
        : Results.BadRequest(new { id = "A fruit with this ID already exists." });
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
app.MapGet("/teapot", (HttpResponse response) =>
{
    // Doesn't have a constant for 418 :(
    response.StatusCode = 418;
    response.ContentType = MediaTypeNames.Text.Plain;
    return response.WriteAsync("I'm a teapot.");
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