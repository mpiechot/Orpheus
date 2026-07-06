# Codex First Task

Set up the initial Orpheus repository.

## Goal

Create a working .NET solution with a deterministic local speech and audio pipeline.

The first version must not use:

- external AI services
- real TTS
- voice cloning
- real voice models
- generated audio

## Required Projects

Create:

- src/Orpheus.Core
- src/Orpheus.Adapters
- src/Orpheus.Api
- src/Orpheus.Cli
- tests/Orpheus.Core.Tests
- tests/Orpheus.Api.Tests

## Required Core Models

Create simple models for:

- Persona
- PersonaSpeechProfile
- PersonaVoiceProfile
- SpeechRequest
- SpeechResult
- AudioResult
- PersonaTextResult
- SpeechSynthesisRequest

## Required Core Interfaces

Create:

- IPersonaRepository
- IPersonaTransformer
- ITextToSpeechProvider
- IAudioCache

## Required Adapters

Implement deterministic stubs:

- InMemoryPersonaRepository
- StubPersonaTransformer
- StubTextToSpeechProvider

The stub transformer should produce predictable output so tests can assert exact values.

For wise-master, the stub may transform:

Input:

In 500 meters, turn right.

Output:

In 500 meters, turn right, you should.

The stub TTS provider should return a deterministic placeholder AudioResult. It must not write generated audio files.

## Required API

Create POST /speak.

Example request:

{
  "persona": "wise-master",
  "text": "In 500 meters, turn right."
}

Example response:

{
  "persona": "wise-master",
  "text": "In 500 meters, turn right, you should.",
  "audioFile": "stub://wise-master-placeholder/speech"
}

## Required CLI

Create a simple CLI command that accepts:

- persona id
- text

It should print transformed text.

## Required Tests

Add tests for:

- core model creation
- deterministic transformation
- deterministic audio result creation
- API /speak happy path
- unknown persona handling
- empty text validation

## Constraints

Do not add real AI provider integration.

Do not add real TTS provider integration.

Do not add voice cloning.

Do not commit audio files.

Do not commit model files.

Do not commit protected audio, extracted assets, trained voice models, generated media, or secrets. Text-only persona presets may name their inspirations.

Use the current persona JSON structure with:

- speech.style
- voice.provider
- voice.voiceId
- voice.style
