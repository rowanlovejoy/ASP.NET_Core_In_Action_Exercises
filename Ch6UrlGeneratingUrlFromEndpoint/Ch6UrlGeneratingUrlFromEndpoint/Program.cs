var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/product/{name}", (string name) =>
{
    return $"The product is {name}";
    // Give this endpoint a name, enabling it to be referenced for URL generation
    // Names are case sensitive and must be globally unique
}).WithName("product");

// Inject LinkGenerator into the endpoint via dependency injection. It's automatically registered with the dependency injection container
app.MapGet("links", (LinkGenerator linkGenerator) =>
{
    // Generate a URL to the named endpoint, substituting the route values for those provided
    // This method generates a relative URL, i.e., only the path; use GetUriByName to generate a full URL including scheme and host
    var relativeUrl = linkGenerator.GetPathByName("product",
        // name will be assigned to {name} in the route template
        // If the route template doesn't include a route value name, the name argument here wil be appended as query parameter, e.g., ?name=relative-widget
        new { name = "relative-widget" });

    var absoluteUrl = linkGenerator.GetUriByName("product", new { name = "absolute-width" },
        // Constant for https; constants for other schemes are available
        Uri.UriSchemeHttps,
        // If re-using the request's host value, e.g., by taking it from an injected HttpContext, you must use host filtering on the web server (e.g., Kestrel) to defend against certain attacks
        new HostString("localhost"));

    return new string[] { $"The relative URL is {relativeUrl}", $"The absolute URL is {absoluteUrl}" };
}).WithName("links");

// Use a catch all parameter to bind to the remainder of the URL after matching "redirect/", including any forward slashes that would otherwise delimit route parts
// {**<part_name>} preserves -- "round trips" -- forward slashes without URL encoding; {*<part_name>} URL encodes forward slashes
app.MapGet("redirect/{**route}", (string route) =>
{
    var routeParts = route.Split('/', StringSplitOptions.RemoveEmptyEntries);

    // Use a collection pattern to a match an array with exactly two elements where the first is product
    if (routeParts is ["product", var name])
    {
        // Redirects to the named "product", passing in the retrieved product name
        return Results.RedirectToRoute(routeParts[0], new { name });
    }

    return Results.RedirectToRoute("links");
});

app.Run();
