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
- sample persona loading
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

- speak or transform a text with a selected persona

## Main Flow

1. Client sends text and persona id.
2. API or CLI creates a speech request.
3. Persona repository resolves the persona.
4. Persona transformer creates persona-specific text.
5. TTS provider optionally creates audio.
6. Audio cache stores or reuses generated audio.
7. Response returns transformed text and optional audio location.

## Core Interfaces

Initial interfaces:

- IPersonaRepository
- IPersonaTransformer
- ITextToSpeechProvider
- IAudioCache

## Safety Boundary

Voice models, samples, generated audio, and secrets are runtime data.

They must not be committed to the repository.

The repository should provide abstractions and documentation, not protected content.