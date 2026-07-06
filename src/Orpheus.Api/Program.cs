using Orpheus.Api;
using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Speech;
using Orpheus.Adapters.Transformation;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPersonaRepository>(_ => InMemoryPersonaRepository.CreateWithSamplePersonas());
builder.Services.AddSingleton<IPersonaTransformer, StubPersonaTransformer>();
builder.Services.AddSingleton<ITextToSpeechProvider, StubTextToSpeechProvider>();
builder.Services.AddSingleton<SpeechEngine>();

var app = builder.Build();

app.MapOrpheusEndpoints();

app.Run();

public partial class Program;
