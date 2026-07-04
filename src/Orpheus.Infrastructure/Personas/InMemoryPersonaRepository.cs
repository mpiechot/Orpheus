using Orpheus.Core.Abstractions;
using Orpheus.Core.Models;

namespace Orpheus.Infrastructure.Personas;

public sealed class InMemoryPersonaRepository : IPersonaRepository
{
    private readonly IReadOnlyDictionary<string, Persona> _personas;

    public InMemoryPersonaRepository(IEnumerable<Persona> personas)
    {
        _personas = personas.ToDictionary(persona => persona.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static InMemoryPersonaRepository CreateWithSamplePersonas()
    {
        return new InMemoryPersonaRepository(SamplePersonas.All);
    }

    public Task<Persona?> GetByIdAsync(string personaId, CancellationToken cancellationToken = default)
    {
        _personas.TryGetValue(personaId, out var persona);
        return Task.FromResult(persona);
    }

    public Task<IReadOnlyCollection<Persona>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Persona>>(_personas.Values.ToArray());
    }
}
