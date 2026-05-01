---
title: Benchmarks - debian
---

# Benchmarks: debian

**.NET** 10.0.3 &nbsp;&middot;&nbsp; **Commit** `4788c5a` &nbsp;&middot;&nbsp; 1 May 2026 17:51 UTC &nbsp;&middot;&nbsp; 76 benchmarks

## Analysis

| Method             | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1      | Allocated | Alloc Ratio |
|------------------- |-------------- |--------:|---------:|---------:|------:|------------:|----------:|----------:|------------:|
| LeanLucene_Analyse | 100000        | 1.475 s | 0.0025 s | 0.0022 s |  1.00 |  48000.0000 | 1000.0000 | 197.07 MB |        1.00 |
| LuceneNet_Analyse  | 100000        | 2.238 s | 0.0027 s | 0.0025 s |  1.52 | 144000.0000 |         - | 576.92 MB |        2.93 |

## Block-Join

| Method                           | BlockCount | Mean          | Error       | StdDev      | Ratio | Gen0      | Gen1     | Allocated  | Alloc Ratio |
|--------------------------------- |----------- |--------------:|------------:|------------:|------:|----------:|---------:|-----------:|------------:|
| LeanLucene_IndexBlocks           | 500        | 65,346.118 μs | 273.7098 μs | 242.6366 μs | 1.000 | 1375.0000 | 625.0000 | 10729212 B |       1.000 |
| LeanLucene_BlockJoinQuery        | 500        |      6.991 μs |   0.0087 μs |   0.0077 μs | 0.000 |    0.1678 |        - |      720 B |       0.000 |
| LuceneNet_IndexBlocks            | 500        | 55,413.125 μs | 267.6132 μs | 237.2321 μs | 0.848 | 5000.0000 | 666.6667 | 28715806 B |       2.676 |
| LuceneNet_ToParentBlockJoinQuery | 500        |     21.762 μs |   0.0500 μs |   0.0468 μs | 0.000 |    3.0518 |        - |    12888 B |       0.001 |

## Boolean queries

| Method                  | BooleanType | DocumentCount | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------------------------ |------------ |-------------- |---------:|--------:|--------:|------:|--------:|---------:|--------:|----------:|------------:|
| **LeanLucene_BooleanQuery** | **Must**        | **100000**        | **263.7 μs** | **2.53 μs** | **2.12 μs** |  **1.00** |    **0.00** |   **2.9297** |       **-** |  **12.94 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Must        | 100000        | 488.8 μs | 1.13 μs | 1.06 μs |  1.85 |    0.01 |  35.1563 |       - | 144.09 KB |       11.14 |
|                         |             |               |          |         |         |       |         |          |         |           |             |
| **LeanLucene_BooleanQuery** | **MustNot**     | **100000**        | **176.4 μs** | **1.38 μs** | **1.29 μs** |  **1.00** |    **0.00** |   **3.1738** |       **-** |   **13.3 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | MustNot     | 100000        | 411.1 μs | 1.40 μs | 1.31 μs |  2.33 |    0.02 |  36.1328 |       - | 149.06 KB |       11.21 |
|                         |             |               |          |         |         |       |         |          |         |           |             |
| **LeanLucene_BooleanQuery** | **Should**      | **100000**        | **219.9 μs** | **1.60 μs** | **1.33 μs** |  **1.00** |    **0.00** |   **3.1738** |       **-** |  **13.69 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Should      | 100000        | 583.1 μs | 1.03 μs | 0.91 μs |  2.65 |    0.02 | 169.9219 | 40.0391 | 695.01 KB |       50.76 |

## Deletion

| Method                     | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|--------------------------- |-------------- |--------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_DeleteDocuments | 100000        | 9.392 s | 0.0218 s | 0.0193 s |  1.00 | 151000.0000 | 72000.0000 | 7000.0000 | 965.25 MB |        1.00 |
| LuceneNet_DeleteDocuments  | 100000        | 7.123 s | 0.0125 s | 0.0111 s |  0.76 | 336000.0000 | 33000.0000 | 1000.0000 |   1960 MB |        2.03 |

## Fuzzy queries

| Method                | QueryTerm | DocumentCount | Mean     | Error     | StdDev    | Ratio | Gen0     | Gen1     | Allocated  | Alloc Ratio |
|---------------------- |---------- |-------------- |---------:|----------:|----------:|------:|---------:|---------:|-----------:|------------:|
| **LeanLucene_FuzzyQuery** | **goverment** | **100000**        | **6.901 ms** | **0.0829 ms** | **0.0775 ms** |  **1.00** |        **-** |        **-** |   **25.88 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | goverment | 100000        | 8.637 ms | 0.0335 ms | 0.0313 ms |  1.25 | 593.7500 | 203.1250 | 2870.85 KB |      110.94 |
|                       |           |               |          |           |           |       |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **markts**    | **100000**        | **7.429 ms** | **0.0512 ms** | **0.0479 ms** |  **1.00** |   **7.8125** |        **-** |   **47.67 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | markts    | 100000        | 9.262 ms | 0.0251 ms | 0.0223 ms |  1.25 | 625.0000 | 187.5000 | 2806.02 KB |       58.87 |
|                       |           |               |          |           |           |       |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **presiden**  | **100000**        | **7.947 ms** | **0.0402 ms** | **0.0335 ms** |  **1.00** |        **-** |        **-** |   **30.61 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | presiden  | 100000        | 8.651 ms | 0.0169 ms | 0.0141 ms |  1.09 | 593.7500 | 218.7500 | 2844.58 KB |       92.93 |

## gutenberg-analysis

| Method                      | Mean     | Error   | StdDev   | Median   | Ratio | RatioSD | Gen0       | Gen1      | Gen2      | Allocated | Alloc Ratio |
|---------------------------- |---------:|--------:|---------:|---------:|------:|--------:|-----------:|----------:|----------:|----------:|------------:|
| LeanLucene_Standard_Analyse | 115.5 ms | 0.57 ms |  0.53 ms | 115.6 ms |  1.00 |    0.00 |  1200.0000 |  600.0000 |         - |   7.08 MB |        1.00 |
| LeanLucene_English_Analyse  | 355.3 ms | 7.03 ms | 14.68 ms | 362.5 ms |  3.08 |    0.13 | 11000.0000 | 7000.0000 | 2000.0000 | 111.87 MB |       15.80 |

## gutenberg-index

| Method                    | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0       | Gen1       | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |---------:|---------:|---------:|------:|--------:|-----------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Standard_Index | 727.1 ms | 10.71 ms | 10.02 ms |  1.00 |    0.00 | 11000.0000 |  6000.0000 | 1000.0000 |  76.58 MB |        1.00 |
| LeanLucene_English_Index  | 733.2 ms |  6.18 ms |  5.78 ms |  1.01 |    0.02 | 28000.0000 | 10000.0000 | 1000.0000 | 169.78 MB |        2.22 |
| LuceneNet_Index           | 649.3 ms |  2.15 ms |  1.79 ms |  0.89 |    0.01 | 41000.0000 |  3000.0000 |         - | 207.68 MB |        2.71 |

## gutenberg-search

| Method                     | SearchTerm | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| **LeanLucene_Standard_Search** | **death**      | **11.36 μs** | **0.022 μs** | **0.021 μs** |  **1.00** |    **0.00** | **0.1068** |      **-** |     **472 B** |        **1.00** |
| LeanLucene_English_Search  | death      | 11.58 μs | 0.034 μs | 0.032 μs |  1.02 |    0.00 | 0.1068 |      - |     472 B |        1.00 |
| LuceneNet_Search           | death      | 22.65 μs | 0.316 μs | 0.295 μs |  1.99 |    0.03 | 2.6550 | 0.0305 |   11231 B |       23.79 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **love**       | **15.54 μs** | **0.036 μs** | **0.033 μs** |  **1.00** |    **0.00** | **0.0916** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | love       | 20.46 μs | 0.043 μs | 0.040 μs |  1.32 |    0.00 | 0.0916 |      - |     464 B |        1.00 |
| LuceneNet_Search           | love       | 29.37 μs | 0.060 μs | 0.056 μs |  1.89 |    0.01 | 2.6245 | 0.0305 |   11175 B |       24.08 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **man**        | **39.53 μs** | **0.064 μs** | **0.059 μs** |  **1.00** |    **0.00** | **0.0610** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | man        | 41.55 μs | 0.069 μs | 0.064 μs |  1.05 |    0.00 | 0.0610 |      - |     464 B |        1.00 |
| LuceneNet_Search           | man        | 48.22 μs | 0.234 μs | 0.219 μs |  1.22 |    0.01 | 2.6245 | 0.0610 |   11038 B |       23.79 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **night**      | **25.44 μs** | **0.051 μs** | **0.048 μs** |  **1.00** |    **0.00** | **0.0916** |      **-** |     **472 B** |        **1.00** |
| LeanLucene_English_Search  | night      | 26.50 μs | 0.054 μs | 0.051 μs |  1.04 |    0.00 | 0.0916 |      - |     472 B |        1.00 |
| LuceneNet_Search           | night      | 35.27 μs | 0.053 μs | 0.047 μs |  1.39 |    0.00 | 2.6245 | 0.0610 |   11223 B |       23.78 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **sea**        | **12.61 μs** | **0.016 μs** | **0.013 μs** |  **1.00** |    **0.00** | **0.1068** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | sea        | 14.08 μs | 0.023 μs | 0.020 μs |  1.12 |    0.00 | 0.1068 |      - |     464 B |        1.00 |
| LuceneNet_Search           | sea        | 26.54 μs | 0.117 μs | 0.103 μs |  2.10 |    0.01 | 2.6550 | 0.0305 |   11271 B |       24.29 |

