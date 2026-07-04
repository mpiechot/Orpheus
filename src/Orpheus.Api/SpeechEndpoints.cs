using Microsoft.AspNetCore.Http.HttpResults;
using Orpheus.Core.Exceptions;
using Orpheus.Core.Models;
using Orpheus.Core.Services;

namespace Orpheus.Api;

public static class SpeechEndpoints
{
    public static IEndpointRouteBuilder MapOrpheusEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/speak", HandleSpeakAsync);
        return app;
    }

    public static async Task<Results<Ok<SpeakResponse>, BadRequest<ErrorResponse>, NotFound<ErrorResponse>>> HandleSpeakAsync(
        SpeakRequest request,
        SpeechEngine speechEngine,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await speechEngine.SpeakAsync(
                new SpeechRequest(request.Persona ?? string.Empty, request.Text ?? string.Empty),
                cancellationToken);

            return TypedResults.Ok(new SpeakResponse(result.PersonaId, result.Text, result.Audio.Uri));
        }
        catch (SpeechRequestValidationException exception)
        {
            return TypedResults.BadRequest(new ErrorResponse(exception.Message));
        }
        catch (PersonaNotFoundException exception)
        {
            return TypedResults.NotFound(new ErrorResponse(exception.Message));
        }
    }
}

public sealed record SpeakRequest(string Persona, string Text);

public sealed record SpeakResponse(string Persona, string Text, string AudioFile);

public sealed record ErrorResponse(string Error);
