using Orpheus.Core.Abstractions;
using Orpheus.Core.Exceptions;
using Orpheus.Core.Models;
using Orpheus.Core.Services;
using Orpheus.Infrastructure.Personas;
using Orpheus.Infrastructure.Speech;
using Orpheus.Infrastructure.Transformation;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: orpheus <persona-id> <text>");
    Environment.ExitCode = 1;
    return;
}

var personaId = args[0];
var text = string.Join(' ', args.Skip(1));

IPersonaRepository personaRepository = InMemoryPersonaRepository.CreateWithSamplePersonas();
IPersonaTransformer personaTransformer = new StubPersonaTransformer();
ITextToSpeechProvider textToSpeechProvider = new StubTextToSpeechProvider();
var speechEngine = new SpeechEngine(personaRepository, personaTransformer, textToSpeechProvider);

try
{
    var result = await speechEngine.SpeakAsync(new SpeechRequest(personaId, text));

    Console.WriteLine(result.Text);
    Console.WriteLine(result.Audio.Uri);
}
catch (SpeechRequestValidationException exception)
{
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 1;
}
catch (PersonaNotFoundException exception)
{
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 2;
}
