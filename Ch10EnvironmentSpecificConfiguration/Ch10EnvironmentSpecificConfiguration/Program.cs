var builder = WebApplication.CreateBuilder(args);

// ASP.NET apps identify the environment they're running in by querying one of two environment variables: DOTNET_ENVIRONMENT AND ASPNETCORE_ENVIRONMENT; the latter overrides the former if both are present.
// The "environment", in the context ASP.NET, is simply a string value read from the environment -- an environment variable -- that developers can use to alter an app's behaviour.
// The environment could be configured as any string, but the framework by default supports three standard environment values: "Development", "Staging", and "Production".
// Helper extension methods on IHostEnvironment -- available on WebApplicationBuilder.Environment and WebApplication.Environment -- are provide for case-insensitively checking for these standard environment strings -- .IsDevelopment(), .IsStaging(), and .IsProduction(). A method for generically querying for any environment value, .IsEnvironment(), is also provided.
// The IHostEnvironment interface is the primary means of querying environmental information, including the environment name and other values such as the content root path -- the directory in which the framework's searches for configuration files.
// The environment is typically configured differently on the different environments the app may run in. For example, by default, Visual Studio generates projects with a launchSettings.json file that sets ASPNETCORE_ENVIRONMENT to "Development". This value will be read by the framework on start-up and used to initialise an IHostEnvironment instance, which will then by queried by code that needs to change its behaviour based on the environment. For example, by the default, WebApplicationBuilder adds the DeveloperException middleware only if the environment is set "Development".
// On a production service, the environment might be set as "Production", which with the default configuration will modify settings to improve performance and security compared to behaviour when running with the "Development" environment value. "Production" is the default assumed environment if neither DOTNET_ENVIRONMENT nor ASPNETCORE_ENVIRONMENT is present.
// When framework has loaded a particular environment value, the app can be said to be "running" in that environment -- if the environment value is "Development", the framework is running the development environment.

// When developing locally, launchSetting.json is the most convenient way to configure the app's environment, e.g., the environment name and URLs to which the app listens.
// launchSetting.jsoon is created by default ASP.NET templates and includes default profiles -- sets of settings used when a profile is selected, e.g., in Visual Studio using the arrow dropdown next to the Start Debugging button. Environment variables configured in launchSettings.json override those set on the system.
// launchSettings.json is intended to be used during local development only; it is apparently not bundled when publishing an ASP.NET app. (The book and the docs state that launchSetting.json isn't "deployed" when an app is published; based on this Stack Overflow post https://stackoverflow.com/questions/55486917/asp-net-core-publish-and-launch-settings, this seems to mean the file isn't included in the build artifacts created by the "dotnet publish" command.)

// One use for environment-specific behaviour is loading different configuration values depending on the environment.
// For example, when in the "Development" environment -- typically configured on development machine -- logging might be configured to be more detailed and verbose to aid debugging. When running in the "Production environment value -- typically set on a production server handling end users -- logging might be reduced to surface only critical issues and reduce noise.
// Environment specific configuration files are implemented by default when using WebApplicationBuilder and its default ConfigurationManager. The framework first loads an "appsetting.json" file, and then, additionally and optionally, one of three environment-specific appsettngs.json files: appsettings.Development.json, appsettings.Staging.json, and appsettings.Production.json.
// The following lines manually recreate this default behaviour.
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    // The optional, environment-specific configuration file is loaded second, enabling environment-specific settings to override those set in the appsettings.json configuration, which is available on all environments.
    // The pattern is generally to configure settings applicable to all environments in appsettings.json, and then override them as need per in environment in the relevant appsettings.<environent_name>.json files.
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

// An example of environment specific configuration included by default is registering the users secrets configuration provider only in the development environment.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

var app = builder.Build();

// Middleware can also be registered conditionally based on the environment.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

// IHostEnvironment can be injected anywhere in an app. (Is it registered with DI or is it a well-known type? Is there a difference?)
// The book recommends against querying host environment details directly application code, which you might do when injecting IHostEnvironment; instead, it recommends loading different configuration values per environment and relying on them to alter behaviour.
app.MapGet("/", (IHostEnvironment environment, IConfiguration configuration) =>
{
    return $"""
    The environment is {environment.EnvironmentName}
    My name is {configuration["MyName"]}
    """;
});

app.Run();
