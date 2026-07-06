namespace Orpheus.Adapters.Personas;

public sealed class PersonaConfigurationException : InvalidOperationException
{
    public PersonaConfigurationException(string message)
        : base(message)
    {
    }
}
