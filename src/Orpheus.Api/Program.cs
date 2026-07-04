using Orpheus.Api;
using Orpheus.Core.Abstractions;
using Orpheus.Core.Services;
using Orpheus.Infrastructure.Personas;
using Orpheus.Infrastructure.Speech;
using Orpheus.Infrastructure.Transformation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPersonaRepository>(_ => InMemoryPersonaRepository.CreateWithSamplePersonas());
builder.Services.AddSingleton<IPersonaTransformer, StubPersonaTransformer>();
builder.Services.AddSingleton<ITextToSpeechProvider, StubTextToSpeechProvider>();
builder.Services.AddSingleton<SpeechEngine>();

var app = builder.Build();

app.MapOrpheusEndpoints();

app.Run();

public partial class Program;
