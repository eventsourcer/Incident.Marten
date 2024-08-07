namespace Incident.Api.Settings;

public class RabbitMQSettings
{
    public const string Section = "RabbitMQ";
    public required string Hostname { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}