public interface IReportingService {
    Task<EventsReport> GenerateReportByEventAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
}