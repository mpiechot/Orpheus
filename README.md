# Orpheus

Orpheus is a local persona speech engine.

It transforms plain text or structured events into persona-specific text and generates spoken audio through configured providers.

The project provides generic engine infrastructure and text-only persona configuration. It does not ship copyrighted voices, celebrity voices, character voices, trained voice models, samples, generated audio, secrets, or proprietary assets.

## Goal

Orpheus provides reusable infrastructure for:

- persona-based text transformation
- text-to-speech provider abstraction
- audio generation as a first-class pipeline step
- audio caching
- API access
- CLI access
- future navigation and companion/story modes

## Non-goals

Orpheus is not:

- a navigation app
- a chatbot
- a voice cloning repository
- a repository for voice models or generated audio files
- a collection of copyrighted character presets

Users may configure their own local personas, voices, models, and providers outside the repository.

See [Legal and repository boundary](docs/legal-boundary.md) for the repository asset boundary and committed persona review checklist.

## V1 Scope

The first milestone is a deterministic local prototype.

V1 currently includes:

- .NET solution
- Orpheus.Core
- Orpheus.Adapters
- Orpheus.Api
- Orpheus.Cli
- xUnit tests
- sample neutral personas
- file-based persona loading from `samples/personas`
- local persona overrides from `.orpheus/personas`
- optional persona `previewText`
- local last-original-text state under `.orpheus/state`
- stub persona transformer
- stub TTS provider
- POST /speak endpoint
- CLI command for generating transformed text and an audio result

No real AI provider or real voice provider is required for V1, but the local stub pipeline should still return a deterministic audio result shape so clients can build against audio generation from the beginning.

The real-audio V1 path will add local/private persona loading, persistent local voice identities, and a process-based provider adapter without committing voices, models, samples, generated voice data, or generated audio.

Planning documents for that work:

- [Voice runtime implementation plan](docs/voice-runtime-implementation-plan.md)
- [Implementation backlog](docs/implementation-backlog.md)
- [Provider evaluation](docs/provider-evaluation.md)
- [Legal and repository boundary](docs/legal-boundary.md)

## Build and Test

Requirements:

- .NET 8 SDK or newer

Commands:

```powershell
dotnet build Orpheus.sln
dotnet test Orpheus.sln
```

## CLI Usage

```powershell
dotnet run --project src/Orpheus.Cli/Orpheus.Cli.csproj -- wise-master "In 500 meters, turn right."
```

Output:

```text
In 500 meters, turn right, you should.
stub://wise-master-placeholder/speech
```

## Example API

POST /speak

Request:

{
  "persona": "wise-master",
  "text": "In 500 meters, turn right."
}

Response:

```json
{
  "persona": "wise-master",
  "text": "In 500 meters, turn right, you should.",
  "audioFile": "stub://wise-master-placeholder/speech"
}
```

## Persona Format

A persona describes how text should be written and how speech should sound.

Example fields:

- id
- displayName
- description
- previewText
- speech.style
- voice.provider
- voice.voiceId
- voice.style

Personas are text-only configuration. They may be generic, or they may explicitly describe the fictional character, game, movie, or other work they are inspired by. Committed personas must not include protected audio, extracted dialogue, proprietary assets, trained voice models, provider secrets, or generated audio.

Committed `previewText` values must be original, generic text. They must not quote or closely reproduce protected dialogue, lyrics, scripts, or extracted game text.

Private personas should be placed outside Git or in ignored local folders. Local/private personas may add optional runtime voice asset references for the user's own machine, but committed personas must not.

Default local development path:

```text
.orpheus/personas/
```

Local personas with the same `id` as a committed sample persona override the committed sample at runtime.

Local-only voice assets may be placed under `voice.assets` in private persona files. Supported common fields are `speakerSample`, `referenceAudio`, `modelPath`, `speakerEmbedding`, and provider-specific values under `providerSettings`. Relative asset paths resolve relative to the local persona file. Asset existence is checked later by provider adapters, not during persona loading.

## Local State

`speak` stores the last original input text per persona under `.orpheus/state` by default so later voice regeneration can preview the text that triggered a reroll. The transformed persona output is not stored for this feature.

The API can disable this behavior with:

```json
{
  "Orpheus": {
    "State": {
      "StoreLastOriginalText": false
    }
  }
}
```

## Runtime Voice Data

Real providers must use a stable local voice identity per persona. If a local persona provides voice assets, those assets take precedence. If it does not, a capable provider may generate or materialize a voice identity once and reuse it for future synthesis requests.

Generated voice profiles, embeddings, reference audio, provider metadata, last-input state, and audio cache files are runtime data. They belong under `.orpheus/` or another ignored local runtime folder and must not be committed.

Voice regeneration should create a candidate voice identity first. Accepting the candidate makes it active and may clean up rejected or stale candidates. Audio cache keys must include the active voice identity version or fingerprint so old audio is not reused for a different active voice.

## Repository Safety Rules

The repository must not contain:

- real voice models
- voice samples
- generated audio
- protected media assets
- celebrity voice presets
- API keys
- secrets
- local caches

These files belong in local-only folders ignored by Git.
