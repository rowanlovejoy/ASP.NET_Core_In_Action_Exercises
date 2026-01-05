var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/hello-world", () => "Hello World!").WithName("HelloWorld");

// A service is an class or interface instance provided by the dependency injection (DI) container.
// The DI container will inject services into endpoint handlers where they include the parameter of the service type, e.g. an endpoint with a LinkGenerator parameter will receive the LinkGenerator service from the DI container.
app.MapGet("/path", (LinkGenerator linkGenerator) =>
{
    return linkGenerator.GetPathByName("HelloWorld");
});

// Within Program.cs*, services can additionally be retrieved directly from the container using the IServiceProvider instance on WebApplication (assigned to app in this case).
// This method of accessing DI services is the "service locator" pattern. Using it to access services within Program.cs is fine, but it's considered an anti-pattern to use within an endpoint handler; services should be retrieved via parameter injection in that case.
// Services accessed this way must be retrieved from WebApplication.Services (app.Services here) and used for whatever purposes before calling WebApplication.Run(); this call blocks until the application exits, so it's not possible to perform any action afer this call.
var linkGenerator = app.Services.GetRequiredService<LinkGenerator>();

var path = linkGenerator.GetPathByName("HelloWorld");

// This doesn't print the path. Presumably this is because the endpoint hasn't been set up yet and so it cannot find it using the name provided. LinkGenerator was the service used in the book's example for retrieving using the service locator pattern; it's odd that the example wouldn't work.
Console.WriteLine($"Link: {path}");

app.Run();
