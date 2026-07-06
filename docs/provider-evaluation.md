# Provider Evaluation

This document records the evaluation used to select the V1 primary TTS provider.

Evaluation snapshot: 2026-07-06.

## Decision

Use **Chatterbox local** as the V1 primary persona-capable TTS provider, with the general default model set to **Chatterbox Multilingual V3** and an optional **Chatterbox-Turbo** mode for English low-latency workflows.

The Orpheus provider id should be `chatterbox-local`.

This decision is intentionally scoped:

- Chatterbox is the first real persona-quality provider.
- The existing `deterministic-wav` provider remains the default development/test provider.
- `windows-sapi` remains an experimental platform fallback, not the V1 quality target.
- V1 Chatterbox synthesis requires local user-supplied voice assets for persona fit.
- Provider-created voice generation from text-only personas is deferred.

## Goal

Choose one persona-capable local TTS provider for V1. The chosen provider must generate real local audio files and support recognizable persona fit through voice characteristics in addition to Orpheus text transformation.

## Criteria

- Persona fit for the intended personas
- Local execution without committing provider assets to the repository
- WAV audio-file output by default, with room for MP3 later
- Compatibility with a process-based adapter
- Compatibility with local persona `voice.assets` and generated voice identities
- Ability to create or reuse a stable voice identity for a persona without regenerating a different voice on each request
- Support for explicit user-provided local assets when present
- Support for regenerating a candidate voice identity and accepting it later
- Clear failure modes and timeout behavior
- Acceptable setup complexity for a quality-focused V1

## Required Behavior

The V1 primary provider must not treat `voice.style` as a one-off prompt that can produce a different voice for every synthesis request. It must support one of these stable identity mechanisms:

- use the user's supplied local assets
- materialize a reusable voice profile, embedding, seed, or provider voice id
- persist provider-specific voice metadata in the ignored runtime voice folder

If `voice.assets` are present for a local persona, the provider must either use them or fail clearly. It must not silently ignore them.

For `chatterbox-local`, V1 uses the first mechanism: user-supplied local assets. A local persona must provide `voice.assets.referenceAudio` or `voice.assets.speakerSample` for real persona synthesis. If neither is present, the provider must fail with a configuration error instead of falling back to Chatterbox's default voice, because the default voice does not satisfy persona fit.

## Recommendation

Implement `chatterbox-local` first through the process adapter.

Reasons:

- Persona fit: Chatterbox supports zero-shot voice cloning from an audio prompt, voice/style controls such as exaggeration and CFG, and a multilingual model positioned for improved speaker similarity and reduced hallucination.
- Local execution: the model can run from a local Python environment, and the model weights are available through Hugging Face under MIT.
- Output shape: official examples save generated tensors to `.wav`, which matches the Orpheus V1 audio-file contract.
- Process boundary: Chatterbox does not require Core changes. Orpheus can call a small local Python wrapper with structured arguments and validate the resulting WAV file.
- Licensing posture: Chatterbox's repository and model card list MIT, which is simpler for Orpheus than F5-TTS's non-commercial pretrained-model license or XTTS-v2's custom model license. This is not legal advice.
- Current fit: it is a strong quality anchor for persona speech without making Orpheus a voice-model repository.

Expected wrapper contract:

```text
python synthesize_chatterbox.py
  --text-file <input.txt>
  --output <output.wav>
  --reference-audio <local-reference.wav>
  --model multilingual-v3|turbo|english
  --language-id <optional-language-code>
  --exaggeration <optional-number>
  --cfg-weight <optional-number>
  --seed <optional-number>
```

The wrapper should write only to the requested output path and return a non-zero exit code on unsupported assets, invalid input, provider errors, timeout-triggered cancellation, or missing model dependencies.

## Chatterbox Integration Rules

