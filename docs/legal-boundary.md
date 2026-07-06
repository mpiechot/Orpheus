# Legal And Repository Boundary

This document records the repository boundary for Orpheus. It is project guidance, not legal advice. A qualified lawyer should review licensing, distribution, and jurisdiction-specific questions before public release or commercial use.

## Repository Scope

Orpheus provides generic engine infrastructure and text-only persona configuration.

The repository must not contain:

- copyrighted character voices
- celebrity voices
- trained voice models
- speaker embeddings
- voice samples
- generated voice identities
- generated audio
- extracted dialogue
- proprietary prompt dumps
- protected media assets
- provider secrets
- API keys

Persona presets may name inspirations from fictional works, games, movies, or other media as text-only context. They must not include protected audio, extracted assets, trained voice models, samples, generated media, secrets, or copied dialogue.

## User Runtime Assets

Users may configure their own local providers, voices, models, samples, embeddings, generated voice identities, and generated audio outside Git.

Those files are user-provided runtime assets. The user is responsible for having the necessary rights, permissions, and legal basis to use them.

Orpheus should provide the technical boundary:

- keep runtime assets in ignored local folders such as `.orpheus/`
- reject runtime asset references in committed persona sources
- allow optional `voice.assets` only in local/private persona sources
- avoid logging local asset paths, secrets, or stored user text unnecessarily
- keep generated voice identities and audio cache files out of Git

## Committed Persona Review Checklist

Before committing a persona file, verify:

- [ ] It contains only text configuration.
- [ ] It does not contain `voice.assets`.
- [ ] It does not contain local paths.
- [ ] It does not contain secrets, API keys, tokens, or provider credentials.
- [ ] It does not quote protected dialogue, lyrics, scripts, or extracted game text.
- [ ] `previewText`, if present, is original and generic.
- [ ] It does not include or reference voice samples, trained models, speaker embeddings, generated media, or protected assets.

## Local Persona Rules

Local/private personas may include runtime-only fields such as `voice.assets`.

Those files should live in ignored local folders, usually:

```text
.orpheus/personas/
```

Local personas may refer to absolute paths or paths relative to the local persona file. They must not be committed.

## License Decision

The repository currently needs an explicit software license decision before public release. A legal-boundary document does not replace a software license.

When a license is selected, add the license file at the repository root and link it from this document and the README.
