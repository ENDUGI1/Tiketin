using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Tiketin.Tests.Integration;

public class TicketApiTests : IClassFixture<TiketinApiFactory>
{
    private readonly TiketinApiFactory _factory;

    private const string EmployeeA = "andini.puspitasari@tiketin.local";
    private const string EmployeeB = "fajar.ramadhan@tiketin.local";
    private const string Technician = "bagus.prasetyo@tiketin.local";

    public TicketApiTests(TiketinApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly object ValidTicket = new
    {
        title = "Laptop tidak menyala setelah update BIOS",
        description = "Layar tetap hitam walau lampu power hidup. Sudah coba lepas baterai.",
        categoryId = 1,
        priority = 3
    };

    [DockerRequiredFact]
    public async Task Creating_a_ticket_returns_201_and_records_a_created_event()
    {
        var client = await _factory.ClientForAsync(EmployeeA);

        var response = await client.PostAsJsonAsync("/api/v1/tickets", ValidTicket);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var ticket = body.GetProperty("data");
        ticket.GetProperty("ticketNumber").GetString()
            .Should().MatchRegex(@"^TKT-\d{6}-\d{4}$");
        ticket.GetProperty("status").GetInt32().Should().Be(1); // Open

        var ticketId = ticket.GetProperty("id").GetGuid();
        var events = JsonDocument.Parse(
                await client.GetStringAsync($"/api/v1/tickets/{ticketId}/events"))
            .RootElement.GetProperty("data");

        events.GetArrayLength().Should().Be(1);
        events[0].GetProperty("eventType").GetInt32().Should().Be(1); // Created
    }

    [DockerRequiredFact]
    public async Task An_employee_cannot_read_someone_elses_ticket()
    {
        var owner = await _factory.ClientForAsync(EmployeeA);
        var created = await owner.PostAsJsonAsync("/api/v1/tickets", ValidTicket);
        var ticketId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var otherEmployee = await _factory.ClientForAsync(EmployeeB);
        var response = await otherEmployee.GetAsync($"/api/v1/tickets/{ticketId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Staff can still read it.
        var technician = await _factory.ClientForAsync(Technician);
        (await technician.GetAsync($"/api/v1/tickets/{ticketId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DockerRequiredFact]
    public async Task An_illegal_status_transition_returns_400_problem_details()
    {
        var employee = await _factory.ClientForAsync(EmployeeA);
        var created = await employee.PostAsJsonAsync("/api/v1/tickets", ValidTicket);
        var ticketId = JsonDocument.Parse(await created.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var technician = await _factory.ClientForAsync(Technician);
        var response = await technician.PatchAsJsonAsync(
            $"/api/v1/tickets/{ticketId}/status", new { status = 4 }); // Open -> Closed

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("detail").GetString().Should().Contain("tidak diizinkan");
    }
}
