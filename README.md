# Orpheus

Orpheus is a local persona speech engine.

It transforms plain text or structured events into persona-specific text and optionally into spoken audio.

The project provides generic infrastructure only. It does not ship copyrighted voices, celebrity voices, character voices, trained voice models, samples, generated audio, secrets, or proprietary assets.

## Goal

Orpheus provides reusable infrastructure for:

- persona-based text transformation
- text-to-speech provider abstraction
- audio generation
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

V1 should include:

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
- CLI command for transforming text

No real AI provider is required for V1.

## Example API

POST /speak

Request:

{
  "persona": "wise-master",
  "text": "In 500 meters, turn right."
}

Response:

{
  "persona": "wise-master",
  "text": "In 500 meters, turn right, you should.",
  "audioFile": null
}

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

Personas should be generic enough to be safely committed unless they are private user personas.

Private personas should be placed outside Git or in ignored local folders.

## Repository Safety Rules

The repository must not contain:

- real voice models
- voice samples
- generated audio
- copyrighted character presets
- celebrity voice presets
- API keys
- secrets
- local caches

These files belong in local-only folders ignored by Git.