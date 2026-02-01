using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// In ASP.NET, the "options pattern" enables the binding -- in the same manner as model binding -- of configuration sections (including the root itself) to POCOs (plain-old C# objects) -- providing a convenient way to represent and access sections of configuration via appropriately typed C# objects.
// To be eligible for binding to configuration sections, a type must be non-abstract and have a public, parameterless constructor. Init-only properties are allowed. Records are not supported (presumably because their constructors have parameters?)
// There are two ways to implement the the options pattern; the book covers the DI-oriented approach using the IOptions<T> interface and IServiceCollection.Configure.
// The following line retrieves a section of the configuration root with the name of "MapSettings" (using the same name for the class and the configuration section enables use of the nameof expression to reduce duplication). It then registers a singleton lifetime instance of the IOptions<T> service, where T is the POCO type (MapSettings in this case). This interface exposes the property .Value, which -- once bound -- will contain an instantiated T with its properties bound to the settings from the specified configuration section.
builder.Services.Configure<MapSettings>(builder.Configuration.GetSection(nameof(MapSettings)));
// This line is identical to the above, except it passes "AppDisplaySettings" as the name of the section to bind to (nameof(AppDisplaySettings)), creating another singleton lifetime instance of the IOptions service where T = AppDisplaySettings.
builder.Services.Configure<AppDisplaySettings>(builder.Configuration.GetSection(nameof(AppDisplaySettings)));
// Accessing configuration using the IOptions<T> interface facilitates testing, as it enables configuration classes in the DI container to be easily replaced with mocks by registering a different implementation.

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

// Once registered, the required instance of the IOptions service can be retrieved via dependency injection by adding a parameter of type IOptions<T>, where T is the POCO bound to configuration section you want to access.
// Binding to specified configuration section happens at the point of injection by the DI container, not at the point of registration with the DI container. Therefore, if there is a binding error between the MapSettings class and the "MapSettings" configuration section, it will surface only when this endpoint is called.
// Snice IOptions<T> is registered with the singleton lifetime, once it's been injected once, it won't be created and bound again.
app.MapGet("/map-settings", (IOptions<MapSettings> mapSettings) =>
{
    // Access the .Value property to retrieve the bound POCO.
    return mapSettings.Value;
});

app.MapGet("/display-settings", (IOptions<AppDisplaySettings> displaySettings) =>
{
    var title = displaySettings.Value.Title;
    var showCopyright = displaySettings.Value.ShowCopyright;

    return new { title, showCopyright };
});

// IOptions<T> is registered with the singleton lifetime; binding happens on first injection and thereafter the same instance -- with the same bound values -- is reused.
// When calling IServiceCollection.Configure() to bind a configuration section to a POCO, T, an additional service is registered alongside IOptions<T>: IOptionsSnapshot<T>.
// IOptionsSnapshot<T> similarly provides access to a bound POCO on its .Value property; the difference is that it's registered with scoped lifetime, and its POCO, T, is rebound every time it is injected.
// In the following example, a different instance of IOptionsSnapshot will be created for every request. If the settings in the "MapSettings" section change between two requests, the changes will be reflected in .Value property of the IOptionsSnapshot<MapSettings> bound and injected for the second request.
// The book makes it unclear when rebinding occurs when using IOptionsSnapshot<T>. It first reads that a new instance will be created "when needed", but goes on to point out performance implications of rebinding with every request. This first point implies some additional logic beyond the behaviour of the scoped lifetime: a new service instance will always be injected, but binding only occurs "as needed", i.e., when the underlying configuration source, e.g., an appsettings.json file, has been changed. The second point instead implies no such behaviour: binding be performed whenever a new instance is created and developers should be alert to performance problems this might cause. According to the official Microsoft docs, the behaviour implied by the second point is the truth of the situation.
app.MapGet("/map-snapshot", (IOptionsSnapshot<MapSettings> mapSettings) =>
{
    return mapSettings.Value;
});

app.Run();

internal class MapSettings
{
    internal class Location
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    // During binding, ASP.NET will attempt to bind each property to a setting with the corresponding key matching its name at the same level in the hierarchy.
    // It will attempt to bind the DefaultZoomLevel property below to a setting named "DefaultZoomLevel" (key binding is case insensitive) nested immediately inside the configuration section with the key "MapSettings" (again, matching case insensitively).
    // The framework will attempt to convert the value of the setting to the type of the property it is attempting to bind it to, and throw an error if it cannot do so, e.g., when attempting to convert the string "apple" to a int.
    // If no matching configuration setting is found, the property will have the default value for its type or the value returned by its property initialiser. For the DefaultZoomLevel property, if no "DefaultZoomLevel" key exists in the expected location of the configuration, the property in the instantiated POCO have the value 5 provided by its initialiser.
    // Validation during binding is supported: it uses data annotations on the POCO classes and required a different method of registering the bound class with the DI container. See https://andrewlock.net/adding-validation-to-strongly-typed-configuration-objects-in-dotnet-6/
    // Validation of settings during binding is mentioned by the book in note, and the above article linked to -- but why isn't this a section in the book? The validation as introduced in ASP.NET 6; the book is about 7.
    public int DefaultZoomLevel { get; init; } = 5;

    // The POCO type can itself contain a property that binds to a nested configuration section.
    // The DefaultLocation property below will be bound to a section named "DefaultLocation" immediately inside the "MapSettings" section, and that itself immediately contains the settings "Latitude" and "Longitude" whose values can be converted to doubles.
    public required Location DefaultLocation { get; init; }
}

internal class AppDisplaySettings
{
    // The required modifier is required here to satisfy the compiler, as Title is declared as a non-nullable string (another way to satisfy it would be to provide a default value).
    // This modifier is misleading in the context of the options pattern; it doesn't guarantee that the specified configuration setting exists or that it has a non-null value.
    // If setting doesn't exist, the Title property below will have the value "null", despite being as required and declared non-nullable.
    // Even if the setting does exist and is explicitly bound to null, the binder will not throw an error and happily bind the value "null" to a property explicitly declared as a required, non-nullable string.
    public required string Title { get; init; }
    public bool ShowCopyright { get; init; }
}
