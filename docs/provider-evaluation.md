# Provider Evaluation

This document records the evaluation used to select the V1 primary TTS provider.

## Goal

Choose one persona-capable local TTS provider for V1. The chosen provider must generate real local audio files and support recognizable persona fit through voice characteristics in addition to Orpheus text transformation.

## Criteria

- Persona fit for the intended personas
- Local execution without committing provider assets to the repository
- WAV audio-file output by default, with room for MP3 later
- Compatibility with a process-based adapter
- Compatibility with local persona `voice.assets` and generated voice identities
- Ability to create or reuse a stable voice identity for a persona without regenerating a different voice on each request
- Support for explicit user-provided local assets when present
- Support for regenerating a candidate voice identity and accepting it later
- Clear failure modes and timeout behavior
- Acceptable setup complexity for a quality-focused V1

## Required Behavior

The V1 primary provider must not treat `voice.style` as a one-off prompt that can produce a different voice for every synthesis request. It must support one of these stable identity mechanisms:

- use the user's supplied local assets
- materialize a reusable voice profile, embedding, seed, or provider voice id
- persist provider-specific voice metadata in the ignored runtime voice folder

If `voice.assets` are present for a local persona, the provider must either use them or fail clearly. It must not silently ignore them.

## Candidates

To be filled during evaluation.

## Recommendation

To be filled after evaluating candidates against the criteria above.

## Rejected Or Deferred Options

To be filled during evaluation.

## Risks

To be filled during evaluation.
