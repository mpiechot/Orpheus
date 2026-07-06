# Implementation Backlog

This backlog turns the voice-runtime plan into issue-ready implementation slices. It is intentionally local Markdown for now; publish these to the issue tracker only after confirming the tracker labels and desired granularity.

## User Stories

- As a developer, I can understand the adapter boundary before adding provider runtime code.
- As a user, I can keep committed personas text-only while using private local persona overrides.
- As a user, I can preview and reroll a generated persona voice without regenerating it for every text request.
- As a user, I can supply my own local voice assets without putting assets, paths, secrets, generated audio, or models in Git.
- As a developer, I can integrate one real local provider through a process boundary with deterministic tests.

## Preparation Already Completed

- ADRs document local persona assets and persistent voice identities.
- The Infrastructure-to-Adapters rename is complete.
- `.orpheus/` is ignored for local runtime data.
- README links to the planning, backlog, provider evaluation, and legal-boundary documents.
- The legal/repository boundary and committed persona review checklist are documented.
- A local persona example JSON exists with placeholder paths only.

## Slice 1: Rename Infrastructure To Adapters

**Status**: Complete.

**User stories covered**: Developer adapter-boundary clarity.

### What to build

Rename the current Infrastructure project to Adapters so concrete implementations are clearly separated from Core abstractions before provider runtime work expands.

### Acceptance criteria

- [x] Project folder, project file, assembly name, namespaces, solution references, API references, CLI references, and tests use `Orpheus.Adapters`.
- [x] Existing stub transformer, stub TTS provider, and in-memory persona repository still work.
- [x] `dotnet build Orpheus.sln` passes.
- [x] `dotnet test Orpheus.sln` passes.
- [x] Architecture docs no longer describe the old name as the target state.

## Slice 2: Add Preview Text To Persona End-To-End

**Status**: Complete.

**User stories covered**: Voice preview setup, text-only committed personas.

### What to build

Add optional `previewText` to the Persona domain model and persona JSON shape so voice regeneration has a persona-owned fallback preview string.

### Acceptance criteria

- [x] Core `Persona` supports optional `previewText` without breaking existing callers.
- [x] Sample persona JSON files may include `previewText`.
- [x] In-memory sample persona definitions include representative original preview text where useful.
- [x] Tests cover creating a persona with and without `previewText`.
- [x] Committed preview text is original and does not quote protected dialogue, lyrics, scripts, or extracted game text.

## Slice 3: Load Local Persona Overrides From `.orpheus/personas`

**Status**: Complete.

**User stories covered**: Private local persona overrides.

### What to build

Add a file-based persona repository that loads committed personas and then overlays local/private personas from `.orpheus/personas` when the folder exists.

### Acceptance criteria

- [x] `samples/personas` can be loaded from JSON.
- [x] `.orpheus/personas` is loaded when present and ignored when absent.
- [x] A local persona with the same `id` overrides the committed sample at runtime.
- [x] Diagnostics can report which source supplied the active persona.
- [x] API and CLI `speak` use the resolved active persona.
- [x] Tests cover missing local folder, local-only persona, and local override behavior.

## Slice 4: Enforce Source-Aware Persona Validation

**Status**: Complete.

**User stories covered**: Repository safety boundary, private local assets.

### What to build

Validate committed and local persona sources differently. Committed personas remain text-only. Local personas may include runtime-only `voice.assets` metadata for provider adapters.

### Acceptance criteria

- [x] Committed persona files reject `voice.assets`, local paths, provider secrets, and runtime asset references.
- [x] Local persona files may include `voice.assets`.
- [x] Supported common asset fields include `speakerSample`, `referenceAudio`, `modelPath`, and `speakerEmbedding`.
- [x] Provider-specific values are accepted only under `providerSettings`.
- [x] Relative asset paths resolve relative to the local persona file.
- [x] Asset existence is not required at application startup.
- [x] Tests cover accepted local assets, rejected committed assets, and relative path resolution.

## Slice 5: Add Persona Runtime Metadata Resolution

**Status**: Complete.

**User stories covered**: Local assets without Core file/path dependencies.

### What to build

Keep local `voice.assets` and source data available to adapters without adding those fields to Core `Persona`.

### Acceptance criteria

- [x] Persona loading reads each persona file once and emits the Core `Persona` plus adapter-layer runtime metadata.
- [x] Core `Persona` does not include asset paths or provider-specific settings.
- [x] A provider adapter can resolve runtime metadata by persona id and/or voice id.
- [x] Tests prove `ITextToSpeechProvider` remains unchanged.
- [x] Missing runtime metadata for a real provider produces a clear configuration error when required.

## Slice 6: Store Last Original Text For Regeneration Preview

**Status**: Complete.

**User stories covered**: Reroll preview with the text that triggered dissatisfaction.

### What to build

Persist only the last original synthesis text per persona in ignored local runtime state and make the behavior configurable.

### Acceptance criteria

- [x] `speak` stores the last original input text per persona when enabled.
- [x] Transformed persona output is not stored for this feature.
- [x] Storage is under `.orpheus/state` or equivalent ignored runtime state.
- [x] A config switch can disable storing the last original text.
- [x] The implementation uses OS/user-bound protection where practical and documents fallback behavior.
- [x] Tests cover enabled, disabled, and overwritten-last-text behavior.

