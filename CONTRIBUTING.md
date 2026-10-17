# Contributing

Use feature branches and focused pull requests. Keep business logic in `src/FlowAndroid.Core` and WinUI-specific code in `src/FlowAndroid.App`. Add unit tests for device output parsing, package metadata, compatibility and failure states.

Do not add destructive device commands until a reviewed threat model, verified firmware trust policy, real-device test matrix and explicit confirmation design are available. Do not commit binaries from Android SDK Platform-Tools or private MSIX signing material.

Maintain `README.md`, `README.en-US.md`, `README.ja-JP.md` and all three UI languages together.
