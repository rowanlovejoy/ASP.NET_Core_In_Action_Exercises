var builder = WebApplication.CreateBuilder(args);

// When registering a service, a lifetime must be specified.
// The lifetime of the service determines how long the DI container retains the instance of that service it creates before creating another instance.
// Services can be registered with one of three lifetimes: transient, scoped, and singleton.
// From shortest lived to longest, the progression is: transient, scoped, singleton; that is, transient instances are retained for the shortest amount of time, while singleton instances are kept for the longest.

// The lifetime of a service determines what services it can depend on and what services can depend on it. 
// If, for example, a singleton service depends on a transient service, the singleton service will receive a new instance of the transient service when the DI container first creates, but will then capture that instance and retain it for its lifetime -- which, because its a singleton service, is the lifetime of the app. Captured dependencies like the transient service in this example break the behavioural promises of a service's lifetime type and can cause bugs.
// Uncommenting the two services registrations below will prevent start-up because ASP.NET detects that a singleton service is attempting to consume a scoped service, which would created a captive dependency and violate lifetime behaviour rules. Changing the lifetime of DataContext to singleton, or the lifetime of Repository to scoped will prevent the latter from capturing the former; there will no longer be a violation and the app will start.
//builder.Services.AddScoped<DataContext>();
//builder.Services.AddSingleton<Repository>();
// To ensure services retain the behaviour expected of their lifetime, a service must depend only on other services that have lifetimes equal to or longer than its own. In practice, this means that a singleton service should depend only on other singleton services; a scoped service can depend only on other scoped services or singleton services; and only transient can depend all services regardless of lifetime.
// ASP.NET automatically checks for the creation of captured dependencies will prevent app start-up or throw an exception at runtime if it detects them. This check has a performance cost, and so by default is enabled only while the app is running the in "Development" environment.
// Use the UseDefaultServiceProvider method to enable these checks in different or all environments, as in the call below.
builder.Host.UseDefaultServiceProvider(options =>
{
    // Enable for captured dependencies.
    options.ValidateScopes = true;
    // Enable check for all service dependencies being registered with and resolvable by the DI container.
    options.ValidateOnBuild = true;
});

// Transient services are registered with the Add[Keyed]Transient* extension methods.
// Transient services are created anew every time they are requested. For example, if an endpoint handler requires an instance of a service, and that service is registered with the transient lifetime, every time that endpoint handler is invoked, it will receive a new instance of that service. And if one service has a dependency on a service that is registered as transient, that service will receive a new instance of the transient service every an instance of it is created.
// Because the DI always creates new instances of transient services, never re-using pre-existing instances, a service's dependency graph -- the graph of dependencies required to a construct a service instance -- may contain multiple different instances of the same service.
// The transient lifetime is best suited for services are A), inexpensive computationally to create, or B), must not be re-used, e.g., because they contain state and are not thread-safe.
//builder.Services.AddTransient<DataContext>();
//builder.Services.AddTransient<Repository>();

// Scoped services are registered with the Add[Keyed]Scoped* extension methods.
// Scoped services are created once per-scope and then re-used within that scope.
// In the context of ASP.NET's DI container, a "scope" is a single HTTP request. (Does anything else count as a scope?) While processing a given HTTP request, the same instance of a scoped service will be used repeatedly. While processing a different HTTP request, a different instance of the service will be created and re-used.
// Per scope, a service's dependency graph will contain a single instance of a scoped service. A dependency graph created for a different scope will use a different instance of a scoped service for the lifetime of that scope.
// The scoped lifetime is best suited for services that shouldn't outlive the request -- the book gave the example of database connections but I'm sure why this can't be re-used across requests? -- or that depend on the request in some way, e.g., using information from it.
// The book suggests this lifetime is most common and should be used for most services; I suppose it strikes a balance between re-use and isolation. 
//builder.Services.AddScoped<DataContext>();
//builder.Services.AddScoped<Repository>();


// Single services are registered with the Add[Keyed]Singleton extension methods.
// Singleton services are created once on app start-up and then re-used for the lifetime of the app instance.
// Regardless of what is requesting the service or what the scope is, the exact same instance of a singleton service will be injected.
// This imposes the requirement that singleton services must be thread-safe: the same service instance could be access by different threads processing different requests.
// The singleton lifetime is best suited for services that are A), expensive computationally to create, and B) that have no internal state, or what state they do have can be accessed in a thread-safe manner, e.g., it's best a thread-safe collection type.
//builder.Services.AddSingleton<DataContext>();
//builder.Services.AddSingleton<Repository>();

var app = builder.Build();

app.MapGet("/rows", RowCounts);

app.Run();

string RowCounts(DataContext db, Repository repository)
{
    int dbCount = db.RowCount;
    int repositoryCount = repository.RowCount;

    return $"""
        DataContext: {dbCount}
        Repository: {repositoryCount}
        """;
}


internal class DataContext
{
    public int RowCount { get; } = Random.Shared.Next(1, 1_000_000_000);
}


internal class Repository(DataContext dataContext)
{
    public int RowCount => dataContext.RowCount;
}