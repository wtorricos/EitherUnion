# Copilot instructions for WTorricos.Either

## Build and test

- Restore/build the solution: `dotnet build WTorricos.Either.slnx`
- Run all tests: `dotnet test tests\WTorricos.Either.UnitTests\WTorricos.Either.UnitTests.csproj`
- Run a single test class or method: `dotnet test tests\WTorricos.Either.UnitTests\WTorricos.Either.UnitTests.csproj --filter FullyQualifiedName~WTorricos.Either.UnitTests.IEitherCoreTest`
- Run the Nuke pipeline used by CI: `./build.cmd CiBuildAndTest`
- Generate coverage locally: `./build.cmd TestCoverage`

## Architecture

- This is a .NET library that targets `net11.0` preview tooling in `src\WTorricos.Either\WTorricos.Either.csproj` and packs as `WTorricos.EitherUnion`.
- `src\WTorricos.Either\IEither.cs` defines the core `IEither<T>` union.
- `src\WTorricos.Either\Ok.cs`, `Failure.cs`, `Detail.cs`, and `Severity.cs` define the public result and error types.
- `src\WTorricos.Either\IEitherExtensions.cs` contains the main fluent API: `Map`, `FlatMap`, `Flatten`, `MapFailure`, `Match`, `Inspect`, `Tap`, `OnFailure`, `Filter`, `FromNullable`, `GetValueOrThrow`, and `Void`.
- `src\WTorricos.Either\IEitherAsyncExtensions.cs` contains the async counterparts: `MapAsync`, `FlatMapAsync`, `MatchAsync`, and `ActionAsync`.
- `src\WTorricos.Either\IEitherQueryExtensions.cs` adds LINQ query support.
- Public extension methods defensively validate null inputs and delegates with `ArgumentNullException`.
- The tests in `tests\WTorricos.Either.UnitTests` double as usage examples; `IEitherCoreTest.cs`, `IEitherExtensionsTest.cs`, `IEitherAsyncExtensionsTest.cs`, and `IEitherQueryTest.cs` show the intended public API patterns.
- `WTorricos.Either.Build\Build.cs` is the Nuke build entrypoint. CI runs `CiBuildAndTest`, which restores, tests with XPlat Code Coverage, and then performs the release build.

## Conventions

- Use file-scoped namespaces.
- Prefer explicit types; `var` is disallowed by the repo analyzers.
- Keep members expression-bodied when it stays readable.
- Preserve the `IEither<T>` / `Ok<T>` / `Failure` pattern matching style instead of adding success flags or similar shortcuts.
- `Failure` instances should keep `ErrorCode`, `Level`, `Timestamp`, and `Details` populated consistently.
- Keep async paths aligned with the sync overloads; the public API intentionally mirrors both sync and `ValueTask` forms.
- Tests use xUnit and FluentAssertions, with `Fact(DisplayName = "...")` and pattern matching to assert result shapes.
- `WTorricos.Either.Build\Directory.Build.props` and `.targets` intentionally block parent MSBuild imports; keep build-project changes local to that folder.
- Update `CHANGELOG.md` when changes are appropriate for release notes (for example, user-visible behavior, API, package, or documentation-impacting updates).
