using Orpheus.Core.Models;

namespace Orpheus.Core.Abstractions;

public interface IPersonaRepository
{
    Task<Persona?> GetByIdAsync(string personaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Persona>> ListAsync(CancellationToken cancellationToken = default);
}
