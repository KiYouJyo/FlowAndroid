# Contributing

Use feature branches and focused pull requests. Keep business logic in `src/FlowAndroid.Core` and WinUI-specific code in `src/FlowAndroid.App`. Add unit tests for device output parsing, package metadata, compatibility and failure states.

The supervised official-OTA sideload path is experimental, with fixed trust and confirmation gates. Do not add factory-image flashing, unlock/relock, wipe, arbitrary commands or other destructive operations without a reviewed threat model, rollback policy, real-device test matrix and explicit physical confirmation design. Do not commit binaries from Android SDK Platform-Tools or private MSIX signing material.

Maintain `README.md`, `README.en-US.md`, `README.ja-JP.md` and all three UI languages together.
