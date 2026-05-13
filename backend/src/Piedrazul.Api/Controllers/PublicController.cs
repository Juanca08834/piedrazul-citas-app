using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Piedrazul.Api.Configuration;
using Piedrazul.Application;

namespace Piedrazul.Api.Controllers;

[Route("api/public")]
public sealed class PublicController(IAppointmentService appointmentService, IOptions<CenterOptions> centerOptions) : ApiControllerBase
{
    private readonly IAppointmentService _appointmentService = appointmentService;
    private readonly CenterOptions _centerOptions = centerOptions.Value;

    [HttpGet("info")]
    public ActionResult<CenterInfoResponse> GetInfo()
    {
        return Ok(new CenterInfoResponse(
            _centerOptions.Name,
            _centerOptions.Tagline,
            _centerOptions.Address,
            _centerOptions.Phone,
            _centerOptions.AttentionHours,
            _centerOptions.About));
    }

    [HttpGet("providers")]
    public async Task<ActionResult<IReadOnlyList<ProviderSummaryResponse>>> GetProviders(CancellationToken cancellationToken)
    {
        var providers = await _appointmentService.GetActiveProvidersAsync(cancellationToken);
        return Ok(providers);
    }

    [HttpGet("providers/{providerId:guid}/availability")]
    public async Task<ActionResult<IReadOnlyList<AvailabilitySlotResponse>>> GetAvailability(Guid providerId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetAvailabilityAsync(providerId, date, cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }

    [HttpGet("patients/lookup")]
    public async Task<ActionResult<PatientPublicLookupResponse>> LookupPatient([FromQuery] string document, CancellationToken cancellationToken)
    {
        var patient = await _appointmentService.GetPatientByDocumentAsync(document, cancellationToken);
        if (patient is null)
            return Ok(new PatientPublicLookupResponse(false, null, null, null, null, null, null, null));

        return Ok(new PatientPublicLookupResponse(
            Exists: true,
            Id: patient.Id,
            FirstName: patient.FirstName,
            LastName: patient.LastName,
            Gender: patient.Gender,
            MaskedPhone: MaskPhone(patient.Phone),
            MaskedEmail: MaskEmail(patient.Email),
            BirthYear: patient.BirthDate?.Year));
    }

    private static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4) return null;
        return $"*** ***-{phone[^4..]}";
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var at = email.IndexOf('@');
        return at > 0 ? $"{email[0]}****{email[at..]}" : null;
    }

    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentResponse>> CreateAppointment([FromBody] PublicAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CreatePublicAppointmentAsync(request, null, "public-web", cancellationToken);
        return result.Succeeded && result.Data is not null
            ? Ok(result.Data)
            : FromFailure(result);
    }
}
