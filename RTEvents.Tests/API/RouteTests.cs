using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RTEvents.API.Controllers;
using RTEvents.API.DTOs;
using RTEvents.Tests.Support;

namespace RTEvents.Tests.API;

// A minimal MVC host tests routing, binding and validation with mocked dependencies.
// It deliberately does not execute Program.cs, migrations or the background worker.
public class RouteTests
{
    [Fact]
    public async Task Event_routes_bind_requests_and_return_expected_statuses_and_location()
    {
        await using var host = await ApiHost.Create();
        var e = Samples.Event();
        host.Events.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(e);
        host.Events.Setup(s => s.CreateAsync("Concert", "An evening concert", e.Date, e.Time, "UTC", 10, 2)).ReturnsAsync(e);
        host.Events.Setup(s => s.UpdateAsync(7, null, null, null, null, null, null, null)).ReturnsAsync(e);
        host.Events.Setup(s => s.DeleteAsync(7)).Returns(Task.CompletedTask);

        var get = await host.Client.GetAsync("/api/events/7");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(EventDTO.FromDomain(e), await get.Content.ReadFromJsonAsync<EventDTO>());
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync("/api/events/99")).StatusCode);
        var create = await host.Client.PostAsJsonAsync("/api/events", new CreateEventRequestDTO(e.Name, e.Description, e.Date, e.Time, e.Timezone, e.VenueId, e.TicketCapacity));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.EndsWith("/api/Events/7", create.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EventDTO.FromDomain(e), await create.Content.ReadFromJsonAsync<EventDTO>());
        var patch = await host.Client.PatchAsJsonAsync("/api/events", new UpdateEventRequestDTO(7, null, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.DeleteAsync("/api/events/7")).StatusCode);
        host.Events.VerifyAll();
    }

    [Fact]
    public async Task Ticket_routes_bind_idempotency_header_and_query()
    {
        await using var host = await ApiHost.Create();
        host.Tickets.Setup(s => s.PurchaseTicketsAsync(2, 7, "token", "request-1")).ReturnsAsync(new Purchase(25m, []));
        host.Tickets.Setup(s => s.GetTicketAvailabilityAsync(7)).ReturnsAsync(new TicketAvailability(7, 5, 2, 3, 10));
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/tickets/purchase") { Content = JsonContent.Create(new PurchaseTicketRequestDTO(2, 7, "token")) };
        request.Headers.Add("Idempotency-Key", "request-1");
        var purchase = await host.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, purchase.StatusCode);
        Assert.Equal(new PurchaseTicketsResponseDTO(25m, PurchaseStatus.Pending), await purchase.Content.ReadFromJsonAsync<PurchaseTicketsResponseDTO>());
        var availability = await host.Client.GetAsync("/api/tickets/availability?eventId=7");
        Assert.Equal(HttpStatusCode.OK, availability.StatusCode);
        Assert.Equal(new TicketAvailabilityDTO(7, 5, 2, 3, 10), await availability.Content.ReadFromJsonAsync<TicketAvailabilityDTO>());
        host.Tickets.VerifyAll();
    }

    [Fact]
    public async Task Reports_and_message_bus_routes_are_reachable()
    {
        await using var host = await ApiHost.Create();
        host.Reports.Setup(s => s.GenerateReportByEventAsync(5, 20)).ReturnsAsync(new EventsReport { TotalSales = 12m });
        var reports = await host.Client.GetAsync("/api/reports?skip=5&take=20");
        Assert.Equal(HttpStatusCode.OK, reports.StatusCode);
        Assert.Equal(12m, (await reports.Content.ReadFromJsonAsync<EventsReportDTO>())!.TotalSales);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsJsonAsync("/api/messagebus", new CreatePaymentResponseRequestDTO(42, true))).StatusCode);
        host.Database.VerifySaved();
        host.Reports.VerifyAll();
    }

    [Theory]
    [InlineData("POST", "/api/events", "{}")]
    [InlineData("POST", "/api/events", "{\"name\":\"ab\",\"description\":\"short\",\"timezone\":\"UT\",\"venueId\":0,\"ticketCapacity\":0}")]
    [InlineData("PATCH", "/api/events", "{\"id\":0}")]
    [InlineData("PATCH", "/api/events", "{\"id\":7,\"name\":\"ab\",\"description\":\"short\",\"timezone\":\"UT\",\"venueId\":\"1\",\"ticketCapacity\":0}")]
    [InlineData("PUT", "/api/tickets/purchase", "{\"quantity\":0,\"eventId\":0,\"paymentDetails\":\"\"}")]
    [InlineData("POST", "/api/messagebus", "{\"paymentId\":0,\"success\":true}")]
    [InlineData("POST", "/api/events", "invalid-json")]
    public async Task Invalid_request_returns_400_before_calling_dependencies(string method, string path, string json)
    {
        await using var host = await ApiHost.Create();
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        var response = await host.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        host.Events.VerifyNoOtherCalls();
        host.Tickets.VerifyNoOtherCalls();
        host.Database.VerifySaved(0);
    }

    [Theory]
    [InlineData("event", 404)]
    [InlineData("venue", 404)]
    [InlineData("purchase", 404)]
    [InlineData("capacity", 400)]
    [InlineData("key", 400)]
    [InlineData("mismatch", 400)]
    [InlineData("unexpected", 500)]
    public async Task Service_errors_are_handled_by_error_route(string kind, int status)
    {
        await using var host = await ApiHost.Create();
        Exception error = kind switch
        {
            "event" => new EventNotFoundException(7), "venue" => new VenueNotFoundException(2),
            "purchase" => new PurchaseNotFoundException(3), "capacity" => new EventOverCapacityException(99),
            "key" => new MissingIdempotencyKeyException("Purchase"), "mismatch" => new MismatchedIdempotencyKeyException("old", "new"),
            _ => new InvalidOperationException("private failure details")
        };
        host.Events.Setup(s => s.GetByIdAsync(7)).ThrowsAsync(error);
        var response = await host.Client.GetAsync("/api/events/7");
        Assert.Equal(status, (int)response.StatusCode);
        if (status == 400) Assert.Contains(error.Message, await response.Content.ReadAsStringAsync());
        if (status == 500) Assert.DoesNotContain(error.Message, await response.Content.ReadAsStringAsync());
    }

    private sealed class ApiHost(WebApplication app, HttpClient client, Mock<IEventsService> events,
        Mock<ITicketsService> tickets, Mock<IReportingService> reports, MockDatabase database) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;
        public Mock<IEventsService> Events { get; } = events;
        public Mock<ITicketsService> Tickets { get; } = tickets;
        public Mock<IReportingService> Reports { get; } = reports;
        public MockDatabase Database { get; } = database;

        public static async Task<ApiHost> Create()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            var events = new Mock<IEventsService>();
            var tickets = new Mock<ITicketsService>();
            var reports = new Mock<IReportingService>();
            var database = new MockDatabase();
            builder.Services.AddSingleton(events.Object);
            builder.Services.AddSingleton(tickets.Object);
            builder.Services.AddSingleton(reports.Object);
            builder.Services.AddSingleton(database.Context.Object);
            builder.Services.AddControllers().AddApplicationPart(typeof(EventsController).Assembly);
            var app = builder.Build();
            app.UseExceptionHandler("/errors");
            app.MapControllers();
            await app.StartAsync();
            return new ApiHost(app, app.GetTestClient(), events, tickets, reports, database);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
            Database.Dispose();
        }
    }
}
