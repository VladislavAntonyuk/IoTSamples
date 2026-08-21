using HomeManagement.Shared;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace HomeManagement.Infrastructure.IpCameras;

public sealed class FrigateEventsSubscriber(Channel<WebHookModel> channel, ILogger<FrigateEventsSubscriber> logger)
{
    private readonly ConcurrentDictionary<string, DateTime> _seenEvents = new();

    public async Task OnFrigateEventAsync(FrigateEventMessage message)
    {
        logger.LogInformation("Received Frigate event: {MessageType}", message.Type);
        if (message.Type is not ("new" or "update"))
        {
            return;
        }

        var after = message.After;
        if (after is null || after.Label != "person" || string.IsNullOrEmpty(after.Id))
        {
            return;
        }

        if (!_seenEvents.TryAdd(after.Id, DateTime.UtcNow))
        {
            return;
        }

        var expiry = DateTime.UtcNow.AddMinutes(-10);
        foreach (var (key, time) in _seenEvents)
        {
            if (time < expiry)
            {
                _seenEvents.TryRemove(key, out _);
            }
        }

        var snapshotUrl = after.HasSnapshot
            ? $"http://cameras.home-management.local/api/events/{after.Id}/snapshot.jpg?bbox=1"
            : $"http://cameras.home-management.local/api/{after.Camera}/latest.jpg";

        await channel.Writer.WriteAsync(new WebHookModel
        {
            Message = $"Person detected on camera '{after.Camera}' (Score: {after.TopScore:P0}, Event ID: {after.Id}), Snapshot URL: {snapshotUrl}"
        });
    }
}