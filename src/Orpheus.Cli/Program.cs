using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Speech;
using Orpheus.Adapters.State;
using Orpheus.Adapters.Transformation;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Exceptions;
using Orpheus.Core.Models;
using Orpheus.Core.Services;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: orpheus <persona-id> <text>");
    Environment.ExitCode = 1;
    return;
}

var personaId = args[0];
var text = string.Join(' ', args.Skip(1));

var filePersonaRepository = PersonaRepositoryFactory.CreateDefault();
IPersonaRepository personaRepository = filePersonaRepository;
IPersonaTransformer personaTransformer = new StubPersonaTransformer();
ITextToSpeechProvider textToSpeechProvider = new StubTextToSpeechProvider();
ILastSpeechTextStore lastSpeechTextStore = new FileLastSpeechTextStore(
    new FileLastSpeechTextStoreOptions(PersonaRepositoryFactory.ResolveRuntimeDirectory("state")));
var speechEngine = new SpeechEngine(personaRepository, personaTransformer, textToSpeechProvider, lastSpeechTextStore);

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
