# Integrate Primary Provider As External Process

The first real TTS integration will invoke the primary persona-capable provider as an external local process instead of embedding a provider SDK in the Orpheus runtime. This keeps provider-specific Python environments, GPU dependencies, model files, and version constraints outside Core and lets Orpheus act as a .NET orchestration layer that writes input, runs the provider command, verifies the generated audio file, and returns an audio result.

**Consequences**

The provider integration must define command configuration, timeout handling, process failure reporting, output-file validation, and path safety. In-process SDK adapters can be added later behind the same provider interface if a chosen provider becomes stable enough to justify tighter coupling.
