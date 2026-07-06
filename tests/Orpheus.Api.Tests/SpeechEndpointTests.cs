using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Orpheus.Api;

namespace Orpheus.Api.Tests;

public class SpeechEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly string _audioOutputDirectory;
    private readonly string _voiceDirectory;

    public SpeechEndpointTests(WebApplicationFactory<Program> factory)
    {
        _audioOutputDirectory = Path.Combine(Path.GetTempPath(), "orpheus-api-tests", Guid.NewGuid().ToString("N"));
        _voiceDirectory = Path.Combine(Path.GetTempPath(), "orpheus-api-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_audioOutputDirectory);
        Directory.CreateDirectory(_voiceDirectory);

        _client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Orpheus:Speech:Provider"] = "deterministic-wav",
                        ["Orpheus:Speech:OutputDirectory"] = _audioOutputDirectory,
                        ["Orpheus:Voice:Directory"] = _voiceDirectory,
                        ["Orpheus:State:StoreLastOriginalText"] = "false"
                    });
                });
            })
            .CreateClient();
    }

    [Fact]
    public async Task Speak_endpoint_returns_transformed_text_and_audio_file()
    {
        var response = await _client.PostAsJsonAsync(
            "/speak",
            new SpeakRequest("wise-master", "In 500 meters, turn right."));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SpeakResponse>();
        Assert.NotNull(body);
        Assert.Equal("wise-master", body.Persona);
        Assert.Equal("In 500 meters, turn right, you should.", body.Text);
        var audioPath = new Uri(body.AudioFile).LocalPath;
        Assert.StartsWith(_audioOutputDirectory, audioPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(audioPath));
        Assert.True(new FileInfo(audioPath).Length > 44);
    }

    [Fact]
    public async Task Speak_endpoint_returns_not_found_for_unknown_persona()
    {
        var response = await _client.PostAsJsonAsync(
            "/speak",
            new SpeakRequest("missing-persona", "Hello."));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Persona 'missing-persona' was not found.", body?.Error);
    }

    [Fact]
    public async Task Speak_endpoint_returns_bad_request_for_empty_text()
    {
        var response = await _client.PostAsJsonAsync(
            "/speak",
            new SpeakRequest("wise-master", " "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Text is required.", body?.Error);
    }

    public void Dispose()
    {
        _client.Dispose();
        if (Directory.Exists(_audioOutputDirectory))
        {
            Directory.Delete(_audioOutputDirectory, recursive: true);
        }

        if (Directory.Exists(_voiceDirectory))
        {
            Directory.Delete(_voiceDirectory, recursive: true);
        }
    }
}
