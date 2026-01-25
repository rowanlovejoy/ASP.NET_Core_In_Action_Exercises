var builder = WebApplication.CreateBuilder(args);

// ASP.NET is able to load settings (key-value pairs that affect the running program) from a range of sources, including files of different types, environment variables, and command line arguments.
// Once read from a source, a setting's value can be retrieved by referencing its key, e.g., given a source the reads from the JSON file with contents { "my-key": "my-value" }, the key would be "my-key", and the value of setting retrieved by referencing the key would "my-value".
// Regardless of the type of the value in the original configuration source, e.g., a JSON number value, the provider will read and store the setting's value as a string. (The book states it's the provider that does this; is it a behaviour required by the base type? Could I write a provider that does respect the original source type?)
// The framework's configuration system is extensible: new configuration sources -- a file or something that can provide key-value pairs can be read in -- can implemented by users -- the book author has created a YAML configuration provider, for example.
// The configuration system includes three main interfaces: IConfigurationSource, IConfigurationBuilder, and IConfigurationRoot.
// IConfigurationSource describes a source of settings and how to read from it -- for example, an IConfigurationSource implementation might describe how to settings from a JSON file.
// IConfigurationBuilder describes how to construct the final set of settings from the various IConfigurationSource implementations registered with it.
// IConfigurationRoot stores the final set of settings that have been read from various registered sources.

// ConfigurationManager is class that implements both IConfigurationBuilder and IConfigurationRoot.
// It is exposed on WebApplicationBuilder and WebApplication via the Configuration property.
// Being an IConfigurationBuilder, IConfigurationSource implementations can be registered with it.
// Being an IConfigurationRoot, it stores and exposes the settings read from the IConfigurationSource implementations registered with it.
// Before ASP.NET 6, separate classes implemented IConfigurationBuilder and IConfigurationRoot, with a .Build() method call required to create IConfigurationRoot from the IConfigurationBuilder.
// From ASP.NET 6 onwards, ConfigurationManager fulfils both, automatically reading settings from sources registered with it and making them available to the appliation code.

// WebApplicationBuilder automatically registers several configuration providers (IConfigurationProvider implementations) by default; these support loading settings from appsettings.json and environment-specific variations thereof, secrets, environment variables, .ini files, and command line arguments.
// The order in which providers are registered with the ConfigurationManager determines the order in which settings are read from them -- first provider registered is read from first, and the last provider last.
// If same setting -- the same key-value pair -- is defined by multiple configuration sources, sources read later overwrite sources read earlier.
// The default providers are configured such that their priority isn't strictly linear, i.e., the last provider read always wins out: command-line arguments are registered twice, once at the beginning and at the end; certain environment variables have higher priority than others and from settings from other providers. See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0#default-app-configuration-sources
// The default providers can be un-registered by calling the .Clear() method on the list configuration sources (theses sources have the IConfigurationSource interface; where does IConfigurationProvider come into this?)
// The following two lines un-register the default configuration sources and the register a JSON file named appsettings.json as an optional source. Making the source optional enables the app to start even if this file cannot be read; otherwise, an exception would be thrown if the file could not be read.
builder.Configuration.Sources.Clear();
// All default providers have been un-registerd using .Clear(); the order of the following registrations determine the exact order in which configuration sources will be read from and their priority.
// The sharedsettings.json configuration provider is registered first and therefore has the lowest priority; any future provider that provides a setting with the same key as its definition of that setting
builder.Configuration.AddJsonFile("sharedsettings.json", optional: true);
// The appsettings.json configuration provider is registered second and therefore has higher priority than sharedsettings.json; settings defined in appsettings.json will overwrite settings with the same keys in sharedsettings.json.
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
// The environment variables configuration provider is registered last and therefore has the highest priority of the three registered providers; any settings defined as environment variables will overwrite those with the same keys read from sharedsettings.json and appsettings.json.
// appsettings.json defines the setting MyAppConnectionString, and this project has the environment MyAppConnectionString defined in its launchSettings.json (this variable will be added to the environment on start-up).
// Because the environment variables configuration source is added last, it has the highest priority. Therefore, only the environment variable MyAppConnectionString will be incorporated into the final set of configuration values; it overwrites the identically named setting in appsettings.json.
builder.Configuration.AddEnvironmentVariables();

