# Architecture

## Overview

Orpheus is split into four main projects.

- Orpheus.Core
- Orpheus.Infrastructure
- Orpheus.Api
- Orpheus.Cli

The dependency direction points inward toward Core.

## Orpheus.Core

Core contains domain models and interfaces.

Responsibilities:

- persona model
- speech request model
- speech result model
- audio result model
- transformation interfaces
- TTS interfaces
- cache interfaces
- navigation event model later

Core must not depend on:

- Infrastructure
- Api
- Cli
- file system
- network
- concrete AI providers
- concrete TTS providers

## Orpheus.Infrastructure

Infrastructure contains implementations.

Initial responsibilities:

- stub persona transformer
- stub TTS provider
- in-memory persona repository
- built-in sample persona registration
- audio cache later

Future responsibilities:

- local LLM provider
- local TTS provider
- remote TTS provider
- provider configuration
- audio processing

## Orpheus.Api

API exposes HTTP endpoints for clients.

Initial endpoint:

- POST /speak

Future endpoints:

- GET /personas
- POST /transform
- POST /synthesize
- POST /navigation/instruction
- POST /story/continue

## Orpheus.Cli

CLI exposes local command-line access.

Initial command:

- speak a text with a selected persona and print transformed text plus audio URI

## Main Flow

1. Client sends text and persona id.
2. API or CLI creates a speech request.
3. Persona repository resolves the persona.
4. Persona transformer creates persona-specific text.
5. TTS provider creates or stubs audio output.
6. Audio cache stores or reuses generated audio.
7. Response returns transformed text and optional audio location.

## Core Interfaces

Initial interfaces:

- IPersonaRepository
- IPersonaTransformer
- ITextToSpeechProvider
- IAudioCache

## Safety Boundary

Voice models, samples, generated audio, proprietary assets, and secrets are runtime data.

They must not be committed to the repository.

The repository may provide text-only persona presets, including presets that name their inspirations. It must not provide protected audio, extracted assets, trained voice models, generated media, or secrets.
