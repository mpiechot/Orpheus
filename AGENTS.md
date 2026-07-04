# Orpheus Agent Instructions

## Purpose

Orpheus is a local Persona Speech Engine.

The repository provides reusable infrastructure for transforming text into persona-specific speech and optionally generating spoken audio.

The project intentionally remains generic.

It must never contain:

- copyrighted character voices
- celebrity voices
- trained voice models
- voice samples
- generated audio
- API secrets
- proprietary assets

Users are expected to configure their own local providers and assets outside of Git.

---

## High-Level Architecture

The solution consists of four primary projects.

Orpheus.Core
    Domain models and interfaces.

Orpheus.Infrastructure
    Implementations of providers and repositories.

Orpheus.Api
    HTTP interface.

Orpheus.Cli
    Local command line interface.

Tests are separated into dedicated test projects.

---

## Architecture Rules

The Core project defines the domain.

Core must never depend on:

- Infrastructure
- Api
- Cli
- filesystem
- network
- external AI providers
- concrete TTS providers

Infrastructure depends on Core.

API depends on Core and Infrastructure.

CLI depends on Core and Infrastructure.

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

The repository contains infrastructure only.

It does not ship voices.

It does not ship AI models.

It does not ship copyrighted personas.

Sample personas must remain generic.

Allowed examples include:

- wise-master
- calm-guide
- pirate-narrator
- sarcastic-ai

Avoid names of movies, games, actors or copyrighted characters.

---

## Development Strategy

Always build the simplest deterministic implementation first.

External AI providers are introduced only after stable abstractions exist.

Never integrate multiple providers before the interfaces have stabilized.

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