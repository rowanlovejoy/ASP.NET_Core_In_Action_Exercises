var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/register/{username}", RegisterUser);

app.Run();

string RegisterUser(string username)
{
    var emailSender = new EmailSender(
        new NetworkClient(
            new EmailServerSettings(
                host: "localhost",
                port: 80)),
        new MessageFactory());
    emailSender.SendEmail(username);
    return $"Email sent to {username}";
}

internal class EmailSender
{
    private readonly NetworkClient networkClient;
    private readonly MessageFactory messageFactory;

    public EmailSender(NetworkClient networkClient, MessageFactory messageFactory)
    {
        this.networkClient = networkClient;
        this.messageFactory = messageFactory;
    }

    public void SendEmail(string username)
    {
        var email = messageFactory.Create();
        this.networkClient.Send(email);
        Console.WriteLine($"Sent email to {username}");
    }
}

internal class NetworkClient
{
    private readonly EmailServerSettings serverSettings;

    public NetworkClient(EmailServerSettings serverSettings)
    {
        this.serverSettings = serverSettings;
    }

    internal void Send(object email)
    {
        Console.WriteLine($"Sent email: {email}");
    }
}

public class EmailServerSettings
{
    public EmailServerSettings(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public string Host { get; }
    public int Port { get; }
}

internal class MessageFactory
{
    internal object Create()
    {
        return "Created message";
    }
}