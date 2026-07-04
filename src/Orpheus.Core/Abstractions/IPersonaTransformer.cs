using Orpheus.Core.Models;

namespace Orpheus.Core.Abstractions;

public interface IPersonaTransformer
{
    Task<PersonaTextResult> TransformAsync(
        Persona persona,
        string text,
        CancellationToken cancellationToken = default);
}
