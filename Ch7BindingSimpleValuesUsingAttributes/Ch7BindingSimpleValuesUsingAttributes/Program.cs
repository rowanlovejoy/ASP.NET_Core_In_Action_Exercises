using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// By default, ASP.NET attempts to bind first from route parameters and second from query parameters
app.MapGet("/products/{id}/paged", (
    // Can use From* attributes to override default binding sources, explicitly stating where an endpoint's parameter(s) should be sourced from
    [FromRoute] int id,
    [FromQuery] int page,
    [FromHeader(Name = "PageSize")] int pageSize) =>
{
    return $"Received ID {id}, page {page}, and pageSize {pageSize}";
});

app.Run();
