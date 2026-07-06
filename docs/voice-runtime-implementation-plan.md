# Voice Runtime Implementation Plan

This plan captures the decisions from the voice-runtime design grill and turns them into small implementation slices.

For issue-ready vertical slices, see [Implementation Backlog](implementation-backlog.md).

## 1. Rename Infrastructure To Adapters

Status: Complete.

The project boundary is now named `Orpheus.Adapters`, with project file, namespaces, references, tests, and deterministic stubs updated.

## 2. Extend Persona Format

Goal: support preview and local override behavior without adding runtime assets to Core.

- Add optional `previewText` to Core `Persona`.
- Load committed persona JSON from `samples/personas`.
- Load local persona JSON from `.orpheus/personas` when present.
- Allow local personas to override committed personas by `id`.
- Report the active source for diagnostics.
- Add source-aware validation.

## 3. Parse Local Voice Assets Outside Core

Goal: keep local paths and provider-specific data out of Core.

- Add adapter-layer persona loading representation with source and runtime metadata.
- Parse local-only `voice.assets`.
- Support `speakerSample`, `referenceAudio`, `modelPath`, `speakerEmbedding`, and `providerSettings`.
- Resolve relative asset paths relative to the local persona file.
- Reject `voice.assets` in committed persona sources.

## 4. Add Provider Process Adapter Contract

Goal: prepare a real local TTS provider without embedding provider runtimes.

- Configure provider command, timeout, output format, and working directory globally.
- Validate required asset paths at synthesis time.
- Pass paths as process arguments, not by shell string concatenation.
- Validate provider exit code and output audio file.
- Fail clearly if a provider ignores unsupported assets.

## 5. Add Persistent Voice Identity Store

Goal: generate or resolve a persona voice once and reuse it.

- Store voice identities under ignored `.orpheus/voices`.
- Track `active`, `candidate`, and `rejected`.
- Fingerprint persona/provider/voice/style/settings/asset metadata.
- Warn when an active voice is stale.
- Include active voice identity version or fingerprint in audio cache keys.

## 6. Add Voice CLI Commands

Goal: make reroll workflows usable.

- `voice status <personaId>`
- `voice regenerate <personaId> [--text "..."]`
- `voice accept <personaId> <candidateId>`
- `voice reject <personaId> <candidateId>`

`speak` creates the first active voice automatically when needed. `voice regenerate` creates a candidate and preview audio. `voice accept` activates the candidate and cleans up old candidates, but does not automatically regenerate the last spoken audio.

## 7. Add Last Original Text State

Goal: preview rerolls with the input that triggered the reroll.

- Store only the last original synthesis text per persona.
- Do not store transformed persona output for this purpose.
- Enable by default with a config switch to disable.
- Protect with OS/user-bound storage where practical.
- Never commit, log, or expose this state as repository data.

## 8. Add Audio Cache Isolation

Goal: avoid reusing old audio for a changed voice.

- Include active voice identity version or fingerprint in the cache key.
- Keep cache cleanup as a separate optional follow-up.

## 9. Maintain Legal And Repository Boundaries

Goal: keep the repository generic and free of protected runtime assets.

- Keep the legal-boundary document synchronized with implementation.
- Add a software license decision before public release.
- Keep committed preview text original and generic.
- Do not attempt database validation of copyrighted quotes.
- Treat human review as the boundary for text that is too close to protected dialogue.
