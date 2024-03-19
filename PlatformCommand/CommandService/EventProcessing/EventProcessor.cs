using System.Text.Json;
using AutoMapper;
using CommandService.Data;
using CommandService.Dtos;
using CommandService.Models;

namespace CommandService.EventProcessing
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;

        public EventProcessor(
                    IServiceScopeFactory serviceScopeFactory,
                    IMapper mapper)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _mapper = mapper;
        }
        public void ProcessEvent(string message)
        {
            var eventType = DetermineEvent(message);
            switch (eventType)
            {
                case EventType.PlatformPublished:
                    addPlatform(message);
                    break;
                default:
                    break;
            }
        }

        private EventType DetermineEvent(string notificationMessage)
        {
            Console.WriteLine("--> Determining Event");
            var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);
            Console.WriteLine($"--> NotficiationMessage.Event: {eventType.Event}");
            switch (eventType.Event)
            {
                case "Platform_Published":
                    Console.WriteLine("--> Platform_Published event detected");
                    return EventType.PlatformPublished;
                default:
                    Console.WriteLine("--> Could not determine event type");
                    return EventType.Undetermined;
            }
        }

        private void addPlatform(string platformPublishedMessage)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<ICommandRepository>();
                var platformPublishedDto = JsonSerializer.Deserialize<PlatformPublishedDto>(platformPublishedMessage);
                try
                {
                    var platform = _mapper.Map<Platform>(platformPublishedDto);
                    if (!repository.ExternalPlatformExists(platform.ExternalId))
                    {
                        repository.CreatePlatform(platform);
                        repository.SaveChanges();
                        Console.WriteLine("--> Platform Added!");
                    }
                    else
                    {
                        Console.WriteLine("--> Platform already exists");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--> Could not add platform to DB: {ex.Message}");
                }
            }
        }
    }

    enum EventType
    {
        PlatformPublished,
        Undetermined
    }
}