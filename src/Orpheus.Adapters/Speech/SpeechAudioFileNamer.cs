using System.Security.Cryptography;
using System.Text;
using Orpheus.Core.Models;

namespace Orpheus.Adapters.Speech;

internal static class SpeechAudioFileNamer
{
    public static string GetOutputPath(string outputDirectory, SpeechSynthesisRequest request, string provider, string extension)
    {
        var hashInput = string.Join(
            '\n',
            provider,
            request.Persona.Id,
            request.Persona.Voice.Provider,
            request.Persona.Voice.VoiceId,
            string.Join('|', request.Persona.Voice.Style),
            request.Text);

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashInput))).ToLowerInvariant();
        var fileName = $"{request.Persona.Id}-{hash[..16]}.{extension.TrimStart('.')}";

        return Path.Combine(outputDirectory, fileName);
    }
}
