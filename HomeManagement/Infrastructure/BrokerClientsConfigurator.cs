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
                                    .WithAtLeastOnceQoS()
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
    public float FrameTime { get; set; }
    public string Label { get; set; }
    public float TopScore { get; set; }
    public bool FalsePositive { get; set; }
    public float StartTime { get; set; }
    public float? EndTime { get; set; }
    public float Score { get; set; }
    public int Area { get; set; }
    public float Ratio { get; set; }
    public bool Active { get; set; }
    public bool Stationary { get; set; }
    public int MotionlessCount { get; set; }
    public int PositionChanges { get; set; }
    public bool HasClip { get; set; }
    public bool HasSnapshot { get; set; }
    public bool PendingLoitering { get; set; }
    public string MaxSeverity { get; set; }
    public int CurrentEstimatedSpeed { get; set; }
    public int AverageEstimatedSpeed { get; set; }
    public int VelocityAngle { get; set; }
}