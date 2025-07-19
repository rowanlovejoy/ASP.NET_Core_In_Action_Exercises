using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// By default, ASP.NET attempts to bind first from route parameters and second from query parameters
app.MapGet("/products/{id:int}/paged", (
    // Can use [From*] attributes to override default binding sources, explicitly defining where an endpoint's parameter(s) should be sourced from
    // These three attributes are the only parameter binding attributes that operate on "simple values", e.g., int, double, others (see below)
    // The book gives int and double as examples of simple types (see below)
    [FromRoute] int id,
    [FromQuery] int page,
    // Route and query parameters can be bound automatically based on convention -- try first from route and second from query -- but the [FromHeader] attribute is required to bind to a request header
    [FromHeader(Name = "PageSize")] int pageSize,
    [FromHeader(Name = "MyString")] string myString, [FromHeader(Name = "MyBool")] bool myBool) =>
{
    return new Dictionary<string, object>
    {
        { "id", id },
        { "page", page },
        { "PageSize", pageSize },
        { "MyString", myString },
        { "MyBool", myBool }
    };
});

app.MapGet("/products/{id}", (ProductId id) =>
{
    return $"The product ID is {id}";
});

app.Run();

// A simple type for the purposes of ASP.NET core model binding (are Razer Pages and MVC different?) is a type that implements the method "public static bool TryParse(string value, out T result)" or an overload of this method that additionally takes an IFormatProvider as its second argument
// The example below explicitly implements the IParsable interface that includes a suitable TryParse method (and also Parse), but the interface isn't required; the type has only to have method with the right signature. ASP.NET Core calls this method during model binding to create the argument for the endpoint handler.
// So, any type could, regardless of complexity, be a simple type as long as it implements TryFormat in a compatible way
// The example below demonstrates this with a strongly typed ID type: strings matching "p<number>", e.g., "p123", will be converted into ProductId instances with the number segment of the input string as the value of the Id property, e.g., "p123" will become ProductId { Id = 123 }
// TryParse accepts a single string parameter from which to create the type; the upper-limit of complexity for a "simple type" is therefore the limit of what can be serialised as a string and de-serialised in a TryParse method
// A great deal can be serialised as a string -- see data URLs, for example -- so this limit is more practical than technical
internal readonly record struct ProductId(int Id) : IParsable<ProductId>
{
    private static ProductId? ParseProductId(string? id)
    {
        if (id is ['p', .. var numberSegment] && int.TryParse(numberSegment, out var idNumber))
        {
            return new ProductId(idNumber);
        }

        return null;
    }

    public static ProductId Parse(string s, IFormatProvider? provider)
    {
        if (ParseProductId(s) is ProductId productId)
        {
            return productId;
        }

        throw new FormatException(s);
    }

    // If TryParse returns true during model binding, the resulting value will be passed to the matching parameter of the endpoint handler; if it returns false, an exception is thrown resulting in 400 Bad Request response
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out ProductId result)
    {
        if (ParseProductId(s) is ProductId productId)
        {
            result = productId;
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString()
    {
        return Id.ToString();
    }
}

