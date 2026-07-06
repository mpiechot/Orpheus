using Orpheus.Api;
using Orpheus.Adapters.Personas;
using Orpheus.Adapters.Speech;
using Orpheus.Adapters.State;
using Orpheus.Adapters.Transformation;
using Orpheus.Adapters.Voice;
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
builder.Services.AddSingleton<IVoiceIdentityStore>(services => new FileVoiceIdentityStore(
    new FileVoiceIdentityStoreOptions(
        builder.Configuration.GetValue<string>("Orpheus:Voice:Directory")
            ?? PersonaRepositoryFactory.ResolveRuntimeDirectory("voices", contentRootPath),
        services.GetRequiredService<IPersonaRuntimeMetadataResolver>())));
builder.Services.AddSingleton<IPersonaTransformer, StubPersonaTransformer>();
builder.Services.AddSingleton<ITextToSpeechProvider>(services =>
{
    var speechProviderName = builder.Configuration.GetValue<string>("Orpheus:Speech:Provider")
        ?? TextToSpeechProviderFactory.DefaultProviderName;
    var audioOutputDirectory = builder.Configuration.GetValue<string>("Orpheus:Speech:OutputDirectory")
        ?? PersonaRepositoryFactory.ResolveRuntimeDirectory("audio", contentRootPath);
    var windowsSapiVoiceName = builder.Configuration.GetValue<string>("Orpheus:Speech:WindowsSapi:VoiceName");
    var windowsSapiTimeoutSeconds = builder.Configuration.GetValue("Orpheus:Speech:WindowsSapi:TimeoutSeconds", 30);
    var processOptions = CreateProcessOptions(builder.Configuration, audioOutputDirectory);

    return TextToSpeechProviderFactory.Create(
        new TextToSpeechProviderFactoryOptions(
            speechProviderName,
            audioOutputDirectory,
            windowsSapiVoiceName,
            windowsSapiTimeoutSeconds,
            processOptions,
            services.GetRequiredService<IVoiceIdentityStore>(),
            services.GetRequiredService<IPersonaRuntimeMetadataResolver>()));
});
builder.Services.AddSingleton<SpeechEngine>();

var app = builder.Build();

app.MapOrpheusEndpoints();

app.Run();

static ProcessTextToSpeechProviderOptions? CreateProcessOptions(IConfiguration configuration, string outputDirectory)
{
    var command = configuration.GetValue<string>("Orpheus:Speech:Process:Command");
    var arguments = configuration.GetSection("Orpheus:Speech:Process:Arguments").Get<string[]>();
    if (string.IsNullOrWhiteSpace(command) || arguments is null || arguments.Length == 0)
    {
        return null;
    }

    var providerName = configuration.GetValue<string>("Orpheus:Speech:Process:ProviderName")
        ?? "process";
    var outputFormat = configuration.GetValue<string>("Orpheus:Speech:Process:OutputFormat")
        ?? "wav";
    var contentType = configuration.GetValue<string>("Orpheus:Speech:Process:ContentType")
        ?? "audio/wav";
    var timeoutSeconds = configuration.GetValue("Orpheus:Speech:Process:TimeoutSeconds", 60);

    return new ProcessTextToSpeechProviderOptions(
        providerName,
        command,
        arguments,
        outputDirectory,
        outputFormat,
        contentType,
        configuration.GetValue<string>("Orpheus:Speech:Process:WorkingDirectory"),
        TimeSpan.FromSeconds(timeoutSeconds),
        configuration.GetValue("Orpheus:Speech:Process:RequireReferenceAudio", false),
        configuration.GetValue("Orpheus:Speech:Process:RequireSpeakerSample", false),
        configuration.GetValue("Orpheus:Speech:Process:RequireModelPath", false),
        configuration.GetValue("Orpheus:Speech:Process:RequireSpeakerEmbedding", false));
}

public partial class Program;
