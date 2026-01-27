using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ASP.NET supports binding -- in the same manner as model binding -- sections of configuration to POCOs (plain-old C# objects) -- providing convenient way to represent and access sections of configuration via appropriately typed C# objects.
// To be eligible for binding to configuration settings, the type must be non-abstract and have a public, parameterless constructor. Init-only properties are allowed. Records are not supported (presumably because their constructors have parameters?)
// There are two ways to implement the options pattern; the book covers the "options pattern", the DI-oriented approach using IServiceCollection.Configure.
// The following line retrieves a section of the configuration root with the name of "MapSettings" (using the same name for the class and the configuration section enables use of the nameof expression to reduce duplication). It then registers an instance of a singleton lifetime instance of the IOptions<T> service, where T is the POCO type (MapSettings in this case). This interface exposes the property .Value, which -- once bound -- will contain an instantiated T with its properties bound to the settings from the specified configuration section.
builder.Services.Configure<MapSettings>(builder.Configuration.GetSection(nameof(MapSettings)));
// This line is identical to the above, except it passes the "AppDisplaySettings" as the name of the section to bind to (nameof(AppDisplaySettings)), creating another singleton lifetime instance of the IOptions service where T = AppDisplaySettings.
builder.Services.Configure<AppDisplaySettings>(builder.Configuration.GetSection(nameof(AppDisplaySettings)));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

// Once registered, the required instance of the IOptions service can be retrieved via dependency injection by adding a parameter of type IOptions<T>, where T is the POCO bound to configuration section you want to access.
// Binding to specified configuration section happens at the point of injection by the DI container, not at the point of registration with the DI container. Therefore, if there is a binding error between MapSettings and the "MapSettings" configuration section, it will surface only when this endpoint is called.
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

app.Run();

internal class MapSettings
{
    internal class Location
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    // During binding, ASP.NET will attempt to bind each property a setting with the corresponding key at the same level in the hierarchy.
    // It will attempt to bind the DefaultZoomLevel property below to a setting named "DefaultZoomLevel" (key binding is case insensitive) nested immediately inside the configuration section with the key "MapSettings" (again, matching case insensitively)
    // The framework will attempt to convert the value of setting to the type of property, and throw any error if it cannot do so, e.g., when attempting to convert the string "apple" to a int.
    // If no matching configuration setting is found, the property will have the default value for its type or value returned by its property initialiser. For the DefaultZoomLevel property, if no "DefaultZoomLevel" key exists in the expected location of the configuration, the property in the instantiated POCO have the value 5.
    public int DefaultZoomLevel { get; init; } = 5;

    // The POCO type can itself contain a property that binds to a nested configuration section.
    // The DefaultLocation property below will be bound to a section named "DefaultLocation" immediately inside the "MapSettings" section, and that itself immediately contains the settings "Latitude" and "Longitude" whose values can be converted to doubles.
    public required Location DefaultLocation { get; init; }
}

internal class AppDisplaySettings
{
    // The required modifier is required here to satisfy the compiler, as Title is declared as a non-nullable string (another way to satisfy it would be to provide a default value).
    // This modifier is misleading the context of the options pattern; it doesn't guarantee that the specified configuration setting exists or that it has a non-null value.
    // If setting doesn't exist, the Title property below will have the value "null".
    // Even if the setting does exist and is explicitly bound to null, the binder will not throw an error and happily bind the value "null" to a property explicitly declared as a required, non-nullable string.
    public required string Title { get; init; }
    public bool ShowCopyright { get; init; }
}
