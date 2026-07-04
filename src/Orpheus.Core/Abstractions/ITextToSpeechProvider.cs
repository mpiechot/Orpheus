using Orpheus.Core.Models;

namespace Orpheus.Core.Abstractions;

public interface ITextToSpeechProvider
{
    Task<AudioResult> SynthesizeAsync(
        SpeechSynthesisRequest request,
        CancellationToken cancellationToken = default);
}
