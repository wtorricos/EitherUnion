# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [1.0.0-preview.1] - 2026-06-24
### Added
- First public release of `WTorricos.EitherUnion`.
- Core union model with `IEither<T>`, `Ok<T>`, and `Failure`.
- Fluent APIs for sync and async flows (`Map`, `FlatMap`, `Flatten`, `MapFailure`, `Inspect`, `Filter`, `Match`, `Tap`, `OnFailure`, `MapAsync`, `FlatMapAsync`, `MatchAsync`, `ActionAsync`).
- LINQ query support (`Select`, `SelectMany`, `Where`).
- Rich failure context (`ErrorCode`, `Level`, `Timestamp`, `Details`, `TraceId`, `StackTrace`, `InnerError`, `Metadata`).
