using Piedrazul.Domain;

namespace Piedrazul.Application.Abstractions.Repositories;

public interface IPatientRepository
{
    Task<PatientProfile?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientProfile?> GetByExternalUserIdAsync(string externalUserId, CancellationToken cancellationToken = default);
    Task<PatientProfile?> GetByDocumentAsync(string documentNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientProfile>> SearchByPrefixAsync(string term, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientProfile>> SearchByTermAsync(string term, int take, CancellationToken cancellationToken = default);
    Task AddAsync(PatientProfile patient, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
