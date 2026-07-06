# Orpheus Agent Instructions

## Purpose

Orpheus is a local Persona Speech Engine.

The repository provides reusable infrastructure for transforming text into persona-specific speech and generating spoken audio through configured providers.

The project intentionally remains generic in implementation, provider wiring, and committed assets.

It must never contain:

- copyrighted character voices
- celebrity voices
- trained voice models
- voice samples
- generated audio
- API secrets
- proprietary assets

Persona presets may explicitly name the fictional characters, games, movies, or other works they are inspired by, as long as they remain text-only configuration. Those presets must not include protected audio, extracted assets, voice models, samples, or provider secrets.

Users are expected to configure their own local providers, voices, models, and runtime assets outside of Git.

---

## High-Level Architecture

The solution consists of four primary projects.

Orpheus.Core
    Domain models and interfaces.

Orpheus.Adapters
    Concrete adapters for providers, repositories, files, runtime state, and caches.

Orpheus.Api
    HTTP interface.

Orpheus.Cli
    Local command line interface.

Tests are separated into dedicated test projects.

---

## Architecture Rules

The Core project defines the domain.

Core must never depend on:

- Adapters
- Api
- Cli
- filesystem
- network
- external AI providers
- concrete TTS providers

Adapters depends on Core.

API depends on Core and Adapters.

CLI depends on Core and Adapters.

Dependencies always point towards the Core.

---

## Coding Principles

Prefer

- clean architecture
- dependency inversion
- immutable models where practical
- deterministic behavior
- small interfaces
- descriptive naming
- unit testing

Avoid

- static global state
- service locator
- hidden dependencies
- speculative abstractions
- unnecessary frameworks

---

## Repository Scope

The repository contains engine infrastructure and text-only persona configuration.

It does not ship voices.

It does not ship AI models.

It does not ship copyrighted audio, protected assets, trained voice models, or generated media.

Sample personas may be generic or explicitly inspired by named works and characters, but they must remain text-only.

Allowed examples include:

- wise-master
- calm-guide
- pirate-narrator
- sarcastic-ai
- portal-announcer

Do not include voice samples, extracted dialogue, proprietary prompt dumps, or audio that attempts to distribute a protected voice.

---

## Development Strategy

Always build the simplest deterministic implementation first.

External AI providers are introduced only after stable abstractions exist.

Never integrate multiple providers before the interfaces have stabilized.

Audio generation is a core feature of the engine. The first implementation may use a deterministic stub provider, but the architecture must treat synthesis as part of the main speech pipeline rather than an optional afterthought.

---

## Testing

Every new feature should include tests whenever practical.

Prefer deterministic tests.

Avoid network access inside unit tests.

---

## Documentation

Keep documentation synchronized with implementation.

When architecture changes, update:

- README.md
- architecture.md
- roadmap.md

within the same change.