## Indexing

| Method                    | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |--------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_IndexDocuments | 100000        | 9.510 s | 0.0425 s | 0.0398 s |  1.00 | 151000.0000 | 71000.0000 | 6000.0000 | 910.01 MB |        1.00 |
| LuceneNet_IndexDocuments  | 100000        | 7.057 s | 0.0251 s | 0.0222 s |  0.74 | 332000.0000 | 33000.0000 | 1000.0000 | 1925.7 MB |        2.12 |

## Index-sort (index)

| Method                    | DocumentCount | Mean     | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |---------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Index_Unsorted | 100000        |  9.316 s | 0.0253 s | 0.0237 s |  1.00 | 154000.0000 | 72000.0000 | 6000.0000 | 942.13 MB |        1.00 |
| LeanLucene_Index_Sorted   | 100000        | 10.183 s | 0.0467 s | 0.0437 s |  1.09 | 156000.0000 | 71000.0000 | 6000.0000 |  952.2 MB |        1.01 |

## Index-sort (search)

| Method                                   | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0    | Allocated | Alloc Ratio |
|----------------------------------------- |-------------- |---------:|--------:|--------:|------:|--------:|----------:|------------:|
| LeanLucene_SortedSearch_EarlyTermination | 100000        | 315.3 μs | 0.54 μs | 0.45 μs |  1.00 | 25.3906 | 105.73 KB |        1.00 |
| LeanLucene_SortedSearch_PostSort         | 100000        | 310.4 μs | 0.65 μs | 0.57 μs |  0.98 | 25.3906 | 105.73 KB |        1.00 |

## Phrase queries

| Method                 | PhraseType     | DocumentCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------------------- |--------------- |-------------- |-----------:|---------:|---------:|------:|--------:|--------:|--------:|----------:|------------:|
| **LeanLucene_PhraseQuery** | **ExactThreeWord** | **100000**        |   **439.7 μs** |  **3.04 μs** |  **2.85 μs** |  **1.00** |    **0.00** | **14.6484** |       **-** |  **59.77 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactThreeWord | 100000        |   342.7 μs |  0.94 μs |  0.84 μs |  0.78 |    0.01 | 90.3320 |  0.4883 | 369.88 KB |        6.19 |
|                        |                |               |            |          |          |       |         |         |         |           |             |
| **LeanLucene_PhraseQuery** | **ExactTwoWord**   | **100000**        |   **333.7 μs** |  **4.65 μs** |  **4.35 μs** |  **1.00** |    **0.00** | **10.2539** |       **-** |  **42.91 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactTwoWord   | 100000        |   405.7 μs |  0.68 μs |  0.64 μs |  1.22 |    0.02 | 72.2656 | 18.0664 | 297.27 KB |        6.93 |
|                        |                |               |            |          |          |       |         |         |         |           |             |
| **LeanLucene_PhraseQuery** | **SlopTwoWord**    | **100000**        |   **997.5 μs** | **18.72 μs** | **17.51 μs** |  **1.00** |    **0.00** | **11.7188** |       **-** |  **48.72 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | SlopTwoWord    | 100000        | 1,025.7 μs |  3.48 μs |  3.26 μs |  1.03 |    0.02 | 37.1094 |       - | 155.61 KB |        3.19 |

## Prefix queries

| Method                 | QueryPrefix | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| **LeanLucene_PrefixQuery** | **gov**         | **100000**        | **122.2 μs** | **1.16 μs** | **1.09 μs** |  **1.00** |  **5.8594** |      **-** |  **23.68 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | gov         | 100000        | 188.1 μs | 0.42 μs | 0.39 μs |  1.54 | 26.8555 | 0.2441 | 110.04 KB |        4.65 |
|                        |             |               |          |         |         |       |         |        |           |             |
| **LeanLucene_PrefixQuery** | **mark**        | **100000**        | **194.4 μs** | **1.56 μs** | **1.46 μs** |  **1.00** |  **8.5449** |      **-** |  **34.56 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | mark        | 100000        | 284.9 μs | 0.86 μs | 0.81 μs |  1.47 | 30.7617 |      - | 126.09 KB |        3.65 |
|                        |             |               |          |         |         |       |         |        |           |             |
| **LeanLucene_PrefixQuery** | **pres**        | **100000**        | **245.8 μs** | **2.75 μs** | **2.44 μs** |  **1.00** | **15.6250** | **0.4883** |  **62.97 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | pres        | 100000        | 355.9 μs | 0.77 μs | 0.72 μs |  1.45 | 32.2266 |      - | 133.65 KB |        2.12 |

## Term queries

| Method               | QueryTerm  | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0    | Gen1   | Allocated | Alloc Ratio |
|--------------------- |----------- |-------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| **LeanLucene_TermQuery** | **government** | **100000**        | **106.5 μs** | **0.16 μs** | **0.13 μs** |  **1.00** |       **-** |      **-** |     **480 B** |        **1.00** |
| LuceneNet_TermQuery  | government | 100000        | 137.3 μs | 0.31 μs | 0.25 μs |  1.29 | 14.4043 |      - |   60896 B |      126.87 |
|                      |            |               |          |         |         |       |         |        |           |             |
| **LeanLucene_TermQuery** | **people**     | **100000**        | **152.2 μs** | **0.36 μs** | **0.34 μs** |  **1.00** |       **-** |      **-** |     **472 B** |        **1.00** |
| LuceneNet_TermQuery  | people     | 100000        | 178.4 μs | 0.34 μs | 0.32 μs |  1.17 | 13.9160 | 0.2441 |   58688 B |      124.34 |
|                      |            |               |          |         |         |       |         |        |           |             |
| **LeanLucene_TermQuery** | **said**       | **100000**        | **673.0 μs** | **1.03 μs** | **0.86 μs** |  **1.00** |       **-** |      **-** |     **464 B** |        **1.00** |
| LuceneNet_TermQuery  | said       | 100000        | 752.4 μs | 1.88 μs | 1.76 μs |  1.12 | 13.6719 |      - |   58720 B |      126.55 |

## Schema and JSON

| Method                      | DocumentCount | Mean       | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|---------------------------- |-------------- |-----------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Index_NoSchema   | 100000        | 9,155.2 ms | 34.52 ms | 32.29 ms |  1.00 | 147000.0000 | 68000.0000 | 2000.0000 | 910.05 MB |        1.00 |
| LeanLucene_Index_WithSchema | 100000        | 9,250.4 ms | 26.38 ms | 24.68 ms |  1.01 | 146000.0000 | 64000.0000 | 2000.0000 | 913.85 MB |        1.00 |
| LeanLucene_JsonMapping      | 100000        |   431.7 ms |  1.96 ms |  1.83 ms |  0.05 |  50000.0000 |  1000.0000 |         - | 212.83 MB |        0.23 |

## Suggester

| Method                 | DocumentCount | Mean      | Error     | StdDev    | Ratio | Gen0      | Gen1    | Allocated  | Alloc Ratio |
|----------------------- |-------------- |----------:|----------:|----------:|------:|----------:|--------:|-----------:|------------:|
| LeanLucene_DidYouMean  | 100000        |  4.648 ms | 0.0179 ms | 0.0158 ms |  1.00 |         - |       - |   24.91 KB |        1.00 |
| LeanLucene_SpellIndex  | 100000        |  4.697 ms | 0.0270 ms | 0.0240 ms |  1.01 |         - |       - |    23.2 KB |        0.93 |
| LuceneNet_SpellChecker | 100000        | 10.242 ms | 0.0249 ms | 0.0221 ms |  2.20 | 1296.8750 | 31.2500 | 5351.15 KB |      214.78 |

## Wildcard queries

| Method                   | WildcardPattern | DocumentCount | Mean        | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------- |---------------- |-------------- |------------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **LeanLucene_WildcardQuery** | **gov***            | **100000**        |   **121.45 μs** | **0.761 μs** | **0.712 μs** |  **1.00** |    **0.00** |  **6.1035** |      **-** |  **24.38 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | gov*            | 100000        |   204.49 μs | 0.473 μs | 0.419 μs |  1.68 |    0.01 | 31.4941 |      - | 129.06 KB |        5.29 |
|                          |                 |               |             |          |          |       |         |         |        |           |             |
| **LeanLucene_WildcardQuery** | **m*rket**          | **100000**        |   **526.62 μs** | **9.506 μs** | **8.892 μs** |  **1.00** |    **0.00** |  **1.9531** |      **-** |  **10.17 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | m*rket          | 100000        | 1,174.39 μs | 8.428 μs | 7.883 μs |  2.23 |    0.04 | 97.6563 | 9.7656 | 404.52 KB |       39.76 |
|                          |                 |               |             |          |          |       |         |         |        |           |             |
| **LeanLucene_WildcardQuery** | **pre*dent**        | **100000**        |    **93.67 μs** | **0.371 μs** | **0.289 μs** |  **1.00** |    **0.00** |  **2.0752** |      **-** |   **8.61 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | pre*dent        | 100000        |   409.97 μs | 0.711 μs | 0.630 μs |  4.38 |    0.01 | 92.7734 |      - |  379.5 KB |       44.07 |

