# Provenance van de algoritmes en regeltabellen

Dit document legt vast waar de algoritmes en de Nederlandse regeltabel van NlFuzzy vandaan komen,
zodat een gebruiker of auditor de herkomst kan controleren. Het is een technische
provenance-verklaring, geen juridisch advies.

## Samenvatting

- NlFuzzy heeft **geen runtime-dependencies** en bevat geen meegepackte broncode van derden.
- Alle algoritmes zijn in C# geschreven op basis van openbare beschrijvingen van het *algoritme*,
  niet door broncode van derden over te nemen, te vertalen of te decompileren.
- Er is geen code overgenomen uit GPL- of LGPL-gelicenseerde projecten.

## Per component

### Levenshtein, Damerau-Levenshtein (OSA), Jaro-Winkler, Dice

Klassieke, in de literatuur beschreven algoritmes zonder auteursrechtelijk beschermde
referentie-implementatie in deze codebase. Geïmplementeerd vanuit de standaardformules
(rijgebaseerde DP voor Levenshtein/OSA, de Jaro-formule plus de Winkler-prefixbonus, bigram-Dice).
De implementaties gebruiken eigen optimalisaties (twee rijen in plaats van een volledige matrix,
gepakte `int`-bigrammen) die niet uit een externe implementatie komen.

### Kölner Phonetik (`ColognePhoneticEncoder`)

Gebaseerd op de gepubliceerde beschrijving van het algoritme van Hans Joachim Postel (1969), zoals
onder meer beschreven op <https://en.wikipedia.org/wiki/Cologne_phonetics>: de tabel met
codewaarden, de contextregels voor `c`, `d/t`, `p`, `x` en de nabewerking (dubbele codes
samenvoegen, nullen behalve de eerste verwijderen).

**Geen port van Apache Commons Codec.** De implementatie in
`src/NlFuzzy/Phonetics/ColognePhoneticEncoder.cs` is een eenvoudige `switch`-gebaseerde encoder met
een aparte `Compact`-stap; ze deelt geen structuur, klassenindeling, buffer-abstracties,
variabelenamen of commentaar met `org.apache.commons.codec.language.ColognePhonetic`. Er gelden
daarom geen Apache-2.0 NOTICE-verplichtingen. Wordt in de toekomst wél code geport, dan moeten de
Apache-2.0 copyright- en NOTICE-verplichtingen worden opgenomen in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

De testwaarden (`Wikipedia` → `3412`, `Breschnew` → `17863`, `Muellerluedenscheidt` → `65752682`,
`Peter` = `Petra` = `127`) zijn feitelijke uitkomsten van het algoritme en dienen als verificatie
van de implementatie.

### Nederlandse regeltabel (`DutchPhoneticRules`, `DutchPhoneticEncoder`)

De regeltabel bestaat uit **taalkundige feiten over Nederlandse spelling-klankcorrespondenties**
(bijvoorbeeld: `ij` en `ei` staan voor dezelfde diftong; eind-obstruenten worden verstemloosd;
`ch` en `g` vallen fonetisch samen). Zulke regels zijn feiten en geen auteursrechtelijk beschermde
code-expressie, en ze zijn in meerdere onafhankelijke bronnen gedocumenteerd — zie de bronnenlijst
in [README §11](README.md#11-bronnen), met name Taalportaal (INT/Meertens) over `ei`/`ij` en de
beschrijving van final-obstruent devoicing.

Het ontwerpdocument `PLAN-NlFuzzy.md` (§2.3) noemt het blogartikel van Diederik Krols (U2U, 2011)
over een Nederlandse Metaphone-variant als vindplaats van zo'n regelset. Voor de duidelijkheid:

- Er is **geen T-SQL, C# of andere broncode uit dat artikel overgenomen of vertaald**. De
  implementatie hier is een eigen, tabelgedreven encoder met twee profielen, klinkerklassen,
  Nederlandse tussenvoegselverwerking en een eigen uitvoeralfabet; dat komt in de beschreven
  T-SQL-implementatie niet voor.
- De overlap beperkt zich tot regels die feitelijke Nederlandse uitspraakregels beschrijven en die
  ook los van dat artikel te onderbouwen zijn.
- Het artikel vermeldt geen expliciete licentie voor de daar getoonde broncode.

**Openstaande actie vóór publicatie van 1.0.0 op nuget.org:** vraag Diederik Krols/U2U schriftelijk
te bevestigen dat hergebruik van de daar beschreven Nederlandse regelset onder MIT-compatibele
voorwaarden is toegestaan, óf verwijder elke verwijzing die overname suggereert en onderbouw elke
regel expliciet met een onafhankelijke fonologische bron. Leg het antwoord vast in dit document.

### Tussenvoegsels en glued-prefixlijsten (`DutchAffixes`)

Lijsten van Nederlandse en Vlaamse tussenvoegsels (`van`, `van der`, `de`, `ten`, `op de`, …) zijn
algemeen bekende taalfeiten; de lijst is met de hand samengesteld en niet uit een bestaand bestand
gekopieerd.

### Gouden dataset (`tests/NlFuzzy.Tests/Golden`)

De positieve en negatieve naamparen zijn met de hand samengesteld uit gangbare Nederlandse en
Vlaamse spellingvarianten. Ze bevatten geen persoonsgegevens uit een echte administratie.

## Controle-instructie

Wil je dit zelf verifiëren:

1. `dotnet list package --include-transitive` op `src/NlFuzzy` toont dat er geen
   runtime-dependencies zijn.
2. `PublicApiTests.AssemblyIsStrongNamedAgnosticAndDependencyFree` faalt zodra er wél een
   dependency bij komt.
3. De volledige broncode staat in deze repository; er zijn geen gegenereerde of meegeleverde
   binaries van derden.
