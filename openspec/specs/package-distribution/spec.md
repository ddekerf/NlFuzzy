# package-distribution Specification

## Purpose

Defines what the published NlFuzzy NuGet package guarantees to consumers: supported frameworks,
dependencies, a stable public API surface, and how versions are released.

## Requirements

### Requirement: Supported target frameworks
The package SHALL ship builds for `netstandard2.0`, `net8.0` and `net10.0`, and every build SHALL
compile without warnings.

#### Scenario: Package contents
- **WHEN** the package is packed in CI
- **THEN** it contains assemblies for all three target frameworks, the README and the third-party notices

#### Scenario: Clean consumer
- **WHEN** CI installs the freshly packed package into a new `net8.0` console app and runs a fuzzy match and an index search
- **THEN** the app builds and runs successfully

### Requirement: No runtime dependencies
The library SHALL NOT reference any assembly outside the .NET base class library on any target
framework.

#### Scenario: Dependency guard
- **WHEN** the test suite inspects the library's referenced assemblies
- **THEN** only `System.*`, `netstandard` and `mscorlib` references are present

### Requirement: Public API changes are deliberate
Every change to the public API surface SHALL update the approved API snapshot and add a CHANGELOG
entry. Every public type SHALL live in the `NlFuzzy` namespace or one of its sub-namespaces.

#### Scenario: Unintended API change
- **WHEN** a public member is added, removed or changed without updating `PublicApi.approved.txt`
- **THEN** the public API test fails

### Requirement: Quality gates in CI
CI SHALL build and test every push and pull request to `main`, and SHALL fail when total line coverage
of the library drops below 95%.

#### Scenario: Coverage drop
- **WHEN** a change brings line coverage of `NlFuzzy` below 95%
- **THEN** the CI test step fails

### Requirement: Releases are tag-driven
A release SHALL be published from a git tag `v<Version>` that matches `<Version>` in the project file.
The release workflow SHALL refuse to publish when they differ, and SHALL build, test and pack before it
pushes to nuget.org.

#### Scenario: Tag mismatch
- **WHEN** tag `v1.0.1` is pushed while the project file says `1.0.0`
- **THEN** the release workflow fails before anything is published

### Requirement: Every README example is tested
Every code example in the README SHALL exist as a test, so the documentation cannot drift from the
behaviour.

#### Scenario: README example changes
- **WHEN** a README example's documented result no longer holds
- **THEN** the README examples test fails
