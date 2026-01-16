var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<DataContext>();

var app = builder.Build();

// In addition to being injected in endpoint handlers and the constructors of other services, services can also be resolved in Program.cs by requiring them the IServiceProvider instance exposed by WebApplication.Services (app.Services here).
// WebApplication.Services is exposes the DI container's API directly, and when access through this instance, it is the "root" DI container. (Are there multiple instances of the DI container within the same? Or is this the same instance as everywhere else, but with different semantics?)
// Services retrieved through this IServiceProvider instance will be retained for the lifetime of the app, regardless of the lifetime the service was registered with: transient, scoped, and singleton services will all last until the app exits.
// For this reason, only singleton services can be safely (or maybe at all, see * below) accessed using this IServiceProvider instance, using this "root" DI container. (Again, is this another container or a different API for the same container?) Singleton services always live for the lifetime of the app, and should be written with this in mind; transient and scoped services would normally be much shorter lived, and may be written with this expectation and thus leak memory if they were to live longer.
// (* It seem as that only singleton and transient services can accessed from the root container; attempting to start this app with DataContext registered the scoped lifetime causes an exception to be thrown. Changing the lifetime to singleton or transient causes the exception to no longer be thrown.)
var rootDb = app.Services.GetRequiredService<DataContext>();

// Services registered with the scoped lifetime are created anew for each "scope"; a scope is automatically created for each HTTP request the ASP.NET framework handles, and a new instance of a scoped service will be created for each and re-used within that scope when anything within the scope requests.
// To safely access instances of services registered with transient or scoped lifetimes outside the scope created for an HTTP request, a new scope must be manually created and the services retrieved within it.
// The IServiceProvider instance on WebApplication.Services -- the "root" DI container -- expose the Create[Async]Scope methods, which create [Async]ServiceScope objects that define scopes. (Are these objects what the framework creates automatically for each HTTP request?)
// These scopes are disposable, and so must be created and used within using statements. Because WebApplication.Run blocks until the app exits, a using statement rather a using declaration must be used. A using statement defines a scope within the enclosing scope after which the created object is disposed; a using declaration disposes the created object at the end of the enclosing scope, which in this case is the Main method. Because WebApplication.Run blocks until the app shuts down, the enclosing scope of Main won't end and therefore disposal won't occur until the app exits, negating any benefit from using a scope in the first place.
// There are two types of scope: synchronous and asynchronous. Synchronous service scopes can safely dispose of services implementing IDisposable; asynchronous service scopes can additionally handle services implementing IAsyncDisposable. The book recommends using asynchronous scopes, created using CreateAsyncScope, whenever possible.
await using (var scope = app.Services.CreateAsyncScope())
{
    // The IServiceProvider instance exposed by the scope is the "scoped" DI container; services retrieved from it are retained in memory until the end of the scope -- in this case end of the using statement's block -- at which point they are disposed of, with Dispose being called on them if they implement IDisposable. (Because we're using an async scope, services implementing IAsyncDisposable will also be handled correctly.)
    // The wording of the book suggests, and repeatedly, that IServiceProvider instance on WebApplication (exposed WebApplication.Services) and the IServiceProvider instance on IServiceScope and AsyncServiceScope -- the synchronous and asynchronous scope types, respectively; AsyncServiceScope implements IServiceScope -- are different DI containers, not simply different APIs with different semantics (i.e., disposable at app end vs disposal at scope end).
    var scopedDb = scope.ServiceProvider.GetRequiredService<DataContext>();

    // How to transient services behave in this a scope like this? If I manually create a scope like this, and somehow require a transient service in multiple locations within, will each injection site received a different instance, as is expected of services with the transient lifetime? Or will transient services behave like scoped services?

    Console.WriteLine($"Manual scope row count: {scopedDb.RowCount}");
}

app.MapGet("/scoped", (DataContext db) =>
{
    return $"Endpoint handler scope row count: {db.RowCount}";
});

app.Run();

internal class DataContext
{
    public int RowCount { get; set; } = Random.Shared.Next(0, int.MaxValue);
}
