var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
// File-based configuration sources, like those for JSON files and XML files, support watching the specified files for changes and rebuilding the IConfigurationRoot if any of the watched files are updated.
// This enables configuration settings to be changed while the app is running -- e.g., in production -- and for those changes to be reflected in the running app, without requiring an app recompilation or restart.
// The books gives the example of using updating the log level of the app while it's running to get the detail needed to diagnose a fault.
// (The book says this watching behaviour is generally available for file-based configuration sources only; environment variable sources don't support this, for example. The wording implies there *may* be some non-file-based sources that do support reloading on changes, maybe those from third-parties.)
// For the including JSON file configuration source, added using the .AddJsonFile() extension method, file watching and reloading on changes can be enabled by passing true to the reloadOnChange parameter, as in the call on the following line.
// This call registers the appsettings.json file as a JSON file configuration source, and sets up a file watcher that will trigger a rebuild of the IConfigurationRoot should the contents of appsettings.json change.
// The default appsettings.json configuration source registered automatically by WebApplicationBuilder is configured with reloadOnChange: true. (Are the other JSON file sources also configured this way?)
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
// The .AddXmlFile() extension methods also supports file watching
// (NB: the top-level <configuration> element appears to be required for XML files.)
builder.Configuration.AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true);

var app = builder.Build();

// If the value of the MyName setting the configuration is updated, i.e., by changing the key value of corresponding key in appsettings.json, this endpoint will return the updated value.
app.MapGet("/", (IConfiguration configuration) => configuration.AsEnumerable());
app.MapGet("/name", (IConfiguration configuration) => $"My name is {configuration["MyName"]}");

app.Run();
