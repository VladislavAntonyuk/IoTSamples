using System.Text.Json;
using MQTTnet.Formatter;
using Silverback.Messaging.Configuration;

namespace HomeManagement.Infrastructure;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddMqttClients(
                clients => clients
                    .ConnectViaTcp("localhost", 1883)
                    .AddClient(
                        client => client
                            .WithClientId("frigate-client")
                            .UseProtocolVersion(MqttProtocolVersion.V500)
                            .Consume(
                                endpoint => endpoint
                                    .ConsumeFrom("frigate/events")
                                    .DeserializeJson(serializer => serializer.Configure(o=>
                                    {
                                        o.PropertyNameCaseInsensitive = true;
                                        o.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                                    }).UseModel<FrigateEventMessage>().IgnoreMessageTypeHeader())
                                    .WithExactlyOnceQoS()
                                    .OnError(policy => policy.Skip()))));
    }
}


public class FrigateEventMessage
{
    public After? After { get; set; }
    public string? Type { get; set; }
}

public class After
{
    public string Id { get; set; }
    public string Camera { get; set; }
    public string Label { get; set; }
    public float TopScore { get; set; }
    public bool HasSnapshot { get; set; }
}