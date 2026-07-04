namespace Orpheus.Core.Exceptions;

public sealed class PersonaNotFoundException : Exception
{
    public PersonaNotFoundException(string personaId)
        : base($"Persona '{personaId}' was not found.")
    {
        PersonaId = personaId;
    }

    public string PersonaId { get; }
}