// See comments /yaml endpoint handler below.
//builder.Configuration.AddYamlFile("appsettings.yml", optional: true);

var app = builder.Build();

// Returns a list of the settings read from the registered configuration sources. In this case, it returns a list of key-value pairs read from the appsettings.json file
app.MapGet("/", () => app.Configuration.AsEnumerable());
// While the ConfigurationManager object can be used directly in endpoint handlers, it is registered with the DI container, enabling it be access using DI. ConfigurationManger implements IConfigurationRoot, which extends IConfiguration; ConfigurationManager is registered with the DI container as an implementation of the IConfiguration service, and can be referenced as such. The following endpoint has identical behaviour to the above.
app.MapGet("/inject", (IConfiguration configuration) => configuration.AsEnumerable());

// The IConfiguration service supports accessing specific settings via key. It exposes methods for this purpose, and also an indexer, enabling dictionary-like access to settings as shown in the following example.
app.MapGet("/zoom", (IConfiguration configuration) =>
{
    // For hierarchical sources, like JSON files, the hierarchy is preserved in the framework.
    // To access the nested DefaultZoomLevel setting's value, specify the key path to it in string form, separating keys with a colon (:)
    // The following line reads from the JSON structure { "MapSettings": { "DefaultZoomLevel": ... } }
    // Note the returned value is of type string?, despite the original JSON value being number.
    // If not key matches that provider, null will be returned instead, hence the nullable string (string?) return type
    var zoomLevel = configuration["MapSettings:DefaultZoomLevel"];
    return $"Default zoom level: {zoomLevel}";
});

app.MapGet("/location", (IConfiguration configuration) =>
{
    // For hierarchical data like JSON object trees, nested sections can be retrieved instead of individual values; doing so "resets the namespace" (as the book puts it), enabling values nested within the retrieved section to themselves be retrieved using shorter paths.
    // Retrieving a section returns an IConfigurationSection instance, which extends IConfiguration; the API for accessing values from the section therefore does not change compared to reading from the configuration root (the injected IConfiguration service).
    // The following lines retrieve MapSettings:DefaultLocation setting and access the Latitude and Longitude values from it; when access these values, because the section is now the root, the paths can be shorter. 
    var locationSection = configuration.GetSection("MapSettings:DefaultLocation");
    // In the appsetings.json file, these latitude and longitude values are assigned to the keys "Longitude" and "Latitude" respectively, with uppercase Ls. In the two following lines, these values are retrieved using the keys "longitude" and "latitude", with lowercase Ls. When accessing values from IConfiguration, keys are case-insensitive.
    var latitude = locationSection["latitude"];
    var longitude = locationSection["longitude"];
    return $"""
    Default location:
    lat: {latitude}
    long: {longitude}
    """;
});

// Case-insensitivity has implications for case-sensitive configuration sources; YAML, for example, uses case-sensitive keys. Given a YAML file with two identical keys that differ only in casing, only one of these keys will be read into configuration (presumably whatever is read last).
// Based on the book's explanation, I attempted to create an example demonstrating the above behaviour. However, the YAML IConfigurationProvider written by the book's author throws an error when attempting to load the YAML file because of duplicate keys. The README for the provider's source code repo highlights this behaviour, but it seems as odds with the book, which implies loading a YAML with keys differentiated by case alone would be allowed, but would not behave as expected, i.e., only one of the key-value pairs would be add the IConfigurationRoot.
app.MapGet("/yaml", (IConfiguration configuration) =>
{
    return $"My name is {configuration["Name"]}";
});

app.Run();
