# Vision

## What is Orpheus?

Orpheus is a reusable Persona Speech Engine.

Its purpose is to transform plain text into speech that matches a configurable persona.

The repository itself is completely generic.

It provides infrastructure.

It does not provide copyrighted content.

---

## Long-Term Goal

A client should only need to provide:

- text
- persona
- optional context

Orpheus decides how the output should look and sound, then asks the configured speech provider to synthesize or stub audio.

Example:

Input

Turn right in 500 meters.

Persona

Wise Master

Output

"In 500 meters, turn right you should."

The same request should produce an audio result through any configured TTS provider. In early local builds this may be a deterministic stub URI rather than a real media file.

---

## Future Applications

Navigation

Desktop assistants

Discord bots

NPC dialogue

Home Assistant

Interactive storytelling

Accessibility

Games

Virtual companions

---

## Design Philosophy

Navigation is only one client.

Games are only one client.

Desktop assistants are only one client.

The engine should remain reusable.

---

## Core Responsibilities

Transform text.

Generate speech audio.

Manage personas.

Manage providers.

Manage audio generation.

Nothing more.

---

## Non Goals

Orpheus is not:

- a navigation application
- a chatbot
- a voice cloning repository
- a collection of voice models

Those are integrations built on top of Orpheus.
