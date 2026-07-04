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

## V1 Scope

The first milestone is a deterministic local prototype.

V1 currently includes:

- .NET solution
- Orpheus.Core
- Orpheus.Infrastructure
- Orpheus.Api
- Orpheus.Cli
- xUnit tests
- sample neutral personas
- stub persona transformer
- stub TTS provider
- POST /speak endpoint
- CLI command for generating transformed text and an audio result

No real AI provider or real voice provider is required for V1, but the local stub pipeline should still return a deterministic audio result shape so clients can build against audio generation from the beginning.

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
- speech.style
- voice.provider
- voice.voiceId
- voice.style

Personas are text-only configuration. They may be generic, or they may explicitly describe the fictional character, game, movie, or other work they are inspired by. Committed personas must not include protected audio, extracted dialogue, proprietary assets, trained voice models, provider secrets, or generated audio.

Private personas should be placed outside Git or in ignored local folders.

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
