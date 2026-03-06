# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- _No changes yet._

## [1.0.1] - 2026-03-05

### Fixed

- NuGet package release fix (version bump for clean release)

## [1.0.0] - 2026-03-05

### Added

- Initial release: Trie-based HTTP router for ASP.NET Core
- Route matching with O(1) average lookup time using immutable trie structure
- Compiled per-route execution chains with short-circuit evaluation (not middleware)
- 11 built-in route parameter constraints: `int`, `long`, `guid`, `bool`, `slug`, `alpha`, `alphanum`, `filename`, `range`, `length`, `regex`
- Support for custom route constraints via `IRouteConstraint` interface
- Route groups with shared prefixes and chain nodes via `MapRouteGroup()`
- Dynamic route providers via `IRouteProvider` interface (static and configuration providers included)
- Hot-reload support with atomic trie swap for zero-downtime route updates
- Route metadata attachment and retrieval
- Before/after hooks via `IRouteWrapper` interface
- URL normalization (trailing slash handling, case sensitivity options)
- Thread-safe lock-free design with volatile reads on hot path
- Zero-allocation path parsing using `ReadOnlySpan<char>`
- Fluent route mapping API with `RouteMappingBuilder` for chainable configuration
- Source-generated logging messages for debugging and monitoring
- Full XML documentation on all public APIs
- Comprehensive test suite with 70+ tests covering all features
- CI/CD workflow configured for multi-platform testing (Windows, Linux, macOS)

### Technical Details

- Built for .NET 8.0 (LTS)
- ASP.NET Core middleware integration
- Dependency injection support
- Async/await native throughout
- Deterministic builds with SourceLink enabled
- Portable PDB symbols included

[1.0.1]: https://github.com/jamesgober/dotnet-webkit-router/releases/tag/v1.0.1
[1.0.0]: https://github.com/jamesgober/dotnet-webkit-router/releases/tag/v1.0.0