<details>
<summary>Full data (report.json)</summary>

<pre><code class="lang-json">{
  "schemaVersion": 2,
  "runId": "2026-05-01 17-51 (4788c5a)",
  "runType": "full",
  "generatedAtUtc": "2026-05-01T17:51:11.1821714\u002B00:00",
  "commandLineArgs": [],
  "hostMachineName": "debian",
  "commitHash": "4788c5a",
  "dotnetVersion": "10.0.3",
  "provenance": {
    "sourceCommit": "4788c5a",
    "sourceRef": "",
    "sourceManifestPath": "",
    "gitCommitHash": "4788c5a",
    "gitAvailable": true,
    "gitDirty": false,
    "benchmarkDotNetVersion": "0.16.0-nightly.20260427.506\u002Bc68dc1556c410c4bdfe21373c7689be5781fbaf9",
    "runtimeFramework": ".NET 10.0.3",
    "runtimeIdentifier": "linux-x64",
    "osDescription": "Debian GNU/Linux 13 (trixie)",
    "processArchitecture": "X64",
    "effectiveDocCount": 100000,
    "dataFingerprintSha256": "",
    "dataSources": []
  },
  "totalBenchmarkCount": 76,
  "suites": [
    {
      "suiteName": "analysis",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.AnalysisBenchmarks-20260501-190532",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "AnalysisBenchmarks.LeanLucene_Analyse|DocumentCount=100000",
          "displayInfo": "AnalysisBenchmarks.LeanLucene_Analyse: DefaultJob [DocumentCount=100000]",
          "typeName": "AnalysisBenchmarks",
          "methodName": "LeanLucene_Analyse",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 1474646228.5714285,
            "medianNanoseconds": 1474771020,
            "minNanoseconds": 1471422848,
            "maxNanoseconds": 1478121534,
            "standardDeviationNanoseconds": 2249573.4076778516,
            "operationsPerSecond": 0.678128747508991
          },
          "gc": {
            "bytesAllocatedPerOperation": 206647088,
            "gen0Collections": 48,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "AnalysisBenchmarks.LuceneNet_Analyse|DocumentCount=100000",
          "displayInfo": "AnalysisBenchmarks.LuceneNet_Analyse: DefaultJob [DocumentCount=100000]",
          "typeName": "AnalysisBenchmarks",
          "methodName": "LuceneNet_Analyse",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 2237760460,
            "medianNanoseconds": 2237627706,
            "minNanoseconds": 2232022580,
            "maxNanoseconds": 2242112641,
            "standardDeviationNanoseconds": 2547886.8784043374,
            "operationsPerSecond": 0.44687535501453984
          },
          "gc": {
            "bytesAllocatedPerOperation": 604939928,
            "gen0Collections": 144,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "blockjoin",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BlockJoinBenchmarks-20260501-200824",
      "benchmarkCount": 4,
      "benchmarks": [
        {
          "key": "BlockJoinBenchmarks.LeanLucene_BlockJoinQuery|BlockCount=500",
          "displayInfo": "BlockJoinBenchmarks.LeanLucene_BlockJoinQuery: DefaultJob [BlockCount=500]",
          "typeName": "BlockJoinBenchmarks",
          "methodName": "LeanLucene_BlockJoinQuery",
          "parameters": {
            "BlockCount": "500"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 6990.77235358102,
            "medianNanoseconds": 6986.786460876465,
            "minNanoseconds": 6980.438858032227,
            "maxNanoseconds": 7005.625297546387,
            "standardDeviationNanoseconds": 7.738392741185511,
            "operationsPerSecond": 143045.7107486486
          },
          "gc": {
            "bytesAllocatedPerOperation": 720,
            "gen0Collections": 22,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BlockJoinBenchmarks.LeanLucene_IndexBlocks|BlockCount=500",
          "displayInfo": "BlockJoinBenchmarks.LeanLucene_IndexBlocks: DefaultJob [BlockCount=500]",
          "typeName": "BlockJoinBenchmarks",
          "methodName": "LeanLucene_IndexBlocks",
          "parameters": {
            "BlockCount": "500"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 65346118.39285714,
            "medianNanoseconds": 65266620.5,
            "minNanoseconds": 64840135.875,
            "maxNanoseconds": 65809768.625,
            "standardDeviationNanoseconds": 242636.59871677606,
            "operationsPerSecond": 15.303127784699575
          },
          "gc": {
            "bytesAllocatedPerOperation": 10729212,
            "gen0Collections": 11,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        },
        {
          "key": "BlockJoinBenchmarks.LuceneNet_IndexBlocks|BlockCount=500",
          "displayInfo": "BlockJoinBenchmarks.LuceneNet_IndexBlocks: DefaultJob [BlockCount=500]",
          "typeName": "BlockJoinBenchmarks",
          "methodName": "LuceneNet_IndexBlocks",
          "parameters": {
            "BlockCount": "500"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 55413124.658730164,
            "medianNanoseconds": 55413339.94444445,
            "minNanoseconds": 54918312.55555555,
            "maxNanoseconds": 55844040,
            "standardDeviationNanoseconds": 237232.11035579568,
            "operationsPerSecond": 18.046266225892264
          },
          "gc": {
            "bytesAllocatedPerOperation": 28715806,
            "gen0Collections": 45,
            "gen1Collections": 6,
            "gen2Collections": 0
          }
        },
        {
          "key": "BlockJoinBenchmarks.LuceneNet_ToParentBlockJoinQuery|BlockCount=500",
          "displayInfo": "BlockJoinBenchmarks.LuceneNet_ToParentBlockJoinQuery: DefaultJob [BlockCount=500]",
          "typeName": "BlockJoinBenchmarks",
          "methodName": "LuceneNet_ToParentBlockJoinQuery",
          "parameters": {
            "BlockCount": "500"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 21762.03877360026,
            "medianNanoseconds": 21757.45782470703,
            "minNanoseconds": 21683.94512939453,
            "maxNanoseconds": 21849.47705078125,
            "standardDeviationNanoseconds": 46.75346531895437,
            "operationsPerSecond": 45951.576982442915
          },
          "gc": {
            "bytesAllocatedPerOperation": 12888,
            "gen0Collections": 100,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "boolean",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BooleanQueryBenchmarks-20260501-190837",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery|BooleanType=Must, DocumentCount=100000",
          "displayInfo": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery: DefaultJob [BooleanType=Must, DocumentCount=100000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LeanLucene_BooleanQuery",
          "parameters": {
            "BooleanType": "Must",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 263696.4533879207,
            "medianNanoseconds": 263637.01123046875,
            "minNanoseconds": 260714.79345703125,
            "maxNanoseconds": 268055.22998046875,
            "standardDeviationNanoseconds": 2116.616380675486,
            "operationsPerSecond": 3792.2390959460954
          },
          "gc": {
            "bytesAllocatedPerOperation": 13248,
            "gen0Collections": 6,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery|BooleanType=MustNot, DocumentCount=100000",
          "displayInfo": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery: DefaultJob [BooleanType=MustNot, DocumentCount=100000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LeanLucene_BooleanQuery",
          "parameters": {
            "BooleanType": "MustNot",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 176388.7826660156,
            "medianNanoseconds": 176388.60009765625,
            "minNanoseconds": 174365.86328125,
            "maxNanoseconds": 178616.54125976562,
            "standardDeviationNanoseconds": 1293.5342543950012,
            "operationsPerSecond": 5669.294752679687
          },
          "gc": {
            "bytesAllocatedPerOperation": 13616,
            "gen0Collections": 13,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery|BooleanType=Should, DocumentCount=100000",
          "displayInfo": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery: DefaultJob [BooleanType=Should, DocumentCount=100000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LeanLucene_BooleanQuery",
          "parameters": {
            "BooleanType": "Should",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 219851.07019981972,
            "medianNanoseconds": 219842.04833984375,
            "minNanoseconds": 217577.52270507812,
            "maxNanoseconds": 222400.82958984375,
            "standardDeviationNanoseconds": 1333.1440582113023,
            "operationsPerSecond": 4548.5336918811145
          },
          "gc": {
            "bytesAllocatedPerOperation": 14022,
            "gen0Collections": 13,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery|BooleanType=Must, DocumentCount=100000",
          "displayInfo": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery: DefaultJob [BooleanType=Must, DocumentCount=100000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LuceneNet_BooleanQuery",
          "parameters": {
            "BooleanType": "Must",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 488821.9752604167,
            "medianNanoseconds": 488536.9228515625,
            "minNanoseconds": 487625.478515625,
            "maxNanoseconds": 490750.501953125,
            "standardDeviationNanoseconds": 1059.4326164698466,
            "operationsPerSecond": 2045.7345426568775
          },
          "gc": {
            "bytesAllocatedPerOperation": 147552,
            "gen0Collections": 36,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery|BooleanType=MustNot, DocumentCount=100000",
          "displayInfo": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery: DefaultJob [BooleanType=MustNot, DocumentCount=100000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LuceneNet_BooleanQuery",
          "parameters": {
            "BooleanType": "MustNot",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 411144.43125,
            "medianNanoseconds": 411198.1689453125,
            "minNanoseconds": 408751.54833984375,
            "maxNanoseconds": 413182.01220703125,
            "standardDeviationNanoseconds": 1309.7405099054952,
            "operationsPerSecond": 2432.23530222629
          },
          "gc": {
            "bytesAllocatedPerOperation": 152640,
            "gen0Collections": 74,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery|BooleanType=Should, DocumentCount=100000",
          "displayInfo": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery: DefaultJob [BooleanType=Should, DocumentCount=100000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LuceneNet_BooleanQuery",
          "parameters": {
            "BooleanType": "Should",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 583142.1185128348,
            "medianNanoseconds": 583245.083984375,
            "minNanoseconds": 580867.5595703125,
            "maxNanoseconds": 584444.34765625,
            "standardDeviationNanoseconds": 913.1401887752359,
            "operationsPerSecond": 1714.8478359790956
          },
          "gc": {
            "bytesAllocatedPerOperation": 711688,
            "gen0Collections": 174,
            "gen1Collections": 41,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "deletion",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.DeletionBenchmarks-20260501-193422",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "DeletionBenchmarks.LeanLucene_DeleteDocuments|DocumentCount=100000",
          "displayInfo": "DeletionBenchmarks.LeanLucene_DeleteDocuments: DefaultJob [DocumentCount=100000]",
          "typeName": "DeletionBenchmarks",
          "methodName": "LeanLucene_DeleteDocuments",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 9391840032.714285,
            "medianNanoseconds": 9395603365.5,
            "minNanoseconds": 9354450958,
            "maxNanoseconds": 9424544353,
            "standardDeviationNanoseconds": 19318826.569166705,
            "operationsPerSecond": 0.10647540806878451
          },
          "gc": {
            "bytesAllocatedPerOperation": 1012139240,
            "gen0Collections": 151,
            "gen1Collections": 72,
            "gen2Collections": 7
          }
        },
        {
          "key": "DeletionBenchmarks.LuceneNet_DeleteDocuments|DocumentCount=100000",
          "displayInfo": "DeletionBenchmarks.LuceneNet_DeleteDocuments: DefaultJob [DocumentCount=100000]",
          "typeName": "DeletionBenchmarks",
          "methodName": "LuceneNet_DeleteDocuments",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 7123391470.928572,
            "medianNanoseconds": 7123914598,
            "minNanoseconds": 7102522504,
            "maxNanoseconds": 7146841201,
            "standardDeviationNanoseconds": 11094876.335152918,
            "operationsPerSecond": 0.1403825697466048
          },
          "gc": {
            "bytesAllocatedPerOperation": 2055208552,
            "gen0Collections": 336,
            "gen1Collections": 33,
            "gen2Collections": 1
          }
        }
      ]
    },
    {
      "suiteName": "fuzzy",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.FuzzyQueryBenchmarks-20260501-192351",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery|DocumentCount=100000, QueryTerm=goverment",
          "displayInfo": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery: DefaultJob [QueryTerm=goverment, DocumentCount=100000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LeanLucene_FuzzyQuery",
          "parameters": {
            "QueryTerm": "goverment",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 6901026.713020833,
            "medianNanoseconds": 6872888.71875,
            "minNanoseconds": 6818058.359375,
            "maxNanoseconds": 7055159,
            "standardDeviationNanoseconds": 77523.23593613277,
            "operationsPerSecond": 144.90597436946643
          },
          "gc": {
            "bytesAllocatedPerOperation": 26498,
            "gen0Collections": 0,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery|DocumentCount=100000, QueryTerm=markts",
          "displayInfo": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery: DefaultJob [QueryTerm=markts, DocumentCount=100000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LeanLucene_FuzzyQuery",
          "parameters": {
            "QueryTerm": "markts",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 7429254.71875,
            "medianNanoseconds": 7418487.4140625,
            "minNanoseconds": 7373066.9765625,
            "maxNanoseconds": 7519526.328125,
            "standardDeviationNanoseconds": 47932.9847195804,
            "operationsPerSecond": 134.6030036466772
          },
          "gc": {
            "bytesAllocatedPerOperation": 48809,
            "gen0Collections": 1,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery|DocumentCount=100000, QueryTerm=presiden",
          "displayInfo": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery: DefaultJob [QueryTerm=presiden, DocumentCount=100000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LeanLucene_FuzzyQuery",
          "parameters": {
            "QueryTerm": "presiden",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 7947070.469951923,
            "medianNanoseconds": 7940726.234375,
            "minNanoseconds": 7895220.484375,
            "maxNanoseconds": 8001698.03125,
            "standardDeviationNanoseconds": 33531.76128808619,
            "operationsPerSecond": 125.83253209859225
          },
          "gc": {
            "bytesAllocatedPerOperation": 31346,
            "gen0Collections": 0,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery|DocumentCount=100000, QueryTerm=goverment",
          "displayInfo": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery: DefaultJob [QueryTerm=goverment, DocumentCount=100000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LuceneNet_FuzzyQuery",
          "parameters": {
            "QueryTerm": "goverment",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 8636823.305208333,
            "medianNanoseconds": 8628756.28125,
            "minNanoseconds": 8586603.25,
            "maxNanoseconds": 8700210.953125,
            "standardDeviationNanoseconds": 31309.73135514723,
            "operationsPerSecond": 115.78331113905757
          },
          "gc": {
            "bytesAllocatedPerOperation": 2939746,
            "gen0Collections": 38,
            "gen1Collections": 13,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery|DocumentCount=100000, QueryTerm=markts",
          "displayInfo": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery: DefaultJob [QueryTerm=markts, DocumentCount=100000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LuceneNet_FuzzyQuery",
          "parameters": {
            "QueryTerm": "markts",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 9261569.57142857,
            "medianNanoseconds": 9259021.0859375,
            "minNanoseconds": 9222466.65625,
            "maxNanoseconds": 9300143.8125,
            "standardDeviationNanoseconds": 22255.921087136583,
            "operationsPerSecond": 107.973059240946
          },
          "gc": {
            "bytesAllocatedPerOperation": 2873368,
            "gen0Collections": 40,
            "gen1Collections": 12,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery|DocumentCount=100000, QueryTerm=presiden",
          "displayInfo": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery: DefaultJob [QueryTerm=presiden, DocumentCount=100000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LuceneNet_FuzzyQuery",
          "parameters": {
            "QueryTerm": "presiden",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 8650627.27764423,
            "medianNanoseconds": 8651681.6875,
            "minNanoseconds": 8616220.328125,
            "maxNanoseconds": 8670259.078125,
            "standardDeviationNanoseconds": 14100.471398661664,
            "operationsPerSecond": 115.59855348112092
          },
          "gc": {
            "bytesAllocatedPerOperation": 2912850,
            "gen0Collections": 38,
            "gen1Collections": 14,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-analysis",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergAnalysisBenchmarks-20260501-201111",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "GutenbergAnalysisBenchmarks.LeanLucene_English_Analyse",
          "displayInfo": "GutenbergAnalysisBenchmarks.LeanLucene_English_Analyse: DefaultJob",
          "typeName": "GutenbergAnalysisBenchmarks",
          "methodName": "LeanLucene_English_Analyse",
          "parameters": {},
          "statistics": {
            "sampleCount": 53,
            "meanNanoseconds": 355339775.8490566,
            "medianNanoseconds": 362525961,
            "minNanoseconds": 325090536,
            "maxNanoseconds": 364494865,
            "standardDeviationNanoseconds": 14679132.22332486,
            "operationsPerSecond": 2.814207887677585
          },
          "gc": {
            "bytesAllocatedPerOperation": 117301704,
            "gen0Collections": 11,
            "gen1Collections": 7,
            "gen2Collections": 2
          }
        },
        {
          "key": "GutenbergAnalysisBenchmarks.LeanLucene_Standard_Analyse",
          "displayInfo": "GutenbergAnalysisBenchmarks.LeanLucene_Standard_Analyse: DefaultJob",
          "typeName": "GutenbergAnalysisBenchmarks",
          "methodName": "LeanLucene_Standard_Analyse",
          "parameters": {},
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 115460544.18666664,
            "medianNanoseconds": 115557085.8,
            "minNanoseconds": 114418489.6,
            "maxNanoseconds": 116366544,
            "standardDeviationNanoseconds": 534287.9350738291,
            "operationsPerSecond": 8.660967320431872
          },
          "gc": {
            "bytesAllocatedPerOperation": 7422736,
            "gen0Collections": 6,
            "gen1Collections": 3,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergIndexingBenchmarks-20260501-201317",
      "benchmarkCount": 3,
      "benchmarks": [
        {
          "key": "GutenbergIndexingBenchmarks.LeanLucene_English_Index",
          "displayInfo": "GutenbergIndexingBenchmarks.LeanLucene_English_Index: DefaultJob",
          "typeName": "GutenbergIndexingBenchmarks",
          "methodName": "LeanLucene_English_Index",
          "parameters": {},
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 733225434.2,
            "medianNanoseconds": 733227220,
            "minNanoseconds": 724061509,
            "maxNanoseconds": 743709447,
            "standardDeviationNanoseconds": 5782124.1595224105,
            "operationsPerSecond": 1.363837032046044
          },
          "gc": {
            "bytesAllocatedPerOperation": 178031208,
            "gen0Collections": 28,
            "gen1Collections": 10,
            "gen2Collections": 1
          }
        },
        {
          "key": "GutenbergIndexingBenchmarks.LeanLucene_Standard_Index",
          "displayInfo": "GutenbergIndexingBenchmarks.LeanLucene_Standard_Index: DefaultJob",
          "typeName": "GutenbergIndexingBenchmarks",
          "methodName": "LeanLucene_Standard_Index",
          "parameters": {},
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 727125922.2666667,
            "medianNanoseconds": 730109373,
            "minNanoseconds": 705524401,
            "maxNanoseconds": 743986121,
            "standardDeviationNanoseconds": 10017338.5028783,
            "operationsPerSecond": 1.3752776092519217
          },
          "gc": {
            "bytesAllocatedPerOperation": 80301216,
            "gen0Collections": 11,
            "gen1Collections": 6,
            "gen2Collections": 1
          }
        },
        {
          "key": "GutenbergIndexingBenchmarks.LuceneNet_Index",
          "displayInfo": "GutenbergIndexingBenchmarks.LuceneNet_Index: DefaultJob",
          "typeName": "GutenbergIndexingBenchmarks",
          "methodName": "LuceneNet_Index",
          "parameters": {},
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 649290486,
            "medianNanoseconds": 649519769,
            "minNanoseconds": 646547210,
            "maxNanoseconds": 652255629,
            "standardDeviationNanoseconds": 1792636.034613645,
            "operationsPerSecond": 1.5401426966234648
          },
          "gc": {
            "bytesAllocatedPerOperation": 217769120,
            "gen0Collections": 41,
            "gen1Collections": 3,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergSearchBenchmarks-20260501-201548",
      "benchmarkCount": 15,
      "benchmarks": [
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_English_Search|SearchTerm=death",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_English_Search: DefaultJob [SearchTerm=death]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_English_Search",
          "parameters": {
            "SearchTerm": "death"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 11581.804871622722,
            "medianNanoseconds": 11575.08267211914,
            "minNanoseconds": 11550.903549194336,
            "maxNanoseconds": 11651.996322631836,
            "standardDeviationNanoseconds": 31.785752211618135,
            "operationsPerSecond": 86342.32842673427
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 7,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_English_Search|SearchTerm=love",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_English_Search: DefaultJob [SearchTerm=love]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_English_Search",
          "parameters": {
            "SearchTerm": "love"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 20463.722570800783,
            "medianNanoseconds": 20459.515869140625,
            "minNanoseconds": 20381.586517333984,
            "maxNanoseconds": 20508.88739013672,
            "standardDeviationNanoseconds": 40.181156338950125,
            "operationsPerSecond": 48866.96428473268
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 3,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_English_Search|SearchTerm=man",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_English_Search: DefaultJob [SearchTerm=man]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_English_Search",
          "parameters": {
            "SearchTerm": "man"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 41547.14703776042,
            "medianNanoseconds": 41568.72625732422,
            "minNanoseconds": 41431.76647949219,
            "maxNanoseconds": 41630.879943847656,
            "standardDeviationNanoseconds": 64.19842364761034,
            "operationsPerSecond": 24069.041349364925
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 1,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_English_Search|SearchTerm=night",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_English_Search: DefaultJob [SearchTerm=night]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_English_Search",
          "parameters": {
            "SearchTerm": "night"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 26495.48139038086,
            "medianNanoseconds": 26478.55697631836,
            "minNanoseconds": 26423.179229736328,
            "maxNanoseconds": 26598.169525146484,
            "standardDeviationNanoseconds": 50.58053236461904,
            "operationsPerSecond": 37742.28462831584
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 3,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_English_Search|SearchTerm=sea",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_English_Search: DefaultJob [SearchTerm=sea]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_English_Search",
          "parameters": {
            "SearchTerm": "sea"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 14081.61648450579,
            "medianNanoseconds": 14080.984619140625,
            "minNanoseconds": 14054.241729736328,
            "maxNanoseconds": 14119.603469848633,
            "standardDeviationNanoseconds": 20.00795830145791,
            "operationsPerSecond": 71014.57429268258
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 7,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search|SearchTerm=death",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search: DefaultJob [SearchTerm=death]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_Standard_Search",
          "parameters": {
            "SearchTerm": "death"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 11363.61122233073,
            "medianNanoseconds": 11358.106918334961,
            "minNanoseconds": 11327.79116821289,
            "maxNanoseconds": 11408.77099609375,
            "standardDeviationNanoseconds": 20.598316566444765,
            "operationsPerSecond": 88000.19469470158
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 7,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search|SearchTerm=love",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search: DefaultJob [SearchTerm=love]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_Standard_Search",
          "parameters": {
            "SearchTerm": "love"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 15543.175099690756,
            "medianNanoseconds": 15555.287384033203,
            "minNanoseconds": 15475.163848876953,
            "maxNanoseconds": 15594.241912841797,
            "standardDeviationNanoseconds": 33.29939965173327,
            "operationsPerSecond": 64336.91916781506
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 3,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search|SearchTerm=man",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search: DefaultJob [SearchTerm=man]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_Standard_Search",
          "parameters": {
            "SearchTerm": "man"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 39532.54919840495,
            "medianNanoseconds": 39532.61486816406,
            "minNanoseconds": 39434.425720214844,
            "maxNanoseconds": 39645.804443359375,
            "standardDeviationNanoseconds": 59.48759123900592,
            "operationsPerSecond": 25295.61134500144
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 1,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search|SearchTerm=night",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search: DefaultJob [SearchTerm=night]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_Standard_Search",
          "parameters": {
            "SearchTerm": "night"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 25440.81951904297,
            "medianNanoseconds": 25439.21746826172,
            "minNanoseconds": 25348.601348876953,
            "maxNanoseconds": 25502.612426757812,
            "standardDeviationNanoseconds": 48.15774671251079,
            "operationsPerSecond": 39306.90987574043
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 3,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search|SearchTerm=sea",
          "displayInfo": "GutenbergSearchBenchmarks.LeanLucene_Standard_Search: DefaultJob [SearchTerm=sea]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LeanLucene_Standard_Search",
          "parameters": {
            "SearchTerm": "sea"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 12608.080105121318,
            "medianNanoseconds": 12604.585876464844,
            "minNanoseconds": 12588.20930480957,
            "maxNanoseconds": 12627.909973144531,
            "standardDeviationNanoseconds": 13.028479887017577,
            "operationsPerSecond": 79314.21688808962
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 7,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LuceneNet_Search|SearchTerm=death",
          "displayInfo": "GutenbergSearchBenchmarks.LuceneNet_Search: DefaultJob [SearchTerm=death]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LuceneNet_Search",
          "parameters": {
            "SearchTerm": "death"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 22647.846881103516,
            "medianNanoseconds": 22631.427490234375,
            "minNanoseconds": 22221.91943359375,
            "maxNanoseconds": 22977.351531982422,
            "standardDeviationNanoseconds": 295.3462984243637,
            "operationsPerSecond": 44154.30770305857
          },
          "gc": {
            "bytesAllocatedPerOperation": 11231,
            "gen0Collections": 87,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LuceneNet_Search|SearchTerm=love",
          "displayInfo": "GutenbergSearchBenchmarks.LuceneNet_Search: DefaultJob [SearchTerm=love]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LuceneNet_Search",
          "parameters": {
            "SearchTerm": "love"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 29365.245115152993,
            "medianNanoseconds": 29367.682830810547,
            "minNanoseconds": 29245.755828857422,
            "maxNanoseconds": 29451.568786621094,
            "standardDeviationNanoseconds": 55.98033728309497,
            "operationsPerSecond": 34053.861838326084
          },
          "gc": {
            "bytesAllocatedPerOperation": 11175,
            "gen0Collections": 86,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LuceneNet_Search|SearchTerm=man",
          "displayInfo": "GutenbergSearchBenchmarks.LuceneNet_Search: DefaultJob [SearchTerm=man]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LuceneNet_Search",
          "parameters": {
            "SearchTerm": "man"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 48218.416499837236,
            "medianNanoseconds": 48338.556701660156,
            "minNanoseconds": 47862.679931640625,
            "maxNanoseconds": 48583.080017089844,
            "standardDeviationNanoseconds": 218.66144002087864,
            "operationsPerSecond": 20738.963918555382
          },
          "gc": {
            "bytesAllocatedPerOperation": 11038,
            "gen0Collections": 43,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LuceneNet_Search|SearchTerm=night",
          "displayInfo": "GutenbergSearchBenchmarks.LuceneNet_Search: DefaultJob [SearchTerm=night]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LuceneNet_Search",
          "parameters": {
            "SearchTerm": "night"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 35274.70778111049,
            "medianNanoseconds": 35280.14456176758,
            "minNanoseconds": 35182.000427246094,
            "maxNanoseconds": 35353.870056152344,
            "standardDeviationNanoseconds": 47.229523692541534,
            "operationsPerSecond": 28348.923716258178
          },
          "gc": {
            "bytesAllocatedPerOperation": 11223,
            "gen0Collections": 43,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "GutenbergSearchBenchmarks.LuceneNet_Search|SearchTerm=sea",
          "displayInfo": "GutenbergSearchBenchmarks.LuceneNet_Search: DefaultJob [SearchTerm=sea]",
          "typeName": "GutenbergSearchBenchmarks",
          "methodName": "LuceneNet_Search",
          "parameters": {
            "SearchTerm": "sea"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 26537.303329467773,
            "medianNanoseconds": 26568.063842773438,
            "minNanoseconds": 26282.017150878906,
            "maxNanoseconds": 26622.038116455078,
            "standardDeviationNanoseconds": 103.4395211183695,
            "operationsPerSecond": 37682.80399800728
          },
          "gc": {
            "bytesAllocatedPerOperation": 11271,
            "gen0Collections": 87,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexingBenchmarks-20260501-185725",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "IndexingBenchmarks.LeanLucene_IndexDocuments|DocumentCount=100000",
          "displayInfo": "IndexingBenchmarks.LeanLucene_IndexDocuments: DefaultJob [DocumentCount=100000]",
          "typeName": "IndexingBenchmarks",
          "methodName": "LeanLucene_IndexDocuments",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 9510197276.133333,
            "medianNanoseconds": 9510918979,
            "minNanoseconds": 9439931282,
            "maxNanoseconds": 9571834084,
            "standardDeviationNanoseconds": 39755429.94407834,
            "operationsPerSecond": 0.105150289837792
          },
          "gc": {
            "bytesAllocatedPerOperation": 954213776,
            "gen0Collections": 151,
            "gen1Collections": 71,
            "gen2Collections": 6
          }
        },
        {
          "key": "IndexingBenchmarks.LuceneNet_IndexDocuments|DocumentCount=100000",
          "displayInfo": "IndexingBenchmarks.LuceneNet_IndexDocuments: DefaultJob [DocumentCount=100000]",
          "typeName": "IndexingBenchmarks",
          "methodName": "LuceneNet_IndexDocuments",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 7057124486.857142,
            "medianNanoseconds": 7058583509,
            "minNanoseconds": 7024227407,
            "maxNanoseconds": 7098717492,
            "standardDeviationNanoseconds": 22243123.819086585,
            "operationsPerSecond": 0.14170077371631365
          },
          "gc": {
            "bytesAllocatedPerOperation": 2019240000,
            "gen0Collections": 332,
            "gen1Collections": 33,
            "gen2Collections": 1
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortIndexBenchmarks-20260501-195638",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "IndexSortIndexBenchmarks.LeanLucene_Index_Sorted|DocumentCount=100000",
          "displayInfo": "IndexSortIndexBenchmarks.LeanLucene_Index_Sorted: DefaultJob [DocumentCount=100000]",
          "typeName": "IndexSortIndexBenchmarks",
          "methodName": "LeanLucene_Index_Sorted",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 10183087194.2,
            "medianNanoseconds": 10197623811,
            "minNanoseconds": 10106505138,
            "maxNanoseconds": 10244889574,
            "standardDeviationNanoseconds": 43720063.02830952,
            "operationsPerSecond": 0.09820204628804237
          },
          "gc": {
            "bytesAllocatedPerOperation": 998451728,
            "gen0Collections": 156,
            "gen1Collections": 71,
            "gen2Collections": 6
          }
        },
        {
          "key": "IndexSortIndexBenchmarks.LeanLucene_Index_Unsorted|DocumentCount=100000",
          "displayInfo": "IndexSortIndexBenchmarks.LeanLucene_Index_Unsorted: DefaultJob [DocumentCount=100000]",
          "typeName": "IndexSortIndexBenchmarks",
          "methodName": "LeanLucene_Index_Unsorted",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 9315702509.8,
            "medianNanoseconds": 9319831837,
            "minNanoseconds": 9260525881,
            "maxNanoseconds": 9365664611,
            "standardDeviationNanoseconds": 23685788.374287136,
            "operationsPerSecond": 0.10734563485126461
          },
          "gc": {
            "bytesAllocatedPerOperation": 987900048,
            "gen0Collections": 154,
            "gen1Collections": 72,
            "gen2Collections": 6
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortSearchBenchmarks-20260501-200558",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "IndexSortSearchBenchmarks.LeanLucene_SortedSearch_EarlyTermination|DocumentCount=100000",
          "displayInfo": "IndexSortSearchBenchmarks.LeanLucene_SortedSearch_EarlyTermination: DefaultJob [DocumentCount=100000]",
          "typeName": "IndexSortSearchBenchmarks",
          "methodName": "LeanLucene_SortedSearch_EarlyTermination",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 315305.2819636418,
            "medianNanoseconds": 315463.9169921875,
            "minNanoseconds": 314160.69677734375,
            "maxNanoseconds": 315930.9658203125,
            "standardDeviationNanoseconds": 451.93718168314086,
            "operationsPerSecond": 3171.5294896814034
          },
          "gc": {
            "bytesAllocatedPerOperation": 108272,
            "gen0Collections": 52,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "IndexSortSearchBenchmarks.LeanLucene_SortedSearch_PostSort|DocumentCount=100000",
          "displayInfo": "IndexSortSearchBenchmarks.LeanLucene_SortedSearch_PostSort: DefaultJob [DocumentCount=100000]",
          "typeName": "IndexSortSearchBenchmarks",
          "methodName": "LeanLucene_SortedSearch_PostSort",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 310380.1221749442,
            "medianNanoseconds": 310288.9650878906,
            "minNanoseconds": 309721.48974609375,
            "maxNanoseconds": 311595.830078125,
            "standardDeviationNanoseconds": 572.7810665212298,
            "operationsPerSecond": 3221.855810200226
          },
          "gc": {
            "bytesAllocatedPerOperation": 108272,
            "gen0Collections": 52,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "phrase",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PhraseQueryBenchmarks-20260501-191340",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery|DocumentCount=100000, PhraseType=ExactThreeWord",
          "displayInfo": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery: DefaultJob [PhraseType=ExactThreeWord, DocumentCount=100000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LeanLucene_PhraseQuery",
          "parameters": {
            "PhraseType": "ExactThreeWord",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 439677.0314778646,
            "medianNanoseconds": 439160.88671875,
            "minNanoseconds": 434847.14794921875,
            "maxNanoseconds": 445219.49072265625,
            "standardDeviationNanoseconds": 2845.40532103873,
            "operationsPerSecond": 2274.396723974299
          },
          "gc": {
            "bytesAllocatedPerOperation": 61203,
            "gen0Collections": 30,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery|DocumentCount=100000, PhraseType=ExactTwoWord",
          "displayInfo": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery: DefaultJob [PhraseType=ExactTwoWord, DocumentCount=100000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LeanLucene_PhraseQuery",
          "parameters": {
            "PhraseType": "ExactTwoWord",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 333695.3375651042,
            "medianNanoseconds": 332468.11767578125,
            "minNanoseconds": 329180.7822265625,
            "maxNanoseconds": 343177.21240234375,
            "standardDeviationNanoseconds": 4351.022865360224,
            "operationsPerSecond": 2996.745496346347
          },
          "gc": {
            "bytesAllocatedPerOperation": 43942,
            "gen0Collections": 21,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery|DocumentCount=100000, PhraseType=SlopTwoWord",
          "displayInfo": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery: DefaultJob [PhraseType=SlopTwoWord, DocumentCount=100000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LeanLucene_PhraseQuery",
          "parameters": {
            "PhraseType": "SlopTwoWord",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 997511.1380208334,
            "medianNanoseconds": 992157.802734375,
            "minNanoseconds": 974888.412109375,
            "maxNanoseconds": 1031631.46875,
            "standardDeviationNanoseconds": 17508.10342183159,
            "operationsPerSecond": 1002.4950718686758
          },
          "gc": {
            "bytesAllocatedPerOperation": 49893,
            "gen0Collections": 6,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery|DocumentCount=100000, PhraseType=ExactThreeWord",
          "displayInfo": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery: DefaultJob [PhraseType=ExactThreeWord, DocumentCount=100000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LuceneNet_PhraseQuery",
          "parameters": {
            "PhraseType": "ExactThreeWord",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 342698.4357910156,
            "medianNanoseconds": 342680.9521484375,
            "minNanoseconds": 341484.43408203125,
            "maxNanoseconds": 344269.42431640625,
            "standardDeviationNanoseconds": 836.3918671687272,
            "operationsPerSecond": 2918.0174041115847
          },
          "gc": {
            "bytesAllocatedPerOperation": 378760,
            "gen0Collections": 185,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery|DocumentCount=100000, PhraseType=ExactTwoWord",
          "displayInfo": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery: DefaultJob [PhraseType=ExactTwoWord, DocumentCount=100000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LuceneNet_PhraseQuery",
          "parameters": {
            "PhraseType": "ExactTwoWord",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 405732.7056640625,
            "medianNanoseconds": 405738.78759765625,
            "minNanoseconds": 404514.85791015625,
            "maxNanoseconds": 406793.103515625,
            "standardDeviationNanoseconds": 637.4612276291571,
            "operationsPerSecond": 2464.676832899878
          },
          "gc": {
            "bytesAllocatedPerOperation": 304408,
            "gen0Collections": 148,
            "gen1Collections": 37,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery|DocumentCount=100000, PhraseType=SlopTwoWord",
          "displayInfo": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery: DefaultJob [PhraseType=SlopTwoWord, DocumentCount=100000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LuceneNet_PhraseQuery",
          "parameters": {
            "PhraseType": "SlopTwoWord",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 1025676.3036458333,
            "medianNanoseconds": 1024735.76171875,
            "minNanoseconds": 1022511.4921875,
            "maxNanoseconds": 1034004.001953125,
            "standardDeviationNanoseconds": 3257.968279853996,
            "operationsPerSecond": 974.9664650001514
          },
          "gc": {
            "bytesAllocatedPerOperation": 159344,
            "gen0Collections": 19,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "prefix",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PrefixQueryBenchmarks-20260501-191847",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery|DocumentCount=100000, QueryPrefix=gov",
          "displayInfo": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery: DefaultJob [QueryPrefix=gov, DocumentCount=100000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LeanLucene_PrefixQuery",
          "parameters": {
            "QueryPrefix": "gov",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 122188.83484700522,
            "medianNanoseconds": 122084.33618164062,
            "minNanoseconds": 120695.57202148438,
            "maxNanoseconds": 124539.13110351562,
            "standardDeviationNanoseconds": 1087.3896807359197,
            "operationsPerSecond": 8184.05381516337
          },
          "gc": {
            "bytesAllocatedPerOperation": 24247,
            "gen0Collections": 24,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery|DocumentCount=100000, QueryPrefix=mark",
          "displayInfo": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery: DefaultJob [QueryPrefix=mark, DocumentCount=100000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LeanLucene_PrefixQuery",
          "parameters": {
            "QueryPrefix": "mark",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 194380.4318033854,
            "medianNanoseconds": 193964.76611328125,
            "minNanoseconds": 192208.91577148438,
            "maxNanoseconds": 196848.92236328125,
            "standardDeviationNanoseconds": 1463.3435283003112,
            "operationsPerSecond": 5144.550769449333
          },
          "gc": {
            "bytesAllocatedPerOperation": 35387,
            "gen0Collections": 35,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery|DocumentCount=100000, QueryPrefix=pres",
          "displayInfo": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery: DefaultJob [QueryPrefix=pres, DocumentCount=100000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LeanLucene_PrefixQuery",
          "parameters": {
            "QueryPrefix": "pres",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 245849.12806919642,
            "medianNanoseconds": 245273.49829101562,
            "minNanoseconds": 242316.8544921875,
            "maxNanoseconds": 250958.38818359375,
            "standardDeviationNanoseconds": 2436.192637279337,
            "operationsPerSecond": 4067.5352719515895
          },
          "gc": {
            "bytesAllocatedPerOperation": 64477,
            "gen0Collections": 32,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery|DocumentCount=100000, QueryPrefix=gov",
          "displayInfo": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery: DefaultJob [QueryPrefix=gov, DocumentCount=100000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LuceneNet_PrefixQuery",
          "parameters": {
            "QueryPrefix": "gov",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 188089.40232747394,
            "medianNanoseconds": 188067.83081054688,
            "minNanoseconds": 187578.50610351562,
            "maxNanoseconds": 188967.09375,
            "standardDeviationNanoseconds": 392.3860136178235,
            "operationsPerSecond": 5316.620647552196
          },
          "gc": {
            "bytesAllocatedPerOperation": 112680,
            "gen0Collections": 110,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery|DocumentCount=100000, QueryPrefix=mark",
          "displayInfo": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery: DefaultJob [QueryPrefix=mark, DocumentCount=100000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LuceneNet_PrefixQuery",
          "parameters": {
            "QueryPrefix": "mark",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 284921.670703125,
            "medianNanoseconds": 284751.92919921875,
            "minNanoseconds": 283552.17822265625,
            "maxNanoseconds": 286596.171875,
            "standardDeviationNanoseconds": 807.5218799557142,
            "operationsPerSecond": 3509.736544546494
          },
          "gc": {
            "bytesAllocatedPerOperation": 129112,
            "gen0Collections": 63,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery|DocumentCount=100000, QueryPrefix=pres",
          "displayInfo": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery: DefaultJob [QueryPrefix=pres, DocumentCount=100000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LuceneNet_PrefixQuery",
          "parameters": {
            "QueryPrefix": "pres",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 355937.4176432292,
            "medianNanoseconds": 355876.52099609375,
            "minNanoseconds": 354676.8857421875,
            "maxNanoseconds": 357160.0859375,
            "standardDeviationNanoseconds": 723.3169584449593,
            "operationsPerSecond": 2809.4826518136438
          },
          "gc": {
            "bytesAllocatedPerOperation": 136856,
            "gen0Collections": 66,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "query",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.TermQueryBenchmarks-20260501-185217",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "TermQueryBenchmarks.LeanLucene_TermQuery|DocumentCount=100000, QueryTerm=government",
          "displayInfo": "TermQueryBenchmarks.LeanLucene_TermQuery: DefaultJob [QueryTerm=government, DocumentCount=100000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LeanLucene_TermQuery",
          "parameters": {
            "QueryTerm": "government",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 106470.94179124098,
            "medianNanoseconds": 106506.82543945312,
            "minNanoseconds": 106119.62084960938,
            "maxNanoseconds": 106646.93469238281,
            "standardDeviationNanoseconds": 132.3330952487763,
            "operationsPerSecond": 9392.23400466123
          },
          "gc": {
            "bytesAllocatedPerOperation": 480,
            "gen0Collections": 0,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LeanLucene_TermQuery|DocumentCount=100000, QueryTerm=people",
          "displayInfo": "TermQueryBenchmarks.LeanLucene_TermQuery: DefaultJob [QueryTerm=people, DocumentCount=100000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LeanLucene_TermQuery",
          "parameters": {
            "QueryTerm": "people",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 152227.84329427083,
            "medianNanoseconds": 152167.73291015625,
            "minNanoseconds": 151813.31127929688,
            "maxNanoseconds": 152897.98583984375,
            "standardDeviationNanoseconds": 339.30692206142135,
            "operationsPerSecond": 6569.100490157411
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 0,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LeanLucene_TermQuery|DocumentCount=100000, QueryTerm=said",
          "displayInfo": "TermQueryBenchmarks.LeanLucene_TermQuery: DefaultJob [QueryTerm=said, DocumentCount=100000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LeanLucene_TermQuery",
          "parameters": {
            "QueryTerm": "said",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 673023.4682241586,
            "medianNanoseconds": 673040.0908203125,
            "minNanoseconds": 671226.7373046875,
            "maxNanoseconds": 674259.01171875,
            "standardDeviationNanoseconds": 861.1517285377215,
            "operationsPerSecond": 1485.8322884915178
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 0,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LuceneNet_TermQuery|DocumentCount=100000, QueryTerm=government",
          "displayInfo": "TermQueryBenchmarks.LuceneNet_TermQuery: DefaultJob [QueryTerm=government, DocumentCount=100000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LuceneNet_TermQuery",
          "parameters": {
            "QueryTerm": "government",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 137340.80412409856,
            "medianNanoseconds": 137364.6279296875,
            "minNanoseconds": 136637.68286132812,
            "maxNanoseconds": 137601.33056640625,
            "standardDeviationNanoseconds": 254.94538849626346,
            "operationsPerSecond": 7281.157310659248
          },
          "gc": {
            "bytesAllocatedPerOperation": 60896,
            "gen0Collections": 59,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LuceneNet_TermQuery|DocumentCount=100000, QueryTerm=people",
          "displayInfo": "TermQueryBenchmarks.LuceneNet_TermQuery: DefaultJob [QueryTerm=people, DocumentCount=100000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LuceneNet_TermQuery",
          "parameters": {
            "QueryTerm": "people",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 178446.72270507814,
            "medianNanoseconds": 178496.94848632812,
            "minNanoseconds": 177814.27075195312,
            "maxNanoseconds": 178928.5390625,
            "standardDeviationNanoseconds": 321.46975383261184,
            "operationsPerSecond": 5603.913508978904
          },
          "gc": {
            "bytesAllocatedPerOperation": 58688,
            "gen0Collections": 57,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LuceneNet_TermQuery|DocumentCount=100000, QueryTerm=said",
          "displayInfo": "TermQueryBenchmarks.LuceneNet_TermQuery: DefaultJob [QueryTerm=said, DocumentCount=100000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LuceneNet_TermQuery",
          "parameters": {
            "QueryTerm": "said",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 752397.6984375,
            "medianNanoseconds": 751836.7109375,
            "minNanoseconds": 750095.4912109375,
            "maxNanoseconds": 756240.3916015625,
            "standardDeviationNanoseconds": 1759.3978316727187,
            "operationsPerSecond": 1329.084342066296
          },
          "gc": {
            "bytesAllocatedPerOperation": 58720,
            "gen0Collections": 14,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "schemajson",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SchemaAndJsonBenchmarks-20260501-194636",
      "benchmarkCount": 3,
      "benchmarks": [
        {
          "key": "SchemaAndJsonBenchmarks.LeanLucene_Index_NoSchema|DocumentCount=100000",
          "displayInfo": "SchemaAndJsonBenchmarks.LeanLucene_Index_NoSchema: DefaultJob [DocumentCount=100000]",
          "typeName": "SchemaAndJsonBenchmarks",
          "methodName": "LeanLucene_Index_NoSchema",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 9155249194.333334,
            "medianNanoseconds": 9153905676,
            "minNanoseconds": 9101779398,
            "maxNanoseconds": 9208759722,
            "standardDeviationNanoseconds": 32287584.640044693,
            "operationsPerSecond": 0.10922695589967694
          },
          "gc": {
            "bytesAllocatedPerOperation": 954253496,
            "gen0Collections": 147,
            "gen1Collections": 68,
            "gen2Collections": 2
          }
        },
        {
          "key": "SchemaAndJsonBenchmarks.LeanLucene_Index_WithSchema|DocumentCount=100000",
          "displayInfo": "SchemaAndJsonBenchmarks.LeanLucene_Index_WithSchema: DefaultJob [DocumentCount=100000]",
          "typeName": "SchemaAndJsonBenchmarks",
          "methodName": "LeanLucene_Index_WithSchema",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 9250403994.733334,
            "medianNanoseconds": 9244031732,
            "minNanoseconds": 9211194968,
            "maxNanoseconds": 9300267029,
            "standardDeviationNanoseconds": 24678890.859557386,
            "operationsPerSecond": 0.10810338668120273
          },
          "gc": {
            "bytesAllocatedPerOperation": 958243528,
            "gen0Collections": 146,
            "gen1Collections": 64,
            "gen2Collections": 2
          }
        },
        {
          "key": "SchemaAndJsonBenchmarks.LeanLucene_JsonMapping|DocumentCount=100000",
          "displayInfo": "SchemaAndJsonBenchmarks.LeanLucene_JsonMapping: DefaultJob [DocumentCount=100000]",
          "typeName": "SchemaAndJsonBenchmarks",
          "methodName": "LeanLucene_JsonMapping",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 431681439.06666666,
            "medianNanoseconds": 432464748,
            "minNanoseconds": 428338515,
            "maxNanoseconds": 433797046,
            "standardDeviationNanoseconds": 1831810.4829730403,
            "operationsPerSecond": 2.3165230410695634
          },
          "gc": {
            "bytesAllocatedPerOperation": 223164856,
            "gen0Collections": 50,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "suggester",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SuggesterBenchmarks-20260501-194259",
      "benchmarkCount": 3,
      "benchmarks": [
        {
          "key": "SuggesterBenchmarks.LeanLucene_DidYouMean|DocumentCount=100000",
          "displayInfo": "SuggesterBenchmarks.LeanLucene_DidYouMean: DefaultJob [DocumentCount=100000]",
          "typeName": "SuggesterBenchmarks",
          "methodName": "LeanLucene_DidYouMean",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 4648351.602120535,
            "medianNanoseconds": 4645071.81640625,
            "minNanoseconds": 4620990.7109375,
            "maxNanoseconds": 4683236.8125,
            "standardDeviationNanoseconds": 15843.413098936084,
            "operationsPerSecond": 215.13002578028073
          },
          "gc": {
            "bytesAllocatedPerOperation": 25512,
            "gen0Collections": 0,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "SuggesterBenchmarks.LeanLucene_SpellIndex|DocumentCount=100000",
          "displayInfo": "SuggesterBenchmarks.LeanLucene_SpellIndex: DefaultJob [DocumentCount=100000]",
          "typeName": "SuggesterBenchmarks",
          "methodName": "LeanLucene_SpellIndex",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 4697371.561383928,
            "medianNanoseconds": 4696587.4140625,
            "minNanoseconds": 4660008.65625,
            "maxNanoseconds": 4754086.3046875,
            "standardDeviationNanoseconds": 23958.469581729223,
            "operationsPerSecond": 212.8850117416265
          },
          "gc": {
            "bytesAllocatedPerOperation": 23752,
            "gen0Collections": 0,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "SuggesterBenchmarks.LuceneNet_SpellChecker|DocumentCount=100000",
          "displayInfo": "SuggesterBenchmarks.LuceneNet_SpellChecker: DefaultJob [DocumentCount=100000]",
          "typeName": "SuggesterBenchmarks",
          "methodName": "LuceneNet_SpellChecker",
          "parameters": {
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 10241707.224330356,
            "medianNanoseconds": 10238062.75,
            "minNanoseconds": 10201087.90625,
            "maxNanoseconds": 10283688.6875,
            "standardDeviationNanoseconds": 22067.687497897645,
            "operationsPerSecond": 97.63997135403214
          },
          "gc": {
            "bytesAllocatedPerOperation": 5479576,
            "gen0Collections": 83,
            "gen1Collections": 2,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "wildcard",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.WildcardQueryBenchmarks-20260501-192857",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery|DocumentCount=100000, WildcardPattern=gov*",
          "displayInfo": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery: DefaultJob [WildcardPattern=gov*, DocumentCount=100000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LeanLucene_WildcardQuery",
          "parameters": {
            "WildcardPattern": "gov*",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 121448.37556966145,
            "medianNanoseconds": 121125.25158691406,
            "minNanoseconds": 120461.00317382812,
            "maxNanoseconds": 122783.76354980469,
            "standardDeviationNanoseconds": 712.1970786893261,
            "operationsPerSecond": 8233.951218445165
          },
          "gc": {
            "bytesAllocatedPerOperation": 24965,
            "gen0Collections": 50,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery|DocumentCount=100000, WildcardPattern=m*rket",
          "displayInfo": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery: DefaultJob [WildcardPattern=m*rket, DocumentCount=100000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LeanLucene_WildcardQuery",
          "parameters": {
            "WildcardPattern": "m*rket",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 526624.2489583333,
            "medianNanoseconds": 525986.0576171875,
            "minNanoseconds": 513290.890625,
            "maxNanoseconds": 541037.5419921875,
            "standardDeviationNanoseconds": 8891.770716142826,
            "operationsPerSecond": 1898.8871134931737
          },
          "gc": {
            "bytesAllocatedPerOperation": 10417,
            "gen0Collections": 2,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery|DocumentCount=100000, WildcardPattern=pre*dent",
          "displayInfo": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery: DefaultJob [WildcardPattern=pre*dent, DocumentCount=100000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LeanLucene_WildcardQuery",
          "parameters": {
            "WildcardPattern": "pre*dent",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 12,
            "meanNanoseconds": 93667.00161743164,
            "medianNanoseconds": 93689.43200683594,
            "minNanoseconds": 93146.38830566406,
            "maxNanoseconds": 94095.82495117188,
            "standardDeviationNanoseconds": 289.423072001694,
            "operationsPerSecond": 10676.118405971241
          },
          "gc": {
            "bytesAllocatedPerOperation": 8818,
            "gen0Collections": 17,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery|DocumentCount=100000, WildcardPattern=gov*",
          "displayInfo": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery: DefaultJob [WildcardPattern=gov*, DocumentCount=100000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LuceneNet_WildcardQuery",
          "parameters": {
            "WildcardPattern": "gov*",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 204488.21454729352,
            "medianNanoseconds": 204543.04125976562,
            "minNanoseconds": 203700.58129882812,
            "maxNanoseconds": 205237.08837890625,
            "standardDeviationNanoseconds": 419.4751247895441,
            "operationsPerSecond": 4890.257378469713
          },
          "gc": {
            "bytesAllocatedPerOperation": 132160,
            "gen0Collections": 129,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery|DocumentCount=100000, WildcardPattern=m*rket",
          "displayInfo": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery: DefaultJob [WildcardPattern=m*rket, DocumentCount=100000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LuceneNet_WildcardQuery",
          "parameters": {
            "WildcardPattern": "m*rket",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 1174392.40625,
            "medianNanoseconds": 1171348.27734375,
            "minNanoseconds": 1166599.73828125,
            "maxNanoseconds": 1190406.853515625,
            "standardDeviationNanoseconds": 7883.298512066206,
            "operationsPerSecond": 851.504143485686
          },
          "gc": {
            "bytesAllocatedPerOperation": 414224,
            "gen0Collections": 50,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery|DocumentCount=100000, WildcardPattern=pre*dent",
          "displayInfo": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery: DefaultJob [WildcardPattern=pre*dent, DocumentCount=100000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LuceneNet_WildcardQuery",
          "parameters": {
            "WildcardPattern": "pre*dent",
            "DocumentCount": "100000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 409965.2988630022,
            "medianNanoseconds": 409940.79833984375,
            "minNanoseconds": 408838.67041015625,
            "maxNanoseconds": 411209.771484375,
            "standardDeviationNanoseconds": 629.950892964672,
            "operationsPerSecond": 2439.2308392281006
          },
          "gc": {
            "bytesAllocatedPerOperation": 388608,
            "gen0Collections": 190,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    }
  ]
}</code></pre>

</details>