- Require a local reference asset for persona-quality synthesis.
- Accept `voice.assets.referenceAudio` first, then `voice.assets.speakerSample`.
- Store only metadata under `.orpheus/voices`, not copied reference audio.
- Fingerprint the active voice identity from persona id, provider id, model id, model version if known, language id, reference asset metadata, `voice.style`, and provider settings.
- Include the active voice identity fingerprint in cache keys.
- Treat `voice regenerate` as candidate metadata and preview generation around the same local asset unless the user supplies a different reference asset.
- Never synthesize with an arbitrary default voice when a persona expects a custom identity.
- Keep Chatterbox model files, reference clips, generated output, and dependency caches outside Git.

## Candidate Summary

| Candidate | Decision | Why |
| --- | --- | --- |
| Chatterbox Multilingual V3 / Turbo | Primary V1 provider | Best current balance of local voice cloning, persona fit, permissive license posture, WAV output, and process-adapter compatibility. Requires a local reference asset for stable persona identity. |
| Coqui XTTS-v2 | Backup / deferred | Strong voice cloning and CLI/API support, but the model uses the Coqui Public Model License and is a less clean V1 licensing choice. |
| F5-TTS | Deferred | Active, high-quality, process-friendly CLI and strong zero-shot behavior, but pretrained models are CC-BY-NC, which is too restrictive for the first recommended provider. |
| OpenVoice V2 | Deferred | MIT and strong cloning research, but it is explicitly a technology rather than a polished product, relies on a base speaker model for accent/emotion, and has a more complex adapter surface. |
| GPT-SoVITS | Deferred | Powerful few-shot/zero-shot workflow, but too heavy and WebUI/training-oriented for the first provider adapter. Better as an advanced local-provider option later. |
| Kokoro | Deferred fallback | Fast, Apache-licensed, local, and small, but not a persona cloning provider. Useful for generic local TTS fallback, not the V1 quality target. |
| Piper / piper1-gpl | Deferred fallback | Fast and local with stable voices, but fixed-voice TTS does not satisfy persona-specific voice identity unless users train or supply models. The current maintained fork is GPL-3.0. |
| Bark | Rejected for V1 | Creative and expressive, but it is a fully generative text-to-audio model that can deviate from the script, which conflicts with deterministic speech output. |
| Windows SAPI | Existing fallback only | Useful for local Windows smoke testing, but voice quality and persona fit depend on installed system voices and do not satisfy V1. |
| Remote APIs | Out of scope for V1 | Remote providers can be useful later, but V1 requires local execution and no provider secrets in the repository. |

## Evaluation Details

### Chatterbox

Sources:

- GitHub: <https://github.com/resemble-ai/chatterbox>
- Hugging Face model card: <https://huggingface.co/ResembleAI/chatterbox>

Fit:

- Supports voice cloning through `audio_prompt_path`.
- Chatterbox Multilingual V3 is documented as supporting 23+ languages, improved speaker similarity, reduced hallucination, and more natural multilingual speech.
- Chatterbox-Turbo is documented as a lower-compute English option with paralinguistic tags.
- Output examples save WAV files through `torchaudio`.
- Repository and Hugging Face model card list MIT.
- Outputs are watermarked, which is acceptable and should be documented to users.

Concerns:

- It is Python/PyTorch based and may need CUDA/MPS for acceptable latency.
- The GitHub README says the project was developed and tested on Python 3.11 and Debian 11; Windows should be treated as a configurable-process or WSL/Docker scenario until validated.
- It has Python APIs rather than a stable official CLI, so Orpheus should own a thin wrapper script outside Core.
- Persona stability depends on reference clip quality and consistent provider settings.

Decision: choose for V1.

### Coqui XTTS-v2

Sources:

- Docs: <https://docs.coqui.ai/en/latest/models/xtts.html>
- GitHub: <https://github.com/coqui-ai/TTS>

Fit:

- Supports voice cloning, cross-language voice cloning, multilingual generation, and WAV file output.
- Supports `speaker_wav` with single or multiple references.
- Provides command-line and Python API entry points.
- Process adapter compatibility is good.

Concerns:

- XTTS-v2 uses the Coqui Public Model License, not a standard permissive model license.
- The setup and ecosystem are older than newer options.

Decision: keep as backup if Chatterbox quality, setup, or runtime fails.

