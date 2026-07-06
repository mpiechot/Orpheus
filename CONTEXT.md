# Orpheus

Orpheus is a local Persona Speech Engine. This glossary defines the project language for persona-based text transformation and local speech audio generation.

## Language

**Persona Speech Engine**:
The reusable engine that turns input text into persona-specific speech text and local speech audio. It is not a chatbot, navigation app, voice cloning repository, or character asset repository.
_Avoid_: Chatbot, voice cloning app, navigation app

**Persona**:
A text-only configuration that describes how speech should be phrased and which voice profile should be requested from a provider. A persona may name its inspiration, but it must not contain protected audio, extracted assets, trained voice models, generated media, or secrets.
_Avoid_: Voice model, character asset, audio preset

**Text Persona**:
The phrasing, vocabulary, structure, and tone Orpheus applies to input text before synthesis. It shapes what is said, but it is not a voice or audio model.
_Avoid_: Voice, voice model

**Voice Profile**:
The provider-facing description or configuration for how synthesized speech should sound, including voice identity, prosody, pacing, style, or locally configured voice assets. Voice assets belong outside the repository.
_Avoid_: Text persona, committed voice asset

**Preview Text**:
An optional text-only persona field used to preview a generated voice when no explicit preview text or last original synthesis text is available. Committed preview text must be original and must not quote protected dialogue, lyrics, scripts, or extracted game text.
_Avoid_: Dialogue quote, sample audio, extracted script text

**Runtime Voice Asset**:
A local voice, model, speaker embedding, sample, provider binary, or related resource used by a TTS provider at runtime. Runtime voice assets are never committed and are referenced through local configuration rather than committed persona files.
_Avoid_: Sample persona file, repository asset

**Local Persona**:
A private persona JSON file loaded from an ignored local folder such as `.orpheus/personas`. It may override a committed persona with the same ID and may include optional runtime-only voice asset references.
_Avoid_: Committed sample persona, provider config file

**Persona Source**:
The origin of a persona definition, such as a committed sample, built-in definition, or ignored local file. The source determines validation rules. Committed sources must remain text-only; local sources may reference runtime assets.
_Avoid_: Persona type, provider type

**Voice Assets**:
Optional local fields under a local persona's voice configuration that reference user-provided runtime assets such as a speaker sample, reference audio, model path, speaker embedding, or provider-specific settings. These are parsed by adapters but are not part of the Core Persona model.
_Avoid_: Core persona field, committed voice sample

**Persona Runtime Metadata**:
Adapter-layer data derived while loading a persona, such as source information and optional local voice assets. It is kept outside Core and is used by provider adapters during synthesis.
_Avoid_: Core domain model, duplicate persona

**Runtime Resource System**:
A possible future abstraction for managing multiple kinds of runtime resources that share lookup, caching, validation, versioning, or cleanup needs. It is not part of the V1 language for audio generation, where local personas, voice assets, voice identities, and audio cache are the preferred explicit concepts.
_Avoid_: ResourceManager, generic resource provider, service locator

**Persona Fit**:
The degree to which generated speech audio is recognizably aligned with the selected persona through both transformed text and voice characteristics. Persona fit is the core product quality bar for Orpheus.
_Avoid_: Generic TTS quality

**Speech Request**:
A request to produce persona-specific speech from input text for a selected persona.
_Avoid_: Chat message, prompt

**Task-Based Speech Pipeline**:
The internal C# async shape where speech operations expose `Task`-based methods and accept cancellation tokens. It allows Orpheus to await file I/O and provider process work without requiring a background job system.
_Avoid_: Background job workflow, fire-and-forget work

**Synchronous Speech Workflow**:
A request/response workflow where the client waits for Orpheus to return transformed text and an audio result. V1 uses this workflow even though the internal implementation is task-based.
_Avoid_: Job queue, polling workflow

**Audio Generation**:
The creation of an actual local speech audio file from persona-specific text through a configured TTS provider. For V1, audio generation is a required capability, not merely a placeholder result.
_Avoid_: Stub audio, fake audio

**Local TTS Provider**:
A provider that runs on the user's machine or local network and can synthesize speech audio without committing voices, models, samples, generated audio, or secrets to the repository.
_Avoid_: Remote AI service, committed voice model

**Persona-Capable TTS Provider**:
A local TTS provider whose voice selection, prosody, style, or conditioning controls are strong enough to support the intended personas. A provider that only produces generic speech audio is not sufficient for the V1 goal.
_Avoid_: Generic TTS provider, placeholder provider

**Primary TTS Provider**:
The single real persona-capable provider integrated for V1 as the quality anchor. Orpheus keeps provider interfaces stable enough that additional providers can be added later without changing Core domain concepts.
_Avoid_: Provider collection, plugin marketplace

**Provider Evaluation**:
A focused comparison that selects the V1 primary TTS provider against persona fit, local execution, audio-file output, process-adapter compatibility, and asset-boundary requirements. It must happen before committing to a real provider integration.
_Avoid_: Provider integration, library preference

**Provider Process Adapter**:
An adapter-layer implementation that invokes a local TTS provider as an external process and verifies the generated audio output file. It keeps provider runtimes, model dependencies, and GPU-specific setup outside the Orpheus process.
_Avoid_: In-process provider SDK, Core provider dependency

**Adapters Project**:
The concrete implementation project for Core interfaces, including files, providers, caches, runtime state, and local process integration. It was formerly named Infrastructure.
_Avoid_: Core, API, CLI

**Stub TTS Provider**:
A deterministic test or development provider that returns a predictable audio result shape without synthesizing real audio. It supports tests and interface design, but it does not satisfy the V1 audio generation capability by itself.
_Avoid_: Real TTS provider, V1 audio implementation

**Audio Output File**:
A generated runtime audio file, such as WAV or MP3, written outside version control to an ignored output or cache location.
_Avoid_: Committed audio, sample voice file

**Voice Identity**:
A stable local representation of the generated or resolved voice used for a persona. It may be backed by user-provided assets, provider metadata, a seed, an embedding, a generated profile, or another provider-specific artifact. It is reused across synthesis requests.
_Avoid_: Audio cache entry, one-off style prompt

**Voice Candidate**:
A newly generated voice identity created during regeneration and not yet accepted as the active voice.
_Avoid_: Active voice, audio preview

**Active Voice**:
The accepted voice identity currently used for normal synthesis for a persona.
_Avoid_: Candidate voice, stale voice

**Stale Voice**:
An active voice identity whose fingerprint no longer matches the current persona voice definition, provider settings, or asset metadata. V1 may keep using it with a warning rather than silently replacing it.
_Avoid_: Invalid voice, missing voice

**Voice Fingerprint**:
A comparison value derived from persona ID, provider, voice ID, voice style, relevant provider settings, and local asset metadata. V1 uses normalized paths, file sizes, and modified times for assets instead of hashing large files.
_Avoid_: Audio cache key, security hash

**Audio Cache**:
A runtime cache that reuses generated audio files for equivalent speech requests instead of synthesizing them again. The cache key must account for persona, text, voice, provider, relevant provider settings, and audio format.
_Avoid_: Source asset folder, committed output

**Last Original Synthesis Text**:
The most recent original input text used for a persona, stored locally so voice regeneration can preview the new voice with the same input that triggered the reroll. The transformed persona output is not stored for this purpose.
_Avoid_: Log history, transformed output cache

**V1 Audio Format**:
The default generated audio format for V1 is WAV. MP3 is a later export or compatibility format unless the provider evaluation gives a specific reason to choose otherwise.
_Avoid_: MP3-first pipeline, provider-specific default
