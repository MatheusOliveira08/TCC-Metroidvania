# CLAUDE.md - Chronicles of The Lost Word (Unity C#)

## Build and Test Commands

- **Run Unity Tests (EditMode):** `Unity.exe -runTests -batchmode -projectPath . -testPlatform EditMode -testResults Logs/editmode-results.xml`
- **Run Unity Tests (PlayMode):** `Unity.exe -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults Logs/playmode-results.xml`
- **Compile/Check Errors:** `dotnet build ChroniclesOfTheLostWord/` (or run your specific compiler/MSBuild toolchain for C# scripts)

## Code Style & Architecture Guidelines

- **Unity Architecture:** Use 2D Physics (`Rigidbody2D`, `BoxCollider2D`). Separate input tracking from physics calculation. Prefers `FixedUpdate()` for movement/physics.
- **Naming Conventions:** PascalCase for Classes, Enums, and Public Methods. camelCase for Private fields (use `_` prefix optional) and Variables.
- **TDD Enforcement:** Only keep tests for critical architecture (provenance generation and base ML-Agent APIs). Delete tests for simple features once the logic is verified.
- **Tone:** Senior Unity Software Engineer. Give concise code blocks with exact folder/file instructions. No fluff.
