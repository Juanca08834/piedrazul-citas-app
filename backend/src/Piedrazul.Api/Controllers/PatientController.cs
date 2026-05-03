using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piedrazul.Api.Extensions;
using Piedrazul.Application;

namespace Piedrazul.Api.Controllers;

[Authorize]
[Route("api/patient")]
public sealed class PatientController(IPatientService patientService, IAppointmentService appointmentService) : ApiControllerBase
{
    private readonly IPatientService _patientService = patientService;
    private readonly IAppointmentService _appointmentService = appointmentService;

    [HttpGet("profile")]
    public async Task<ActionResult<PatientProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _patientService.GetMyProfileAsync(User.GetSubject(), cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<PatientProfileResponse>> UpsertProfile([FromBody] PatientProfileUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await _patientService.UpsertMyProfileAsync(User.GetSubject(), User.GetEmail(), request, cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> GetMyAppointments(CancellationToken cancellationToken)
    {
        var result = await _patientService.GetMyAppointmentsAsync(User.GetSubject(), cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }

    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentResponse>> CreateAuthenticatedAppointment([FromBody] PublicAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CreatePublicAppointmentAsync(request, User.GetSubject(), User.GetDisplayName(), cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }

    [HttpPatch("appointments/{appointmentId:guid}/cancel")]
    public async Task<ActionResult<AppointmentResponse>> CancelMyAppointment(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CancelPatientAppointmentAsync(appointmentId, User.GetSubject(), cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }
}

