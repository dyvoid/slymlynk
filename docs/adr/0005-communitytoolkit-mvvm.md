# ADR 0005: CommunityToolkit.MVVM for ViewModel boilerplate

- **Status:** Accepted
- **Date:** 2026-06-04

## Context

WPF MVVM requires `INotifyPropertyChanged` implementations and `ICommand` wrappers on every
ViewModel. Writing these by hand is repetitive and error-prone. Several libraries address this:
CommunityToolkit.MVVM (Microsoft), Prism, ReactiveUI, and others.

## Decision

Use `CommunityToolkit.MVVM`. Use `[ObservableProperty]` for bindable properties and
`[RelayCommand]` for commands. Source generators emit the boilerplate at compile time.

## Consequences

- ViewModels are significantly smaller and easier to read — the signal-to-noise ratio is high.
- Source-generated code means no runtime reflection; performance is equivalent to hand-written code.
- The library is Microsoft-maintained, ships on NuGet, and has no transitive dependencies.
- `[ObservableProperty]` and `[RelayCommand]` are well-understood patterns; AI agents working in
  this repo can be expected to know them.
- ViewModels remain plain C# classes — testable with xUnit without a UI host or WPF thread.
- If CommunityToolkit.MVVM is ever removed, the migration path is mechanical: replace attributes
  with hand-written implementations. There is no deep architectural coupling.
