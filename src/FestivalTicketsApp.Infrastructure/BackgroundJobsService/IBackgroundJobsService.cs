using FestivalTicketsApp.Shared;

namespace FestivalTicketsApp.Infrastructure.BackgroundJobsService;

public interface IBackgroundJobsService
{
    Task ScheduleEventArchiveTask(int eventId, DateTime startDate);

    Task<Result> CancelEventArchiveTask(int eventId);
}
