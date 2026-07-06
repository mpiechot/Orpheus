using Orpheus.Api;
using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Speech;
using Orpheus.Adapters.State;
using Orpheus.Adapters.Transformation;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Services;

var builder = WebApplication.CreateBuilder(args);
var contentRootPath = builder.Environment.ContentRootPath;

builder.Services.AddSingleton(_ => PersonaRepositoryFactory.CreateDefault(contentRootPath));
builder.Services.AddSingleton<IPersonaRepository>(services => services.GetRequiredService<FilePersonaRepository>());
builder.Services.AddSingleton<IPersonaRuntimeMetadataResolver>(services => services.GetRequiredService<FilePersonaRepository>());
builder.Services.AddSingleton<ILastSpeechTextStore>(_ => new FileLastSpeechTextStore(
    new FileLastSpeechTextStoreOptions(
        PersonaRepositoryFactory.ResolveRuntimeDirectory("state", contentRootPath),
        builder.Configuration.GetValue("Orpheus:State:StoreLastOriginalText", true))));
builder.Services.AddSingleton<IPersonaTransformer, StubPersonaTransformer>();
builder.Services.AddSingleton<ITextToSpeechProvider>(_ =>
{
    var speechProviderName = builder.Configuration.GetValue<string>("Orpheus:Speech:Provider")
        ?? TextToSpeechProviderFactory.DefaultProviderName;
    var audioOutputDirectory = builder.Configuration.GetValue<string>("Orpheus:Speech:OutputDirectory")
        ?? PersonaRepositoryFactory.ResolveRuntimeDirectory("audio", contentRootPath);
    var windowsSapiVoiceName = builder.Configuration.GetValue<string>("Orpheus:Speech:WindowsSapi:VoiceName");
    var windowsSapiTimeoutSeconds = builder.Configuration.GetValue("Orpheus:Speech:WindowsSapi:TimeoutSeconds", 30);

    return TextToSpeechProviderFactory.Create(
        speechProviderName,
        audioOutputDirectory,
        windowsSapiVoiceName,
        windowsSapiTimeoutSeconds);
});
builder.Services.AddSingleton<SpeechEngine>();

var app = builder.Build();

app.MapOrpheusEndpoints();

app.Run();

public partial class Program;