## Slice 7: Add Persistent Voice Identity Store With Stub Provider Support

**Blocked by**: Slice 5.

**User stories covered**: Stable persona voice identity, regenerate without real provider dependency.

### What to build

Introduce the voice identity lifecycle using deterministic local storage and wire the stub provider through it so the lifecycle is testable before choosing the real provider.

### Acceptance criteria

- [ ] Voice identities are stored under `.orpheus/voices` or equivalent ignored runtime storage.
- [ ] Supported states are `active`, `candidate`, and `rejected`.
- [ ] First `speak` can create or resolve an active voice identity.
- [ ] Voice identity fingerprints include persona id, provider, voice id, voice style, relevant provider settings, and local asset metadata.
- [ ] Stale active voices produce a warning without silent replacement.
- [ ] Tests cover first activation, candidate creation, accept, reject, cleanup, and stale detection.

## Slice 8: Add Voice Lifecycle CLI Commands

**Blocked by**: Slice 6, Slice 7.

**User stories covered**: Preview and reroll generated voices.

### What to build

Expose the minimal voice lifecycle through CLI commands.

### Acceptance criteria

- [ ] `voice status <personaId>` shows active/candidate/stale state and persona source.
- [ ] `voice regenerate <personaId> [--text "..."]` creates a candidate and preview audio.
- [ ] Preview text selection order is explicit text, last original text, persona `previewText`, global preview text, then clear error.
- [ ] CLI output shows the preview text source.
- [ ] `voice accept <personaId> <candidateId>` activates the candidate and cleans up old candidates.
- [ ] `voice reject <personaId> <candidateId>` rejects a candidate.
- [ ] Accepting a voice does not automatically regenerate the last spoken audio.
- [ ] CLI integration tests cover the happy path and missing-preview-text error.

## Slice 9: Isolate Audio Cache By Active Voice Identity

**Blocked by**: Slice 7.

**Partial progress**: The deterministic WAV provider already reuses equivalent generated files and keys them by provider, persona id, requested persona voice, voice style, and transformed text. Full active-voice identity isolation is still blocked by Slice 7.

**User stories covered**: Correct audio after voice changes.

### What to build

Ensure audio cache entries are keyed by the active voice identity version or fingerprint so changing voices never reuses stale audio.

### Acceptance criteria

- [ ] Cache key includes persona id, text, provider, voice id, output format, and active voice identity version or fingerprint.
- [ ] The same text with a new accepted voice produces a different cache key.
- [ ] Existing cache cleanup remains optional and is not required for correctness.
- [ ] Tests cover cache hit for same voice and cache miss after voice change.

## Slice 10: Add Process-Based Provider Adapter Harness

**Blocked by**: Slice 5, Slice 7, Slice 9.

**User stories covered**: Real provider integration through a safe process boundary.

### What to build

Add a provider process adapter that can invoke a configured local command, pass text/assets safely, validate output, and fail clearly. Use a deterministic test helper process before integrating a real TTS provider.

### Acceptance criteria

- [ ] Provider command, timeout, working directory, and output format are configured globally.
- [ ] Process arguments are built structurally, not by shell string concatenation.
- [ ] Required asset paths are checked at synthesis time.
- [ ] Non-zero exit codes, timeout, missing output, invalid output path, and unsupported assets produce clear errors.
- [ ] Tests use a deterministic local helper process and require no network.
- [ ] Core remains free of provider process and filesystem dependencies.

## Slice 11: Complete Primary Provider Evaluation

**Status**: Complete.

**User stories covered**: Choose one quality anchor for real V1 audio.

### What to build

Evaluate candidate local persona-capable TTS providers against the documented criteria and select one primary provider for V1 integration.

### Acceptance criteria

- [x] `docs/provider-evaluation.md` lists evaluated candidates.
- [x] Each candidate is evaluated against persona fit, local execution, WAV output, process-adapter compatibility, stable voice identity, local asset support, regeneration support, failure modes, timeout behavior, and setup complexity.
- [x] The recommendation names one V1 primary provider or explicitly blocks provider integration with reasons.
- [x] Rejected or deferred options include concrete reasons.
- [x] No provider assets, model files, samples, generated audio, or secrets are committed.

## Slice 12: Choose Software License Before Public Release

**Blocked by**: None - can start immediately.

**User stories covered**: Repository safety boundary, release readiness.

### What to build

Choose and add an explicit software license before public release or external distribution. This is an owner decision and should not be treated as legal advice from implementation agents.

### Acceptance criteria

- [ ] Repository root contains the selected license file.
- [ ] README links to the license.
- [ ] `docs/legal-boundary.md` links to the license.
- [ ] License choice is compatible with the intended distribution model.
- [ ] Legal review is performed if the project will be published or commercialized.

## Recommended Start Order

Slices 1 through 6 and Slice 11 are complete. Continue with Slice 7, then Slice 8 and Slice 9. Slice 12 can run in parallel. Do not start real provider integration before Slice 10 is complete.
