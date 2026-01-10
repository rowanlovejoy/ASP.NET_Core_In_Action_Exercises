using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// If there are multiple implementations of a service, it's possible to each inject an instance of each in a single parameter.
// Each implementation of the service -- defined by some kind of base type, class or interface, in this case IMessageSender -- must be registered with the DI container, as individual services would be.
builder.Services.AddScoped<IMessageSender, EmailSender>();
builder.Services.AddScoped<IMessageSender, SmsSender>();
builder.Services.AddScoped<IMessageSender, DiscordSender>();
// The TryAdd* extension methods can be used to conditionally registered a service implementation if an implementation of the same service hasn't already been registered.
// In this example, the RedditSender implementation of the IMessageSender service won't be registered because at least one other implementation has been already (three in this case: EmailSender, SmsSender, and DiscordSender).
// If the previous three implementations registered were not registered, RedditSender would be registered.
builder.Services.TryAddSingleton<IMessageSender, RedditSender>();

// It's possible to explicitly replace a previously registered implementation using the Replace extension method.
// If another implementation of the IMessageSender service has been registered, this Replace call will replace it with the RedditSender implementation.
// If no previous implementation has been registered, this call will simply register the RedditSender implementation with the specified lifetime, in this case scoped.
// The book says that to replace a previously registered implementation, the Replace call must specify the same lifetime as the implementation to be replaced; e.g., a scoped implementation can be replaced only if with another scoped implementation. However, this doesn't appear to be case, at least in ASP.NET 10. Here, I'm replacing a scoped implementation with a singleton, and the replacement is occurring as expected. The official docs also make no reference to this restriction. Perhaps this was a restriction present in ASP.NET 7, the subject of the book, but that has since been removed.
builder.Services.Replace(new ServiceDescriptor(typeof(IMessageSender), typeof(DiscordSender), ServiceLifetime.Singleton));

// To have multiple implementations of a service, multiple implementations of the same interface or base class are required.
// In this example, the service is defined by non-abstract a class; this base includes an implementation itself (hence its appearance in both the service and implementation generic parameters), and I'm also registering its two derived classes as is implementations.
builder.Services.AddScoped<ColourPrinter, ColourPrinter>();
builder.Services.AddScoped<ColourPrinter, InkjetPrinter>();
builder.Services.AddScoped<ColourPrinter, LaserPrinter>();

var app = builder.Build();

app.MapGet("/register/{username}", RegisterUser);
app.MapGet("/register/2/{username}", RegisterUser2);
app.MapGet("/print/{message}", PrintMessage);

app.Run();

// To inject an instance of each service implementation in an endpoint handler, the handler must accept a parameter of IEnumerable<T>, where T is the type defining the service.
// The DI container will inject an argument to this parameter that is an array of T (T[]), containing one item for each registered implementation of the service, in the same order in which they were registered with the DI container, i.e., the first implementation registered will have its instance in the first slot of the array.
string RegisterUser(string username, IEnumerable<IMessageSender> senders)
{
    // Actions can be performed with the injected implementations by simply looping over them, e.g., with a foreach loop.
    foreach (var sender in senders)
    {
        sender.SendMessage($"Welcome {username}");
    }

    return $"Welcome message sent to {username}";
}

// If multiple implementations of a service are registered, but an injection site, such as an endpoint handler, requires only a single instance, the last implementation registered will be the instance in injected.
// This endpoint handler requires a single instance of the IMessageSender service; as DiscordSender was the last implementation of the IMessageSender service to be registered, an instance of this implementation will be injected.
// This behaviour can be useful for overriding an implementation added by the framework or a third-party library, for example; just ensure your implementation is registered last, and it will be injected wherever a single implementation of that service is required.
string RegisterUser2(string username, IMessageSender sender)
{
    sender.SendMessage(username);

    return $"Welcome message sent to {username}";
}

// This handler will receive an array containing the three implementations of the ColourPrinter service: ColourPrinter itself, as a non-abstract class containing an implementation; and its two derived classes, InkjetPrinter and LaserPrinter
string PrintMessage(string message, IEnumerable<ColourPrinter> printers)
{
    foreach (var printer in printers)
    {
        printer.Print(message);
    }

    return $"Printed message \"{message}\" using all printers";
}

internal interface IMessageSender
{
    void SendMessage(string message);
}

public class EmailSender : IMessageSender
{
    public void SendMessage(string message)
    {
        Console.WriteLine("Sending message via email");
    }
}

public class SmsSender : IMessageSender
{
    public void SendMessage(string message)
    {
        Console.WriteLine("Sending message via SMS");
    }
}

public class DiscordSender : IMessageSender
{
    public void SendMessage(string message)
    {
        Console.WriteLine("Sending message via Discord DM");
    }
}

public class RedditSender : IMessageSender
{
    public void SendMessage(string message)
    {
        Console.WriteLine("Sending message via Reddit DM");
    }
}

public class ColourPrinter
{
    public virtual void Print(string message)
    {
        Console.WriteLine("Printing in colour...");
    }
}

public class InkjetPrinter : ColourPrinter
{
    public override void Print(string message)
    {
        base.Print(message);
        Console.WriteLine($"Printing message using inkjet: {message}");
    }
}

public class LaserPrinter : ColourPrinter
{
    public override void Print(string message)
    {
        base.Print(message);
        Console.WriteLine($"Printing message using laser: {message}");
    }
}