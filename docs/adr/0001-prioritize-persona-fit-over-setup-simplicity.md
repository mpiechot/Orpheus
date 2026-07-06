# Prioritize Persona Fit Over Setup Simplicity

Orpheus exists to create recognizable persona-specific speech, so V1 prioritizes a persona-capable local TTS provider over the simplest provider to install. Generic TTS that only turns text into speech may support tests or fallback flows, but it does not satisfy the V1 product goal unless the generated audio fits the selected persona through both text transformation and voice characteristics.

**Consequences**

Tickets for real audio generation must evaluate provider support for voice identity, prosody, pacing, style, or local voice assets before treating the integration as V1-complete. User-provided voices, models, samples, and generated audio remain local runtime assets outside version control.
