using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Consumer2;
using COnsumer2;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory {HostName = "localhost", UserName = "rabbit", Password = "rabbit"};
var connection = factory.CreateConnection();
var channel = connection.CreateModel();
string _exchange = "incident";
string _queue = "incidentConsumer2";
var subscriptions = new Dictionary<string, Type>()
{
    { typeof(IncidentLogged).Name, typeof(IncidentLogged) },
    { typeof(AgentAssignedToIncident).Name, typeof(AgentAssignedToIncident) },
    { typeof(IncidentCategorised).Name, typeof(IncidentCategorised) },
    { typeof(IncidentPrioritised).Name, typeof(IncidentPrioritised) },
};

channel.ExchangeDeclare(exchange: _exchange, type: ExchangeType.Direct);
channel.QueueDeclare(queue: _queue, durable: true, exclusive: false, autoDelete: false);

foreach (var eventName in subscriptions.Keys)
{
    channel.QueueBind(_queue, _exchange, eventName);
}

var consumer = new EventingBasicConsumer(channel);
Console.WriteLine("Listening for events........");

consumer.Received += (model, ea) =>
{
    var eventName = ea.RoutingKey;
    var message = Encoding.UTF8.GetString(ea.Body.Span);
    subscriptions.TryGetValue(eventName, out var type);

    var myEvent = DeserializeEvent(message, type);

    Console.WriteLine($"Received event {myEvent.GetType().Name} with incident id {myEvent.IncidentId}");
    Console.WriteLine("End........................");

    Thread.Sleep(15000);
    consumer.Model.BasicAck(ea.DeliveryTag, false);
};

channel.BasicConsume(
    queue: _queue,
    autoAck: false,
    consumer: consumer
);


IntegrationEvent DeserializeEvent(string message, Type eventType)
{
    return JsonSerializer.Deserialize(message, eventType) as IntegrationEvent;
}


Console.ReadLine();

JsonSerializerOptions serializerOptions = new()
{
    TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault ? new DefaultJsonTypeInfoResolver() : JsonTypeInfoResolver.Combine()
};