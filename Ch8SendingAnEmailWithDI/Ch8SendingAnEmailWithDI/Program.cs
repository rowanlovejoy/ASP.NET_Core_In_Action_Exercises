var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEmailSender();

var app = builder.Build();

app.MapGet("/register/{username}", RegisterUser);

app.Run();

string RegisterUser(string username, IEmailSender emailSender)
{
    emailSender.SendEmail(username);
    return $"Email sent to {username}";
}

internal interface IEmailSender
{
    void SendEmail(string username);
}

internal class EmailSender(NetworkClient networkClient, MessageFactory messageFactory) : IEmailSender
{
    public void SendEmail(string username)
    {
        var email = messageFactory.Create();
        networkClient.Send(email);
        Console.WriteLine($"Sent email to {username}");
    }
}

internal class NetworkClient(EmailServerSettings serverSettings)
{
    internal void Send(object email)
    {
        Console.WriteLine($"Sent email {email} using {serverSettings}");
    }
}

public record EmailServerSettings(string Host, int Port);


internal class MessageFactory
{
    internal object Create()
    {
        return "Created message";
    }
}

public static class EmailSenderServiceCollectionExtensions
{
    public static IServiceCollection AddEmailSender(this IServiceCollection services)
    {
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<NetworkClient>();
        services.AddSingleton<MessageFactory>();
        services.AddScoped(provider =>
        {
            return new EmailServerSettings(Host: "smtp.server.com", Port: 1025);
        });

        return services;
    }
}