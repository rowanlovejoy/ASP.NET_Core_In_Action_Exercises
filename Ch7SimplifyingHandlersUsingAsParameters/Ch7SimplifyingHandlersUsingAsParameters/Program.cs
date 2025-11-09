using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// The [AsParameters] attribute enables multiple endpoint handler parameters to be encapsulated into a single custom type, e.g., a class or struct.
// Normal model binding rules apply: the custom type of the parameter can be annotated with attributes designating what part of the HttpContext to bind to, and TryParse and BindAsync are invoked as normal.
app.MapGet("/category/{id}", ([AsParameters] SearchModel model) => $"Received {model}");
// Including the definition of SearchModel, the above endpoint handler is equivalent to the below, which has all parameters declared inline.
//app.MapGet("/category/{id}", ([FromRoute(Name = "id")] int Id,
//                            [FromQuery(Name = "page")] int Page,
//                            [FromHeader(Name = "sort")] bool? SortAscending,
//                            [FromQuery(Name = "q")] string Search) => $"Received ...");

app.MapPost("/item/{id}", ([AsParameters] ItemSearchModel model) => $"Received {model}");

app.MapGet("/category/2/{id}", ([AsParameters] SearchModel model) => $"Received {model}");


app.Run();

// Under default naming convention rules, record constructor parameter names must be in PascalCase.
// This complicates the default model binding behaviour for simple types, where the framework looks for an exact name match in the rout, query string, headers, etc.
// The record struct below violates the naming convention, but by default would be bind parameters named "id", "page", and "search"
#pragma warning disable IDE1006 // Naming Styles
internal record struct ItemSearchModel(int id, int page, string search);
#pragma warning restore IDE1006 // Naming Styles

// To follow the naming convention, this type's constructor parameters use PascalCase, and are explicitly mapped to route parameters, query string values, and header values named in lower case
internal record SearchModel([FromRoute(Name = "id")] int Id,
                            [FromQuery(Name = "page")] int Page,
                            [FromHeader(Name = "sort")] bool? SortAscending,
                            [FromQuery(Name = "q")] string Search);

// This type uses a class with an explicitly declared constructor. Constructor parameters in this style are typically named using camelCase, so do not require explicit mapping to bind to typically named route parameters, query string values, header values, etc.
internal class SearchModel2
{
    public SearchModel2(int id,
                        int page,
                        [FromHeader(Name = "sort")] bool? sortAscending,
                        string search)
    {
        Id = id;
        Page = page;
        SortAscending = sortAscending;
        Search = search;
    }

    public int Id { get; }
    public int Page { get; }
    public bool? SortAscending { get; }
    public string Search { get; }
}