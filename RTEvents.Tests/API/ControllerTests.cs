using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using RTEvents.API.Controllers;
using RTEvents.API.DTOs;
using RTEvents.Tests.Support;

namespace RTEvents.Tests.API;

public class ControllerTests
{
    [Fact]
    public async Task Get_event_returns_200_and_mapped_body()
    {
        var service = new Mock<IEventsService>(MockBehavior.Strict);
        service.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(Samples.Event());
        var result = Assert.IsType<OkObjectResult>((await new EventsController(service.Object).GetEventByIdAsync(7)).Result);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(EventDTO.FromDomain(Samples.Event()), Assert.IsType<EventDTO>(result.Value));
        service.VerifyAll();
    }

    [Fact]
    public async Task Get_missing_event_returns_404()
    {
        var service = new Mock<IEventsService>();
        service.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((Event?)null);
        Assert.Equal(404, Assert.IsType<NotFoundResult>((await new EventsController(service.Object).GetEventByIdAsync(99)).Result).StatusCode);
    }

    [Fact]
    public async Task Create_forwards_request_and_returns_201_with_get_route()
    {
        var dto = new CreateEventRequestDTO("Concert", "An evening concert", new(2026, 10, 1), new(19, 30), "UTC", 2, 10);
        var service = new Mock<IEventsService>(MockBehavior.Strict);
        service.Setup(s => s.CreateAsync(dto.Name, dto.Description, dto.Date, dto.Time, dto.Timezone, dto.TicketCapacity, dto.VenueId)).ReturnsAsync(Samples.Event());
        var result = Assert.IsType<CreatedAtRouteResult>((await new EventsController(service.Object).CreateEventAsync(dto)).Result);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(nameof(EventsController.GetEventByIdAsync), result.RouteName);
        Assert.Equal(7, result.RouteValues!["id"]);
        Assert.Equal(EventDTO.FromDomain(Samples.Event()), result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task Update_forwards_supported_fields_and_returns_200()
    {
        var dto = new UpdateEventRequestDTO(7, "Concert", "An evening concert", new(2026, 10, 1), new(19, 30), "UTC", null, 10);
        var service = new Mock<IEventsService>(MockBehavior.Strict);
        service.Setup(s => s.UpdateAsync(7, dto.Name, dto.Description, dto.Date, dto.Time, dto.Timezone, null, dto.TicketCapacity)).ReturnsAsync(Samples.Event());
        var result = Assert.IsType<OkObjectResult>((await new EventsController(service.Object).UpdateEventAsync(dto)).Result);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(EventDTO.FromDomain(Samples.Event()), result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task Delete_forwards_id_and_returns_200()
    {
        var service = new Mock<IEventsService>(MockBehavior.Strict);
        service.Setup(s => s.DeleteAsync(7)).Returns(Task.CompletedTask);
        Assert.Equal(200, Assert.IsType<OkResult>(await new EventsController(service.Object).DeleteEventAsync(7)).StatusCode);
        service.Verify(s => s.DeleteAsync(7), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("request-1")]
    public async Task Purchase_forwards_body_and_header_and_maps_response(string? key)
    {
        var service = new Mock<ITicketsService>(MockBehavior.Strict);
        service.Setup(s => s.PurchaseTicketsAsync(2, 7, "token", key)).ReturnsAsync(new Purchase(25m, []));
        var result = Assert.IsType<OkObjectResult>((await new TicketsController(service.Object).PurchaseEventTicketAsync(new(2, 7, "token"), key)).Result);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(new PurchaseTicketsResponseDTO(25m, PurchaseStatus.Pending), result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task Availability_forwards_id_and_returns_mapped_counts()
    {
        var service = new Mock<ITicketsService>(MockBehavior.Strict);
        service.Setup(s => s.GetTicketAvailabilityAsync(7)).ReturnsAsync(new TicketAvailability(7, 5, 2, 3, 10));
        var result = Assert.IsType<OkObjectResult>((await new TicketsController(service.Object).GetEventTicketAvailabilityAsync(7)).Result);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(new TicketAvailabilityDTO(7, 5, 2, 3, 10), result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task Reports_forwards_pagination_and_maps_results()
    {
        var service = new Mock<IReportingService>(MockBehavior.Strict);
        service.Setup(s => s.GenerateReportByEventAsync(5, 20)).ReturnsAsync(new EventsReport { TotalSales = 12.5m, TotalTicketsSold = 2 });
        var result = Assert.IsType<OkObjectResult>((await new ReportsController(service.Object).GetEventsReportsAsync(5, 20)).Result);
        Assert.Equal(200, result.StatusCode);
        var dto = Assert.IsType<EventsReportDTO>(result.Value);
        Assert.Equal(12.5m, dto.TotalSales);
        Assert.Equal(2, dto.TotalTicketsSold);
        service.VerifyAll();
    }

    [Theory]
    [InlineData(true, MessageType.PaymentSucceeded)]
    [InlineData(false, MessageType.PaymentFailed)]
    public async Task Payment_response_creates_message_saves_and_returns_200(bool success, MessageType type)
    {
        using var db = new MockDatabase();
        Message? message = null;
        Mock.Get(db.Context.Object.Messages).Setup(s => s.Add(It.IsAny<Message>())).Callback<Message>(m => message = m);
        var before = DateTimeOffset.UtcNow;
        Assert.Equal(200, Assert.IsType<OkResult>(await new MessageBusController(db.Context.Object).CreatePaymentResponseAsync(new(42, success))).StatusCode);
        Assert.NotNull(message);
        Assert.Equal(type, message.Type);
        Assert.Equal(new PaymentResponseEvent(42, success), JsonSerializer.Deserialize<PaymentResponseEvent>(message.Payload));
        Assert.InRange(message.CreatedAt, before, DateTimeOffset.UtcNow);
        db.VerifySaved();
    }

    public static TheoryData<Exception?, int> Errors => new()
    {
        { new EventNotFoundException(7), 404 }, { new VenueNotFoundException(2), 404 }, { new PurchaseNotFoundException(3), 404 },
        { new EventOverCapacityException(99), 400 }, { new MissingIdempotencyKeyException("Purchase"), 400 },
        { new MismatchedIdempotencyKeyException("old", "new"), 400 }, { new InvalidOperationException("secret"), 500 }, { null, 500 }
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public void Errors_maps_exceptions_to_public_status_and_body(Exception? error, int status)
    {
        var context = new DefaultHttpContext();
        if (error != null) context.Features.Set<IExceptionHandlerPathFeature>(new ExceptionHandlerFeature { Error = error, Path = "/api/events" });
        var controller = new ErrorsController(NullLogger<ErrorsController>.Instance) { ControllerContext = new ControllerContext { HttpContext = context } };
        var result = controller.HandleError();
        if (status == 404) Assert.Equal(status, Assert.IsType<NotFoundResult>(result).StatusCode);
        else if (status == 400)
        {
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(status, bad.StatusCode);
            Assert.Equal(error!.Message, JsonSerializer.SerializeToElement(bad.Value).GetProperty("message").GetString());
        }
        else
        {
            var problem = Assert.IsType<ObjectResult>(result);
            Assert.Equal(status, problem.StatusCode);
            var details = Assert.IsType<ProblemDetails>(problem.Value);
            Assert.Equal(status, details.Status);
            Assert.Null(details.Detail);
            Assert.DoesNotContain("secret", details.Title!);
        }
    }
}
