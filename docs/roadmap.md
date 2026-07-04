# Roadmap

## M1 - Repository Skeleton

Goal: Create a clean .NET solution.

Status: Complete.

Deliverables:

- solution file
- Core project
- Infrastructure project
- API project
- CLI project
- test projects
- README
- AGENTS.md
- docs
- .gitignore
- sample personas

## M2 - Core Domain

Goal: Define the stable domain model.

Status: Complete for the deterministic V1 pipeline.

Deliverables:

- Persona
- SpeechRequest
- SpeechResult
- AudioResult
- PersonaTextResult
- SpeechSynthesisRequest
- core interfaces
- unit tests

## M3 - Stub Speech Pipeline

Goal: Make the full text and audio pipeline work without external services.

Status: Complete for API, CLI, and tests.

Deliverables:

- deterministic persona transformer
- deterministic stub TTS provider returning an audio result
- POST /speak endpoint
- CLI command
- tests

## M4 - Persona Files

Goal: Load personas from local files.

Deliverables:

- JSON persona format
- sample personas
- persona repository
- validation
- tests

## M5 - Audio Cache

Goal: Avoid regenerating identical output.

Deliverables:

- cache key
- cache interface
- file-based cache implementation
- ignored cache directory
- tests

## M6 - Provider Abstractions

Goal: Prepare real providers without coupling Core to them.

Deliverables:

- provider configuration model
- local provider interface
- remote provider interface
- failure handling
- timeout handling

## M7 - Navigation Event Model

Goal: Add navigation-specific input without making Orpheus a navigation app.

Deliverables:

- navigation instruction model
- distance
- maneuver type
- priority
- interruption rules
- tests

## M8 - Story Mode Prototype

Goal: Allow a persona to speak between required instructions.

Deliverables:

- story context model
- interruptible narration
- navigation insertion rules
- silence rules
- tests

## M9 - Client Prototypes

Goal: Explore integrations.

Possible clients:

- Android prototype
- desktop test client
- Discord bot
- Home Assistant script
- game prototype
