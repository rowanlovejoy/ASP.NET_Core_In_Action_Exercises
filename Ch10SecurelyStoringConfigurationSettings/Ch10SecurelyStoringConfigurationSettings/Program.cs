var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();

// When working with secret settings -- passwords, connection strings, API keys, or anything else that shouldn't be committed to source control -- that need to be included in the apps' configuration, is advisable to store these settings outside the project's file tree, i.e., outside the repo.
// Two suitable configuration sources for secret settings are environment variables and user secrets.
// WebApplicationBuilder automatically registers multiple environment variable configuration sources, with certain variables having higher priority than others.
// Environment variable configuration sources can be added manually using the .AddEnvironmentVariables() extension method on IConfigurationBuilder.
// By default, this extension adds all environment variables on the host machine (in the context of Docker, the app's host machine would be the container) to the ConfigurationManager.
builder.Configuration.AddEnvironmentVariables();
// Instead of adding all environment variables on the host machine, the selection can be limited to only those variables with a given prefix.
// The following call will add all environment variables with the prefix "Demo" to the ConfigurationManager. The specified prefix is stripped from the variable's key when it is read into the ConfigurationManager. Accordingly, environment variables with different prefixes can overwrite each other.
// Given the following configuration sources and an environment containing the variables, MySecret and DemoMySecret, the "Demo" prefix would be stripped, and the previously prefixed MySecret will overwrite that lacked the prefix.
builder.Configuration.AddEnvironmentVariables("Demo");
// The environment variable source with the SpecialDemo prefix is registered after the source with the Demo prefix; any variables it provides that, after stripping the prefixes, match those proved by source with the Demo prefix, will overwrite them.
// Given the complete set of environment variable providers, environment variables on the system would have following priority, in descending order: SpecialDemo<variable_name> -> Demo<variable_name> -> <variable_name>
// The environment variables configuration source strips the prefix after the reading, so regardless of which prefix wins out, the final configuration setting will be named <variable_name>
builder.Configuration.AddEnvironmentVariables("SpecialDemo");

// User secrets is tool provided included with the .NET CLI that stores values in a platform-specific location on the host machine. The user secrets store is a JSON file named secrets.json.
// Each .NET app has a user secrets ID that uniquely identifies its secrets store -- the ID is incorporated into the path of the settings.json file. This enables apps on the same host machine to keep their secrets separate. This ID is stored in the project's .csproj file under the <UserSecretsId> key (this app's ID is d01551ac-ac23-401d-9173-e048589b41ac).
// User secrets are not encrypted in any way; they are secret only in so far as they are kept separate from source control and are segregated per app.
// The user secrets store can be initialised for a given app using the .NET CLI (from the project directory) or Visual Studio. Once initialised, the settings.json file can be edited manually, assuming one knows the ID that corresponds to a given app. The .NET CLI -- use from the project's directory -- provides a API for managing the file and its contents; Visual Studio provides an editor for opening and editing the file within the IDE.
// The book and the official Microsoft documentation advise that user secrets should be used only during development; that is, when the ASP.NET environment is set to "Development". (The launchSettings.json file sets the environment to Development.) Indeed, the user secrets WebApplicationBuilder configuration source automatically by WebApplicationBuilder is configured this way.
// The book additionally recommends that environment variables (or a dedicated encrypted secrets store) should be used storing secret settings in production environments.
// Neither makes clear clear why user secrets should be used only in development. User secrets appears no less secure than environment variables: both are stored outside the code and neither are encrypted. 
// Environment variables have the advantage of being platform agnostic; they're available on platforms and various cloud providers enable easy editing of them. To use user secrets without having to re-initialise it on the production server, a settings.json file containing the secrets needs to exist on the production machine at same path it existed at on the development machine (the unique ID is part of the path and would need to be extracted from the .csproj). Considering this, environment variables win out by sheer convenience.
if (builder.Environment.IsDevelopment())
{
    // The user secrets ID for the app will be incorporated into the app's assembly when published.
    // The .AddUserSecrets() extension method used for adding the user secrets configuration source needs the app's Program class as a type argument so that it knows which assembly contains the user secrets ID.
    builder.Configuration.AddUserSecrets<Program>();
}

var app = builder.Build();

app.MapGet("/", (IConfiguration configuration) => configuration.AsEnumerable());

app.MapGet("/env", (IConfiguration configuration) =>
{
    // Environment variables can be given hierarchical sections once read into the ConfigurationManager.
    // This is achieved by prefixing the environment variable's name with one or more section names, separated by a colon (:) or double underscore (__).
    // For example, the variables TopLevel:FirstName and TopLevel:LastName would be both be under the TopLevel section in the final configuration.
    // Note that some environments -- Linux is given as an example -- do not support colons in environment variable names. Using a double underscore is supported in all environments; for simplicity and consistency, using double underscores on platforms is advisable.
    // Regardless of the section separated used in the environment variables name, once read in the configuration, the colon syntax must be used to traverse the hierarchy. For example, given an environment variable named MyRoot__MyValue, it would be accessed in application code using MyRoot:MyValue.
    var sectionVars = configuration.GetSection("MySection");
    return $"""
    var from section: {sectionVars["MyName"]}
    var from root: {configuration["MySection:MyName"]}
    """;
});

// The secret.json file corresponding to this app contains the setting "MyOtherSecret".
app.MapGet("/secret", (IConfiguration configuration) =>
{
    return $"My other secret: {configuration["MyOtherSecret"]}";
});

app.Run();
