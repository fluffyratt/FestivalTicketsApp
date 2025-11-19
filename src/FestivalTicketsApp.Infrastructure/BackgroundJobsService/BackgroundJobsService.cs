using FestivalTicketsApp.Application.EventService;
using FestivalTicketsApp.Shared;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace FestivalTicketsApp.Infrastructure.BackgroundJobsService;

public class BackgroundJobsService(IEventService eventService, IBackgroundJobClient backgroundWorker)
    : IBackgroundJobsService
{
    private readonly IEventService _eventService = eventService;
    private readonly IBackgroundJobClient _backgroundWorker = backgroundWorker;

    public Task ScheduleEventArchiveTask(int eventId, DateTime startDate)
    {
        TimeSpan archiveTimeOffset = startDate - DateTime.Now;
        _backgroundWorker
            .Schedule(() => _eventService
                .ArchiveEventAsync(eventId), archiveTimeOffset);

        return Task.CompletedTask;
    }

    public Task<Result> CancelEventArchiveTask(int eventId)
    {
        IMonitoringApi? backgroundMonitor = JobStorage.Current.GetMonitoringApi();
        int searchFrom = 0;
        const int searchOffset = 25;
        string jobToCancel = String.Empty;
        JobList<ProcessingJobDto> jobs;

        do
        {
            jobs = backgroundMonitor.ProcessingJobs(searchFrom, searchOffset);
            jobToCancel = backgroundMonitor
                .ProcessingJobs(searchFrom, searchOffset)
                .FirstOrDefault(x => x.Value.Job.Method.Name == nameof(eventService.ArchiveEventAsync)
                                  && (int)x.Value.Job.Args[0] == eventId)
                .Key ?? String.Empty;

            searchFrom += searchOffset;
        } while (String.IsNullOrEmpty(jobToCancel) && jobs.Count > 0);

        bool result = _backgroundWorker.Delete(jobToCancel);

        return result switch
        {
            true => Task.FromResult(Result.Success()),
            false => Task.FromResult(Result.Failure(BackgroundProcessingErrors.CantDeleteBackgroundTaskError))
        };
    }
}
