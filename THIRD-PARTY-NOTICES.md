# Third-party notices

## Runtime

Het NuGet-pakket **NlFuzzy** heeft **geen runtime-dependencies**. Er wordt geen broncode of binaire
code van derden meegeleverd of ingesloten. De bibliotheek gebruikt alleen types uit .NET zelf
(`netstandard2.0`, `net8.0`, `net10.0`).

Te controleren met:

```bash
dotnet list src/NlFuzzy/NlFuzzy.csproj package --include-transitive
```

De test `PublicApiTests.AssemblyIsStrongNamedAgnosticAndDependencyFree` bewaakt dit automatisch.

## Algoritmes

De algoritmes zijn zelfstandig geïmplementeerd op basis van openbare beschrijvingen. Er is geen
code overgenomen, vertaald of geport uit een implementatie van derden — ook niet uit Apache Commons
Codec (`ColognePhonetic`) of uit T-SQL-voorbeelden van een Nederlandse Metaphone-variant. Zie
[PROVENANCE.md](PROVENANCE.md) voor de volledige onderbouwing per component.

## Ontwikkel- en testafhankelijkheden

Onderstaande pakketten worden alleen gebruikt bij het bouwen, testen en benchmarken. Ze zitten
**niet** in het gepubliceerde NlFuzzy-pakket en worden er niet door meegeleverd.

| Pakket | Versie | Licentie | Gebruikt in |
| --- | --- | --- | --- |
| xunit | 2.9.3 | Apache-2.0 | tests |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 | tests |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT | tests |
| coverlet.collector | 6.0.4 | MIT | tests (coverage) |
| coverlet.msbuild | 6.0.4 | MIT | tests (coverage) |
| BenchmarkDotNet | 0.14.0 | MIT | benchmarks |

De actuele lijst staat in `tests/NlFuzzy.Tests/NlFuzzy.Tests.csproj` en
`bench/NlFuzzy.Benchmarks/NlFuzzy.Benchmarks.csproj`.
