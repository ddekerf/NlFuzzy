# Wikidata source for the evaluation dataset

`wikidata-p460-dutch-surnames.csv` is a snapshot of Dutch family names that Wikidata links to another
Dutch family name with "said to be the same as" (P460). It was fetched on **2026-11-22**. Wikidata's
data is released under **CC0 1.0**, so it can be used and redistributed without restriction; it is
still recorded in `PROVENANCE.md` and `THIRD-PARTY-NOTICES.md` for traceability.

The tests never query Wikidata. They only read this file, so the build stays offline and a recorded
baseline cannot change under it.

## Query

```sparql
SELECT ?a ?b ?aLabel ?bLabel WHERE {
  ?a wdt:P31 wd:Q101352; wdt:P460 ?b; wdt:P407 wd:Q7411.   # a: family name, language Dutch
  ?b wdt:P31 wd:Q101352; wdt:P407 wd:Q7411.                # b: family name, language Dutch
  FILTER(STR(?a) < STR(?b))                                # each unordered pair once
  OPTIONAL { ?a rdfs:label ?an. FILTER(LANG(?an) = "nl") }
  OPTIONAL { ?a rdfs:label ?am. FILTER(LANG(?am) = "mul") }
  OPTIONAL { ?a rdfs:label ?ae. FILTER(LANG(?ae) = "en") }
  OPTIONAL { ?b rdfs:label ?bn. FILTER(LANG(?bn) = "nl") }
  OPTIONAL { ?b rdfs:label ?bm. FILTER(LANG(?bm) = "mul") }
  OPTIONAL { ?b rdfs:label ?be. FILTER(LANG(?be) = "en") }
  BIND(COALESCE(?an, ?am, ?ae, "") AS ?aLabel)
  BIND(COALESCE(?bn, ?bm, ?be, "") AS ?bLabel)
}
ORDER BY ?a ?b
```

Refetch with (PowerShell or bash, `curl` required), with the query above saved as `query.rq`:

```bash
curl -s -G -H "Accept: text/csv" -A "nlfuzzy-golden-dataset/1.0" \
  --data-urlencode "query@query.rq" https://query.wikidata.org/sparql \
  | sed 's|http://www.wikidata.org/entity/||g'
```

On 2026-11-22 this returned 350 rows, all with a label.

## Labeling

Every row was labeled by hand, on linguistic judgement and without looking at NlFuzzy's scores. The
`decision` column records the outcome:

| Decision | Meaning |
|---|---|
| `match:<category>` | Spelling variant of the same name; copied into `evaluation-pairs.csv` as `match`. |
| `nomatch:<category>` | Related but different name: a changed ending (`Daams`/`Daam`), a derived or composite name (`Bos`/`Bosman`), a different vowel; copied as `nomatch`. |
| `excluded:case` | Differs only in capitalization, so it says nothing about matching. |
| `excluded:particle` | A particle on one side only (`de Grave`/`Grave`); whether that is the same name is ambiguous. |
| `excluded:foreign` | A cognate in another language (`Fischer`/`Visser`, `Brun`/`Bruijn`); not a Dutch spelling variant. |
| `excluded:ambiguous` | No confident label either way. |
| `excluded:duplicate` | The same pair in the other order. |
| `excluded:core` | Already in the must-pass core dataset (`positive-pairs.csv`, `negative-pairs.csv`). |

Only a `-se`/`-sen` swap counts as the same name. Other ending changes (`-s`, `-e`, `-en`) are
labeled `nomatch`, consistent with `Kuipers`/`Kuiper` in the core dataset.

Do not relabel a row to match NlFuzzy's output. If a label turns out to be wrong on linguistic
grounds, change it in a commit that explains why.
