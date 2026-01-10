var builder = WebApplication.CreateBuilder(args);

// If there are multiple implementations of a service, it's possible to each inject an instance of each in a single parameter.
// Each implementation of the service -- defined by some kind of base type, class or interface, in this case IMessageSender -- must be registered with the DI container, as individual services would be.
builder.Services.AddScoped<IMessageSender, EmailSender>();
builder.Services.AddScoped<IMessageSender, SmsSender>();
builder.Services.AddScoped<IMessageSender, DiscordSender>();

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