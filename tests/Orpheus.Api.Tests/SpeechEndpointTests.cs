using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Orpheus.Api;

namespace Orpheus.Api.Tests;

public class SpeechEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SpeechEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
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
        Assert.Equal("stub://wise-master-placeholder/speech", body.AudioFile);
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
}
