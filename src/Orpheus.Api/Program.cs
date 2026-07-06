using Orpheus.Api;
using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Speech;
using Orpheus.Adapters.State;
using Orpheus.Adapters.Transformation;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Services;

var builder = WebApplication.CreateBuilder(args);
var contentRootPath = builder.Environment.ContentRootPath;
var storeLastOriginalText = builder.Configuration.GetValue("Orpheus:State:StoreLastOriginalText", true);

builder.Services.AddSingleton(_ => PersonaRepositoryFactory.CreateDefault(contentRootPath));
builder.Services.AddSingleton<IPersonaRepository>(services => services.GetRequiredService<FilePersonaRepository>());
builder.Services.AddSingleton<IPersonaRuntimeMetadataResolver>(services => services.GetRequiredService<FilePersonaRepository>());
builder.Services.AddSingleton<ILastSpeechTextStore>(_ => new FileLastSpeechTextStore(
    new FileLastSpeechTextStoreOptions(
        PersonaRepositoryFactory.ResolveRuntimeDirectory("state", contentRootPath),
        storeLastOriginalText)));
builder.Services.AddSingleton<IPersonaTransformer, StubPersonaTransformer>();
builder.Services.AddSingleton<ITextToSpeechProvider, StubTextToSpeechProvider>();
builder.Services.AddSingleton<SpeechEngine>();

var app = builder.Build();

app.MapOrpheusEndpoints();

app.Run();

public partial class Program;
