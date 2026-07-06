# Local Personas May Carry Runtime Voice Assets

Committed persona files remain text-only and must not contain local paths, samples, voice models, generated media, provider secrets, or protected assets. Local/private persona files loaded from ignored folders may include optional `voice.assets` data for user-provided runtime voice assets.

The default local persona folder is `.orpheus/personas`. Local personas with the same `id` as a committed sample persona override the committed persona at runtime. Additional local persona paths may later be supplied through `ORPHEUS_PERSONA_PATHS`.

`voice.assets` is not part of the Core `Persona` model. The persona loader reads a persona file once, creates the Core `Persona`, and stores source information plus optional local assets as adapter-layer runtime metadata. Provider adapters can resolve that metadata during synthesis without making Core depend on file paths or provider-specific asset structures.

V1 defines a small common `voice.assets` shape for frequent local asset fields such as `speakerSample`, `referenceAudio`, `modelPath`, and `speakerEmbedding`. Provider-specific data belongs under `providerSettings`.

**Consequences**

The repository keeps one persona concept while still allowing local users to wire their own assets. Validation must be source-aware: committed persona sources reject runtime asset fields and local paths; local persona sources may contain them. Asset file existence is checked when the persona is used for synthesis, not at application startup.