### F5-TTS

Source: <https://github.com/SWivid/F5-TTS>

Fit:

- Active project with recent releases.
- Provides a CLI using `--ref_audio`, `--ref_text`, and `--gen_text`.
- Strong quality and zero-shot behavior.
- Process adapter compatibility is strong.

Concerns:

- Code is MIT, but pretrained models are CC-BY-NC.
- That makes it unsuitable as the first recommended provider for a reusable engine that may be published or used commercially.

Decision: defer. Consider only for explicitly non-commercial local setups.

### OpenVoice V2

Source: <https://github.com/myshell-ai/OpenVoice>

Fit:

- MIT licensed.
- Supports instant voice cloning and style controls.
- V2 natively supports English, Spanish, French, Chinese, Japanese, and Korean.

Concerns:

- The project documentation describes it as a technology, not a product.
- It clones tone color, while accent and emotion come from the base speaker model.
- The integration would need more provider-specific orchestration than Chatterbox.

Decision: defer until the process adapter and voice identity lifecycle are stable.

### GPT-SoVITS

Source: <https://github.com/RVC-Boss/GPT-SoVITS>

Fit:

- Supports zero-shot TTS from a short vocal sample and few-shot fine-tuning from about one minute of data.
- Strong option for users willing to manage heavier voice workflows.

Concerns:

- WebUI/training workflow is larger than the first Orpheus provider adapter should absorb.
- More moving parts mean more failure modes and setup burden.

Decision: defer as an advanced-provider candidate.

### Kokoro

Source: <https://github.com/hexgrad/kokoro>

Fit:

- Small, fast, local model.
- Apache-licensed weights.
- Useful generic fallback candidate.

Concerns:

- It is not primarily a voice-cloning/persona-identity provider.
- Persona fit would mostly come from selecting a fixed voice, not matching user-supplied persona assets.

Decision: defer as a generic fallback, not the primary provider.

### Piper / piper1-gpl

Sources:

- Current fork: <https://github.com/OHF-Voice/piper1-gpl>
- Archived original: <https://github.com/rhasspy/piper>

Fit:

- Fast local neural TTS.
- Good deterministic process-adapter candidate.
- Useful for generic fallback and accessibility-style use cases.

Concerns:

- The original repository is archived and points to the Open Home Foundation fork.
- The maintained fork is GPL-3.0.
- Fixed voices are not enough for persona-specific voice identity unless users train or provide a matching model.

Decision: defer as fallback.

### Bark

Source: <https://github.com/suno-ai/bark>

Fit:

- Expressive and can produce nonverbal audio.
- MIT-licensed repository.

Concerns:

- It is documented as a fully generative text-to-audio model that can deviate from prompts.
- Orpheus needs reliable spoken output for caller-provided text.

Decision: reject for V1.

## Risks

- Legal/licensing: Chatterbox is MIT according to its repository and model card, but legal review is still needed before public release or commercial use.
- Misuse: Any voice-cloning-capable provider can be misused. Orpheus must keep runtime assets local, never ship voices, and clearly document user responsibility.
- Runtime stability: Chatterbox may need pinned Python, PyTorch, model, and CUDA versions. The process adapter must expose timeout and stderr diagnostics.
- Windows support: Direct Windows setup is not assumed. The provider command should support Python, WSL, or Docker without changing Core.
- Voice consistency: Generated audio may vary if provider settings or seeds are not fixed. Orpheus must persist provider settings in voice identity metadata.
- Cache correctness: Audio cache keys must include active voice identity metadata before Chatterbox is enabled for normal `speak`.
- Output disclosure: Chatterbox-generated audio is watermarked; user-facing docs should mention this once real provider setup instructions are added.

## Next Steps

1. Add a local Chatterbox wrapper script and setup documentation for the generic process adapter.
2. Validate Chatterbox setup on the target local environment before declaring the provider production-ready.
3. Keep `deterministic-wav` as the default provider until a user explicitly configures `process` with `ProviderName` set to `chatterbox-local`.
