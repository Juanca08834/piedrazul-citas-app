using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piedrazul.Api.Extensions;
using Piedrazul.Application;

namespace Piedrazul.Api.Controllers;

[Authorize(Policy = "InternalStaff")]
[Route("api/internal")]
public sealed class InternalController(IAppointmentService appointmentService) : ApiControllerBase
{
    private readonly IAppointmentService _appointmentService = appointmentService;

    [HttpGet("patients/search")]
    public async Task<ActionResult<IReadOnlyList<PatientLookupResponse>>> SearchPatients([FromQuery] string document, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.SearchPatientsAsync(document, cancellationToken);
        return Ok(result);
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<AppointmentListResponse>> GetAppointments([FromQuery] Guid providerId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetAppointmentsByProviderAndDateAsync(providerId, date, cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }

    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentResponse>> CreateInternalAppointment([FromBody] InternalCreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CreateInternalAppointmentAsync(request, User.GetDisplayName(), cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }

    [HttpGet("appointments/export/pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] Guid providerId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var bytes = await _appointmentService.ExportAppointmentsPdfAsync(providerId, date, cancellationToken);
        if (bytes.Length == 0)
        {
            return NotFound(new { errors = new[] { "No fue posible generar el PDF solicitado." } });
        }

        return File(bytes, "application/pdf", $"citas-{date:yyyyMMdd}.pdf");
    }

    [HttpPatch("appointments/{appointmentId:guid}/status")]
    public async Task<ActionResult<AppointmentResponse>> UpdateAppointmentStatus(Guid appointmentId, [FromBody] AppointmentStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.UpdateAppointmentStatusAsync(appointmentId, request.Status, cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }
}
