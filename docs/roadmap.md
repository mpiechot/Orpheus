# Roadmap

## M1 - Repository Skeleton

Goal: Create a clean .NET solution.

Status: Complete.

Deliverables:

- solution file
- Core project
- Adapters project
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

## M4 - Adapter Project Rename

Goal: Keep the concrete implementation project named Orpheus.Adapters before real provider work expands.

Status: Complete.

Deliverables:

- project rename
- namespace rename
- solution reference updates
- test reference updates
- documentation updates

## M5 - Persona Files

Goal: Load personas from local files.

Status: Complete for committed samples and `.orpheus/personas` overrides.

Deliverables:

- JSON persona format
- sample personas
- optional `previewText`
- persona repository
- validation
- `.orpheus/personas` local override folder
- optional `ORPHEUS_PERSONA_PATHS` later
- local persona source diagnostics
- strict committed-vs-local validation
- local persona example file
- tests

## M6 - Runtime Voice Metadata

Goal: Keep local voice assets outside Core while still making them available to provider adapters.

Status: Complete for persona loading and metadata resolution. Synth-time provider asset validation remains part of provider adapter work.

Deliverables:

- local-only `voice.assets` parsing
- common asset fields such as `speakerSample`, `referenceAudio`, `modelPath`, and `speakerEmbedding`
- provider-specific `providerSettings`
- runtime metadata store or resolver
- relative path resolution from the persona file location
- synth-time asset existence validation
- tests

## M7 - Voice Identity Lifecycle

Goal: Generate or resolve a stable voice identity once per persona and reuse it.

Status: In progress. Last-original-text state is complete; voice identity storage and lifecycle commands remain.

Deliverables:

- ignored `.orpheus/voices` runtime storage
- active/candidate/rejected lifecycle
- fingerprinting from persona, provider, voice, settings, and asset metadata
- stale voice warning behavior
- CLI voice status/regenerate/accept/reject commands
- preview text selection
- local last-original-text state
- tests

## M8 - Audio Cache

Goal: Avoid regenerating identical output while respecting active voice identity.

Status: Partial. Deterministic WAV output files are reused for equivalent provider/persona/text/voice-style requests. Active voice identity isolation remains blocked by M7.

Deliverables:

- cache key
- cache interface
- file-based cache implementation
- active voice identity version or fingerprint in cache key
- ignored cache directory
- tests

## M9 - Provider Process Adapter

Goal: Prepare real providers without coupling Core to them.

Deliverables:

- provider configuration model
- local process adapter
- command configuration
- failure handling
- timeout handling
- output-file validation
- path safety
- no shell string concatenation for asset arguments
- provider evaluation

## M10 - Legal and Repository Boundary

Goal: Keep the repository generic and free of protected runtime assets.

Deliverables:

- license decision
- legal-boundary document
- README safety rules
- committed persona review checklist
- runtime asset ignore rules

## M11 - Navigation Event Model

Goal: Add navigation-specific input without making Orpheus a navigation app.

Deliverables:

- navigation instruction model
- distance
- maneuver type
- priority
- interruption rules
- tests

## M12 - Story Mode Prototype

Goal: Allow a persona to speak between required instructions.

Deliverables:

- story context model
- interruptible narration
- navigation insertion rules
- silence rules
- tests

## M13 - Client Prototypes

Goal: Explore integrations.

Possible clients:

- Android prototype
- desktop test client
- Discord bot
- Home Assistant script
- game prototype
