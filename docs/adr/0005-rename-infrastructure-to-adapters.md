# Rename Infrastructure To Adapters Before Provider Work

Status: Implemented.

`Orpheus.Infrastructure` is a conventional Clean Architecture name, but it is too vague for this repository. The project contains concrete adapters for Core interfaces: persona repositories, file loading, provider processes, audio caches, voice identity storage, protected local state, and deterministic stubs.

Before real provider and voice runtime work expands, the project was renamed to `Orpheus.Adapters`.

The existing stub persona transformer and stub TTS provider stay in the renamed project. They are deterministic development/test adapters, not proof that real V1 audio generation is complete.

**Consequences**

The rename should happen early, while the project is still small. The work must update the project file, solution references, namespaces, tests, docs, API references, and CLI references. Core remains independent and continues to expose only abstractions and domain models.
