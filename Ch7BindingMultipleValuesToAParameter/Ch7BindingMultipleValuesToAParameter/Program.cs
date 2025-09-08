using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Using arrays, it's possible to bind a single parameter to multiple values in the request -- in this example, multiple query string values with the same key
// The name of the value to bind to in the request would normally have to match the name of the parameter, but can be overridden using the From* attribute to enable more accurate naming
app.MapGet("/product/search", ([FromQuery(Name = "id")] int[] ids) => $"Received {ids.Length} IDs from query: {string.Join(", ", ids)}");

// Similar to query string, multiple header values with same name can be bound to a single array parameter
// Overriding of the header key is possible in the same manner as with the query string binding
app.MapGet("/product/search/header", ([FromHeader(Name = "id")] int[] ids) => $"Received {ids.Length} IDs from headers: {string.Join(", ", ids)}");

// Binding request values to arrays of custom  simple types is allowed -- i.e., types that implement TryParse
app.MapGet("/product/search/id", ([FromQuery(Name = "id")] ProductId[] ids) => $"Received {ids.Length} product IDs from query: {string.Join(", ", ids)}");

// For strings, StringValues can be used as the parameter type instead of string[] or a custom simple type
// If a StringValues parameter is paired with a From* attribute, the compiler errors because StringValues, apparently, does not implement TryParse
// This behaviour seems to make it impossible to override the name of the binding source when using StringValues
// The book list StringValues as an example of a simple type, so this error is perplexing
app.MapGet("/product/string", (StringValues id) => $"Received {id.Count} StringValue IDs from query: {string.Join(", ", [.. id])}");

// It's not possible to have multiple route parameters with the same name, so it's not possible to bind multiple route values to the same parameter
//app.MapGet("/product/search/{id}/{id}", ([FromRoute(Name = "id")] int[] ids) => $"Received {ids.Length} IDs: {string.Join(", ", ids)}");

// For verbs where the framework assumes a body will be present -- e.g., POST -- the default binding source for array parameters will be the body instead of the query string or headers
// Here, a body that is JSON array of numbers is expected
app.MapPost("/product/search", (int[] id) => $"Received {id.Length} IDs from body: {string.Join(", ", id)}");

// For verbs where the framework assumes a body will typically not be present -- e.g., GET -- and parameter is of a complex type -- i.e., it doesn't implement TryParse -- an exception will occur unless the FromBody attribute is used to force binding to the request body
app.MapGet("/product/search/get", ([FromBody] Product[] products) => $"Received {products.Length} IDs from body: {string.Join<Product>(", ", products)}");

app.Run();

internal record struct ProductId(string Id) : IParsable<ProductId>
{
    private static string? Parse(string? Id)
    {
        return Id switch
        {
            ['p', .. var rest] => rest,
            _ => null
        };
    }

    public static ProductId Parse(string s, IFormatProvider? provider)
    {
        if (Parse(s) is string id)
        {
            return new ProductId(id);
        }

        throw new FormatException("ID format is invalid");
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out ProductId result)
    {
        if (Parse(s) is string id)
        {
            result = new ProductId(id);
            return true;
        }

        result = default;
        return false;
    }
}

internal record Product(string Id, string Name);