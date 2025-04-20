var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Lambda handler
app.MapGet("/fruit", () => Fruit.All);

// The name of the parameter in the handler must match the name of the route param, otherwise an exception occurs when tring to match the request to a endpoint

// Delegate handler
var getFruit = (string id) =>
{
    return Fruit.All[id];
};
app.MapGet("/fruit/{id}", getFruit);

// Static method handler
app.MapPost("/fruit/{id}", Handlers.AddFruit);

// Instance method handler
var handlers = new Handlers();
app.MapPut("/fruit/{id}", handlers.ReplaceFruit);

// Local function handler
app.MapDelete("/fruit/{id}", DeleteFruit);

app.Run();

static void DeleteFruit(string id)
{
    Fruit.All.Remove(id);
}

internal record Fruit(string Name, int Stock)
{
    public static readonly Dictionary<string, Fruit> All = [];
}

// Thee fruit parameters will be retrieved by deserializing the JSON body of the request
// This happens because there's no route parameter that could fulfil them; they're complex types
// Only a single parameter can be bound to the request body
internal class Handlers
{
    public void ReplaceFruit(string id, Fruit fruit)
    {
        Fruit.All[id] = fruit;
    }

    public static void AddFruit(string id, Fruit fruit)
    {
        Fruit.All.Add(id, fruit);
    }
}