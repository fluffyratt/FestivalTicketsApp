using FestivalTicketsApp.Shared;

namespace FestivalTicketsApp.Infrastructure;

public static class BackgroundProcessingErrors
{
    public static readonly Error CantDeleteBackgroundTaskError = new Error(nameof(CantDeleteBackgroundTaskError));
}
