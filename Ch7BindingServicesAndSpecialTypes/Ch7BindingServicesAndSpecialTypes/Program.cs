var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Objects of "well-known" types can be "injected" into handlers. As with parameters bound to properties of the request -- route parameters, query parameters, etc. -- if a parameter of a well-known types appears in an endpoint handler's signature, the framework will provide the necessary argument, i.e., it will "inject" it into the handler.
// These well-known types include HttpContext -- a container for all details about the request and response being processed -- as well as properties thereof.
// This endpoint will receive the current HttpContext
app.MapGet("/", (HttpContext httpContext) => "Hello World!");

// These endpoints will receive the Request and Response properties, respectively, of the current HttpContext
app.MapGet("/request", (HttpRequest httpRequest) => "Hello World!");
app.MapGet("/response", (HttpResponse httpResponse) => "Hello World!");

// This endpoint will receive the Body property of the current HttpRequest
app.MapGet("/stream", (Stream stream) => "Hello World!");
// The body of this handler access the equivalent property of HttpContext to that injected by the framework in the last handler
app.MapGet("/stream2", (HttpRequest httpRequest) =>
{
    return (httpRequest.Body is Stream bodyStream);
});

app.Run();
