# Bijdragen aan NlFuzzy

Bedankt voor je interesse. Dit is een kleine bibliotheek met een duidelijke scope: fuzzy matching
voor Nederlandse en Vlaamse namen en adressen, zonder runtime-dependencies.

## Licentie van bijdragen (inbound = outbound)

Door een pull request, patch of andere bijdrage in te dienen, ga je ermee akkoord dat je bijdrage
onder dezelfde licentie wordt uitgebracht als het project zelf: de **MIT-licentie** (zie
[LICENSE](LICENSE)). Je verklaart daarbij dat:

1. je de bijdrage zelf hebt geschreven, of het recht hebt om deze onder MIT bij te dragen;
2. je geen broncode hebt overgenomen uit werk met een incompatibele licentie (bijvoorbeeld GPL of
   LGPL) of uit werk zonder licentie;
3. als je code baseert op een externe beschrijving of implementatie, je de herkomst vermeldt in
   [PROVENANCE.md](PROVENANCE.md) en in de pull request.

Er is geen aparte CLA. Het inbound=outbound-model hierboven is voldoende.

## Regels voor code

- Geen runtime-dependencies in `src/NlFuzzy`. Dat is bewust en wordt door een test bewaakt.
- Alle drie de doelframeworks moeten blijven bouwen: `netstandard2.0`, `net8.0`, `net10.0`.
  `netstandard2.0` mist API's als `double.IsFinite` en generieke `Enum.IsDefined<T>`; gebruik de
  helpers in `src/NlFuzzy/Compatibility.cs`.
- Waarschuwingen zijn fouten (`TreatWarningsAsErrors`). Los analyzer-meldingen op in plaats van ze
  te onderdrukken, tenzij je de onderdrukking kunt motiveren.
- Publieke API-wijzigingen vereisen een update van `tests/NlFuzzy.Tests/Api/PublicApi.approved.txt`
  en een entry in [CHANGELOG.md](CHANGELOG.md).
- Wijzig je scoring, normalisatie of fonetiek, voeg dan cases toe aan de gouden dataset in
  `tests/NlFuzzy.Tests/Golden`.

## Controle vóór het indienen

```bash
dotnet build NlFuzzy.slnx -c Release
dotnet test tests/NlFuzzy.Tests/NlFuzzy.Tests.csproj -c Release
```

Prestatiegevoelige wijzigingen graag onderbouwen met een BenchmarkDotNet-run:

```bash
dotnet run --project bench/NlFuzzy.Benchmarks -c Release -- --filter '*'
```

## Beveiliging

Meld een kwetsbaarheid niet via een publieke issue. Gebruik in plaats daarvan de
security-advisory-functie van GitHub op deze repository.
