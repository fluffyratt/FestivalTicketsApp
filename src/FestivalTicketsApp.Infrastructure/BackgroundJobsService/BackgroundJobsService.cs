using System;
using System.Threading.Tasks;
using FestivalTicketsApp.Application.EventService;
using FestivalTicketsApp.Shared;

namespace FestivalTicketsApp.Infrastructure.BackgroundJobsService
{
    public class BackgroundJobsService : IBackgroundJobsService
    {
        private readonly IEventService _eventService;

        public BackgroundJobsService(IEventService eventService /*, IBackgroundJobClient backgroundWorker*/)
        {
            _eventService = eventService;
            // Ми прибрали backgroundWorker, бо локально не використовуємо Hangfire
        }

        public Task ScheduleEventArchiveTask(int eventId, DateTime startDate)
        {
            // TODO: у проді тут можна знову підключити Hangfire.
            // Для локального запуску просто нічого не робимо.
            return Task.CompletedTask;
        }

        public Task<Result> CancelEventArchiveTask(int eventId)
        {
            // TODO: у проді тут мала б бути логіка скасування фонового завдання.
            // Для локального запуску просто повертаємо успіх.
            return Task.FromResult(Result.Success());
        }
    }
}
