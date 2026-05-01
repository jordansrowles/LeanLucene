---
title: Benchmarks - debian
---

# Benchmarks: debian

**.NET** 10.0.3 &nbsp;&middot;&nbsp; **Commit** `24ce653` &nbsp;&middot;&nbsp; 30 April 2026 09:14 UTC &nbsp;&middot;&nbsp; 76 benchmarks

## Analysis

| Method             | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1      | Allocated | Alloc Ratio |
|------------------- |-------------- |--------:|---------:|---------:|------:|------------:|----------:|----------:|------------:|
| LeanLucene_Analyse | 100000        | 1.542 s | 0.0036 s | 0.0034 s |  1.00 |  48000.0000 | 2000.0000 | 197.07 MB |        1.00 |
| LuceneNet_Analyse  | 100000        | 2.268 s | 0.0019 s | 0.0017 s |  1.47 | 144000.0000 |         - | 576.92 MB |        2.93 |

## Block-Join

| Method                           | BlockCount | Mean          | Error       | StdDev      | Ratio | Gen0      | Gen1     | Allocated  | Alloc Ratio |
|--------------------------------- |----------- |--------------:|------------:|------------:|------:|----------:|---------:|-----------:|------------:|
| LeanLucene_IndexBlocks           | 500        | 65,854.334 Î¼s | 516.8797 Î¼s | 483.4896 Î¼s | 1.000 | 1375.0000 | 625.0000 | 10729212 B |       1.000 |
| LeanLucene_BlockJoinQuery        | 500        |      6.972 Î¼s |   0.0151 Î¼s |   0.0141 Î¼s | 0.000 |    0.1678 |        - |      720 B |       0.000 |
| LuceneNet_IndexBlocks            | 500        | 55,594.445 Î¼s | 227.3357 Î¼s | 189.8356 Î¼s | 0.844 | 5000.0000 | 555.5556 | 28715379 B |       2.676 |
| LuceneNet_ToParentBlockJoinQuery | 500        |     21.599 Î¼s |   0.0509 Î¼s |   0.0476 Î¼s | 0.000 |    3.0518 |        - |    12888 B |       0.001 |

## Boolean queries

| Method                  | BooleanType | DocumentCount | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------------------------ |------------ |-------------- |---------:|--------:|--------:|------:|--------:|---------:|--------:|----------:|------------:|
| **LeanLucene_BooleanQuery** | **Must**        | **100000**        | **265.5 Î¼s** | **2.85 Î¼s** | **2.52 Î¼s** |  **1.00** |    **0.00** |   **2.9297** |       **-** |  **12.94 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Must        | 100000        | 487.8 Î¼s | 0.64 Î¼s | 0.59 Î¼s |  1.84 |    0.02 |  35.1563 |       - | 144.09 KB |       11.14 |
|                         |             |               |          |         |         |       |         |          |         |           |             |
| **LeanLucene_BooleanQuery** | **MustNot**     | **100000**        | **173.8 Î¼s** | **1.76 Î¼s** | **1.47 Î¼s** |  **1.00** |    **0.00** |   **3.1738** |       **-** |  **13.29 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | MustNot     | 100000        | 409.3 Î¼s | 0.75 Î¼s | 0.70 Î¼s |  2.35 |    0.02 |  36.1328 |       - | 149.06 KB |       11.21 |
|                         |             |               |          |         |         |       |         |          |         |           |             |
| **LeanLucene_BooleanQuery** | **Should**      | **100000**        | **222.0 Î¼s** | **1.82 Î¼s** | **1.71 Î¼s** |  **1.00** |    **0.00** |   **3.1738** |       **-** |  **13.69 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Should      | 100000        | 581.7 Î¼s | 0.94 Î¼s | 0.79 Î¼s |  2.62 |    0.02 | 169.9219 | 40.0391 | 695.01 KB |       50.76 |

## Deletion

| Method                     | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|--------------------------- |-------------- |--------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_DeleteDocuments | 100000        | 9.435 s | 0.0328 s | 0.0291 s |  1.00 | 149000.0000 | 70000.0000 | 5000.0000 | 965.25 MB |        1.00 |
| LuceneNet_DeleteDocuments  | 100000        | 7.192 s | 0.0211 s | 0.0198 s |  0.76 | 338000.0000 | 33000.0000 | 1000.0000 |   1960 MB |        2.03 |

## Fuzzy queries

| Method                | QueryTerm | DocumentCount | Mean     | Error     | StdDev    | Ratio | Gen0     | Gen1     | Allocated  | Alloc Ratio |
|---------------------- |---------- |-------------- |---------:|----------:|----------:|------:|---------:|---------:|-----------:|------------:|
| **LeanLucene_FuzzyQuery** | **goverment** | **100000**        | **6.937 ms** | **0.0314 ms** | **0.0245 ms** |  **1.00** |        **-** |        **-** |   **25.88 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | goverment | 100000        | 8.693 ms | 0.0323 ms | 0.0302 ms |  1.25 | 593.7500 | 203.1250 | 2870.85 KB |      110.94 |
|                       |           |               |          |           |           |       |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **markts**    | **100000**        | **7.555 ms** | **0.0677 ms** | **0.0634 ms** |  **1.00** |   **7.8125** |        **-** |   **47.67 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | markts    | 100000        | 9.261 ms | 0.0227 ms | 0.0213 ms |  1.23 | 625.0000 | 187.5000 | 2806.02 KB |       58.87 |
|                       |           |               |          |           |           |       |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **presiden**  | **100000**        | **8.020 ms** | **0.0546 ms** | **0.0484 ms** |  **1.00** |        **-** |        **-** |   **30.61 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | presiden  | 100000        | 8.679 ms | 0.0275 ms | 0.0257 ms |  1.08 | 593.7500 | 218.7500 | 2844.58 KB |       92.93 |

## gutenberg-analysis

| Method                      | Mean     | Error   | StdDev   | Median   | Ratio | RatioSD | Gen0       | Gen1      | Gen2      | Allocated | Alloc Ratio |
|---------------------------- |---------:|--------:|---------:|---------:|------:|--------:|-----------:|----------:|----------:|----------:|------------:|
| LeanLucene_Standard_Analyse | 118.2 ms | 0.50 ms |  0.44 ms | 118.4 ms |  1.00 |    0.00 |  1200.0000 |  800.0000 |         - |   7.08 MB |        1.00 |
| LeanLucene_English_Analyse  | 355.9 ms | 7.09 ms | 13.49 ms | 361.8 ms |  3.01 |    0.11 | 11000.0000 | 7000.0000 | 2000.0000 | 111.87 MB |       15.80 |

## gutenberg-index

| Method                    | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0       | Gen1       | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |---------:|---------:|---------:|------:|--------:|-----------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Standard_Index | 727.6 ms | 10.71 ms | 10.02 ms |  1.00 |    0.00 | 11000.0000 |  6000.0000 | 1000.0000 |  76.53 MB |        1.00 |
| LeanLucene_English_Index  | 733.2 ms |  5.48 ms |  5.13 ms |  1.01 |    0.02 | 28000.0000 | 10000.0000 | 1000.0000 | 169.78 MB |        2.22 |
| LuceneNet_Index           | 656.6 ms |  5.70 ms |  5.06 ms |  0.90 |    0.01 | 41000.0000 |  3000.0000 |         - | 207.68 MB |        2.71 |

## gutenberg-search

| Method                     | SearchTerm | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| **LeanLucene_Standard_Search** | **death**      | **11.31 Î¼s** | **0.026 Î¼s** | **0.025 Î¼s** |  **1.00** |    **0.00** | **0.1068** |      **-** |     **472 B** |        **1.00** |
| LeanLucene_English_Search  | death      | 11.51 Î¼s | 0.022 Î¼s | 0.021 Î¼s |  1.02 |    0.00 | 0.1068 |      - |     472 B |        1.00 |
| LuceneNet_Search           | death      | 22.87 Î¼s | 0.271 Î¼s | 0.253 Î¼s |  2.02 |    0.02 | 2.6550 | 0.0305 |   11231 B |       23.79 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **love**       | **15.46 Î¼s** | **0.040 Î¼s** | **0.037 Î¼s** |  **1.00** |    **0.00** | **0.0916** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | love       | 20.57 Î¼s | 0.042 Î¼s | 0.039 Î¼s |  1.33 |    0.00 | 0.0916 |      - |     464 B |        1.00 |
| LuceneNet_Search           | love       | 30.68 Î¼s | 0.057 Î¼s | 0.051 Î¼s |  1.98 |    0.01 | 2.6245 | 0.0610 |   11175 B |       24.08 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **man**        | **39.60 Î¼s** | **0.091 Î¼s** | **0.081 Î¼s** |  **1.00** |    **0.00** | **0.0610** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | man        | 39.59 Î¼s | 0.061 Î¼s | 0.057 Î¼s |  1.00 |    0.00 | 0.0610 |      - |     464 B |        1.00 |
| LuceneNet_Search           | man        | 49.75 Î¼s | 0.251 Î¼s | 0.235 Î¼s |  1.26 |    0.01 | 2.6245 | 0.0610 |   11038 B |       23.79 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **night**      | **25.33 Î¼s** | **0.054 Î¼s** | **0.050 Î¼s** |  **1.00** |    **0.00** | **0.0916** |      **-** |     **472 B** |        **1.00** |
| LeanLucene_English_Search  | night      | 26.26 Î¼s | 0.072 Î¼s | 0.063 Î¼s |  1.04 |    0.00 | 0.0916 |      - |     472 B |        1.00 |
| LuceneNet_Search           | night      | 35.94 Î¼s | 0.074 Î¼s | 0.069 Î¼s |  1.42 |    0.00 | 2.6245 | 0.0610 |   11223 B |       23.78 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **sea**        | **12.84 Î¼s** | **0.024 Î¼s** | **0.023 Î¼s** |  **1.00** |    **0.00** | **0.1068** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | sea        | 14.24 Î¼s | 0.023 Î¼s | 0.022 Î¼s |  1.11 |    0.00 | 0.1068 |      - |     464 B |        1.00 |
| LuceneNet_Search           | sea        | 26.18 Î¼s | 0.090 Î¼s | 0.084 Î¼s |  2.04 |    0.01 | 2.6550 | 0.0305 |   11271 B |       24.29 |

## Indexing

| Method                    | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |--------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_IndexDocuments | 100000        | 9.386 s | 0.0514 s | 0.0455 s |  1.00 | 151000.0000 | 71000.0000 | 6000.0000 | 910.01 MB |        1.00 |
| LuceneNet_IndexDocuments  | 100000        | 6.942 s | 0.0281 s | 0.0234 s |  0.74 | 330000.0000 | 30000.0000 | 1000.0000 | 1925.7 MB |        2.12 |

## Index-sort (index)

| Method                    | DocumentCount | Mean     | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |---------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Index_Unsorted | 100000        |  9.493 s | 0.0439 s | 0.0410 s |  1.00 | 154000.0000 | 72000.0000 | 6000.0000 | 942.12 MB |        1.00 |
| LeanLucene_Index_Sorted   | 100000        | 10.305 s | 0.0439 s | 0.0411 s |  1.09 | 156000.0000 | 71000.0000 | 6000.0000 |  952.2 MB |        1.01 |

## Index-sort (search)

| Method                                   | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0    | Allocated | Alloc Ratio |
|----------------------------------------- |-------------- |---------:|--------:|--------:|------:|--------:|----------:|------------:|
| LeanLucene_SortedSearch_EarlyTermination | 100000        | 315.5 Î¼s | 0.66 Î¼s | 0.61 Î¼s |  1.00 | 25.3906 | 105.73 KB |        1.00 |
| LeanLucene_SortedSearch_PostSort         | 100000        | 313.6 Î¼s | 0.73 Î¼s | 0.69 Î¼s |  0.99 | 25.3906 | 105.73 KB |        1.00 |

## Phrase queries

| Method                 | PhraseType     | DocumentCount | Mean       | Error   | StdDev  | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------------------- |--------------- |-------------- |-----------:|--------:|--------:|------:|--------:|--------:|--------:|----------:|------------:|
| **LeanLucene_PhraseQuery** | **ExactThreeWord** | **100000**        |   **430.7 Î¼s** | **4.15 Î¼s** | **3.47 Î¼s** |  **1.00** |    **0.00** | **14.6484** |       **-** |  **59.77 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactThreeWord | 100000        |   350.2 Î¼s | 0.86 Î¼s | 0.81 Î¼s |  0.81 |    0.01 | 90.3320 |  0.4883 | 369.88 KB |        6.19 |
|                        |                |               |            |         |         |       |         |         |         |           |             |
| **LeanLucene_PhraseQuery** | **ExactTwoWord**   | **100000**        |   **335.2 Î¼s** | **4.98 Î¼s** | **4.41 Î¼s** |  **1.00** |    **0.00** | **10.2539** |       **-** |  **42.92 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactTwoWord   | 100000        |   406.7 Î¼s | 0.62 Î¼s | 0.58 Î¼s |  1.21 |    0.02 | 72.2656 | 18.0664 | 297.27 KB |        6.93 |
|                        |                |               |            |         |         |       |         |         |         |           |             |
| **LeanLucene_PhraseQuery** | **SlopTwoWord**    | **100000**        |   **983.4 Î¼s** | **9.15 Î¼s** | **8.56 Î¼s** |  **1.00** |    **0.00** | **11.7188** |       **-** |  **48.72 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | SlopTwoWord    | 100000        | 1,034.9 Î¼s | 3.13 Î¼s | 2.78 Î¼s |  1.05 |    0.01 | 37.1094 |       - | 155.61 KB |        3.19 |

## Prefix queries

| Method                 | QueryPrefix | DocumentCount | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-------------- |---------:|--------:|--------:|------:|--------:|--------:|-------:|----------:|------------:|
| **LeanLucene_PrefixQuery** | **gov**         | **100000**        | **119.7 Î¼s** | **0.68 Î¼s** | **0.53 Î¼s** |  **1.00** |    **0.00** |  **5.8594** |      **-** |  **23.68 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | gov         | 100000        | 190.4 Î¼s | 0.36 Î¼s | 0.33 Î¼s |  1.59 |    0.01 | 26.8555 | 0.2441 | 110.04 KB |        4.65 |
|                        |             |               |          |         |         |       |         |         |        |           |             |
| **LeanLucene_PrefixQuery** | **mark**        | **100000**        | **195.6 Î¼s** | **1.84 Î¼s** | **1.72 Î¼s** |  **1.00** |    **0.00** |  **8.5449** |      **-** |  **34.56 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | mark        | 100000        | 287.4 Î¼s | 0.99 Î¼s | 0.92 Î¼s |  1.47 |    0.01 | 30.7617 |      - | 126.09 KB |        3.65 |
|                        |             |               |          |         |         |       |         |         |        |           |             |
| **LeanLucene_PrefixQuery** | **pres**        | **100000**        | **247.4 Î¼s** | **3.05 Î¼s** | **2.70 Î¼s** |  **1.00** |    **0.00** | **15.6250** | **0.4883** |  **62.96 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | pres        | 100000        | 354.6 Î¼s | 0.78 Î¼s | 0.73 Î¼s |  1.43 |    0.02 | 32.2266 |      - | 133.65 KB |        2.12 |

## Term queries

| Method               | QueryTerm  | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0    | Gen1   | Allocated | Alloc Ratio |
|--------------------- |----------- |-------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| **LeanLucene_TermQuery** | **government** | **100000**        | **106.5 Î¼s** | **0.19 Î¼s** | **0.18 Î¼s** |  **1.00** |       **-** |      **-** |     **480 B** |        **1.00** |
| LuceneNet_TermQuery  | government | 100000        | 136.9 Î¼s | 0.40 Î¼s | 0.37 Î¼s |  1.29 | 14.4043 |      - |   60896 B |      126.87 |
|                      |            |               |          |         |         |       |         |        |           |             |
| **LeanLucene_TermQuery** | **people**     | **100000**        | **149.4 Î¼s** | **0.35 Î¼s** | **0.33 Î¼s** |  **1.00** |       **-** |      **-** |     **472 B** |        **1.00** |
| LuceneNet_TermQuery  | people     | 100000        | 176.7 Î¼s | 0.23 Î¼s | 0.21 Î¼s |  1.18 | 13.9160 | 0.2441 |   58688 B |      124.34 |
|                      |            |               |          |         |         |       |         |        |           |             |
| **LeanLucene_TermQuery** | **said**       | **100000**        | **681.1 Î¼s** | **1.35 Î¼s** | **1.26 Î¼s** |  **1.00** |       **-** |      **-** |     **464 B** |        **1.00** |
| LuceneNet_TermQuery  | said       | 100000        | 755.0 Î¼s | 1.76 Î¼s | 1.64 Î¼s |  1.11 | 13.6719 |      - |   58720 B |      126.55 |

## Schema and JSON

| Method                      | DocumentCount | Mean       | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|---------------------------- |-------------- |-----------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Index_NoSchema   | 100000        | 9,167.2 ms | 36.76 ms | 32.59 ms |  1.00 | 147000.0000 | 68000.0000 | 2000.0000 | 910.04 MB |        1.00 |
| LeanLucene_Index_WithSchema | 100000        | 9,307.9 ms | 42.82 ms | 40.06 ms |  1.02 | 146000.0000 | 64000.0000 | 2000.0000 | 913.85 MB |        1.00 |
| LeanLucene_JsonMapping      | 100000        |   420.9 ms |  1.44 ms |  1.34 ms |  0.05 |  50000.0000 |  1000.0000 |         - | 212.83 MB |        0.23 |

## Suggester

| Method                 | DocumentCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1    | Allocated  | Alloc Ratio |
|----------------------- |-------------- |----------:|----------:|----------:|------:|--------:|----------:|--------:|-----------:|------------:|
| LeanLucene_DidYouMean  | 100000        |  4.620 ms | 0.0424 ms | 0.0396 ms |  1.00 |    0.00 |         - |       - |   24.91 KB |        1.00 |
| LeanLucene_SpellIndex  | 100000        |  4.700 ms | 0.0489 ms | 0.0457 ms |  1.02 |    0.01 |         - |       - |    23.2 KB |        0.93 |
| LuceneNet_SpellChecker | 100000        | 10.151 ms | 0.0212 ms | 0.0188 ms |  2.20 |    0.02 | 1296.8750 | 31.2500 | 5351.15 KB |      214.78 |

## Wildcard queries

| Method                   | WildcardPattern | DocumentCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0     | Gen1   | Allocated  | Alloc Ratio |
|------------------------- |---------------- |-------------- |-----------:|---------:|---------:|------:|--------:|---------:|-------:|-----------:|------------:|
| **LeanLucene_WildcardQuery** | **gov***            | **100000**        |   **125.2 Î¼s** |  **1.02 Î¼s** |  **0.90 Î¼s** |  **1.00** |    **0.00** |   **7.5684** |      **-** |   **30.55 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | gov*            | 100000        |   203.9 Î¼s |  0.52 Î¼s |  0.49 Î¼s |  1.63 |    0.01 |  31.4941 |      - |  129.06 KB |        4.23 |
|                          |                 |               |            |          |          |       |         |          |        |            |             |
| **LeanLucene_WildcardQuery** | **m*rket**          | **100000**        | **1,176.9 Î¼s** | **22.87 Î¼s** | **22.47 Î¼s** |  **1.00** |    **0.00** | **371.0938** |      **-** | **1510.77 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | m*rket          | 100000        | 1,173.4 Î¼s |  2.57 Î¼s |  2.40 Î¼s |  1.00 |    0.02 |  97.6563 | 9.7656 |  404.52 KB |        0.27 |
|                          |                 |               |            |          |          |       |         |          |        |            |             |
| **LeanLucene_WildcardQuery** | **pre*dent**        | **100000**        |   **152.6 Î¼s** |  **1.75 Î¼s** |  **1.63 Î¼s** |  **1.00** |    **0.00** |  **33.2031** |      **-** |  **135.15 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | pre*dent        | 100000        |   407.8 Î¼s |  0.69 Î¼s |  0.62 Î¼s |  2.67 |    0.03 |  92.7734 |      - |   379.5 KB |        2.81 |

<details>
<summary>Full data (report.json)</summary>

<pre><code class="lang-json">{
  "schemaVersion": 2,
  "runId": "2026-04-30 09-14 (24ce653)",
  "runType": "full",
  "generatedAtUtc": "2026-04-30T09:14:58.9463559\u002B00:00",
  "commandLineArgs": [],
  "hostMachineName": "debian",
  "commitHash": "24ce653",
  "dotnetVersion": "10.0.3",
  "provenance": {
    "sourceCommit": "24ce653",
    "sourceRef": "",
    "sourceManifestPath": "",
    "gitCommitHash": "24ce653",
    "gitAvailable": true,
    "gitDirty": true,
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.AnalysisBenchmarks-20260430-103005",
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
            "sampleCount": 15,
            "meanNanoseconds": 1542138516,
            "medianNanoseconds": 1540383347,
            "minNanoseconds": 1537894234,
            "maxNanoseconds": 1550039667,
            "standardDeviationNanoseconds": 3351040.753443814,
            "operationsPerSecond": 0.6484501811120059
          },
          "gc": {
            "bytesAllocatedPerOperation": 206647088,
            "gen0Collections": 48,
            "gen1Collections": 2,
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
            "sampleCount": 14,
            "meanNanoseconds": 2268394963.5,
            "medianNanoseconds": 2268645291.5,
            "minNanoseconds": 2265378288,
            "maxNanoseconds": 2271584044,
            "standardDeviationNanoseconds": 1707151.4066561628,
            "operationsPerSecond": 0.44084033693015207
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BlockJoinBenchmarks-20260430-113328",
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
            "sampleCount": 15,
            "meanNanoseconds": 6971.879746500651,
            "medianNanoseconds": 6974.6550216674805,
            "minNanoseconds": 6943.185157775879,
            "maxNanoseconds": 6994.177665710449,
            "standardDeviationNanoseconds": 14.146301075899133,
            "operationsPerSecond": 143433.34026980647
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
            "sampleCount": 15,
            "meanNanoseconds": 65854333.55,
            "medianNanoseconds": 65709941.375,
            "minNanoseconds": 65136860.375,
            "maxNanoseconds": 66701849,
            "standardDeviationNanoseconds": 483489.55221843574,
            "operationsPerSecond": 15.1850295355392
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
            "sampleCount": 13,
            "meanNanoseconds": 55594444.56410256,
            "medianNanoseconds": 55666349.333333336,
            "minNanoseconds": 55154273.222222224,
            "maxNanoseconds": 55805129.44444445,
            "standardDeviationNanoseconds": 189835.5851910377,
            "operationsPerSecond": 17.98740877511531
          },
          "gc": {
            "bytesAllocatedPerOperation": 28715379,
            "gen0Collections": 45,
            "gen1Collections": 5,
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
            "meanNanoseconds": 21599.131510416668,
            "medianNanoseconds": 21596.089447021484,
            "minNanoseconds": 21504.990997314453,
            "maxNanoseconds": 21673.992767333984,
            "standardDeviationNanoseconds": 47.57072565681551,
            "operationsPerSecond": 46298.15784573225
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BooleanQueryBenchmarks-20260430-103310",
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
            "sampleCount": 14,
            "meanNanoseconds": 265461.56483677455,
            "medianNanoseconds": 265147.1748046875,
            "minNanoseconds": 262163.81298828125,
            "maxNanoseconds": 271178.90576171875,
            "standardDeviationNanoseconds": 2522.2542483926977,
            "operationsPerSecond": 3767.0236767227457
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
            "sampleCount": 13,
            "meanNanoseconds": 173847.59003155047,
            "medianNanoseconds": 173656.22583007812,
            "minNanoseconds": 172398.2353515625,
            "maxNanoseconds": 177238.45532226562,
            "standardDeviationNanoseconds": 1473.0179425681451,
            "operationsPerSecond": 5752.164869346285
          },
          "gc": {
            "bytesAllocatedPerOperation": 13611,
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
            "sampleCount": 15,
            "meanNanoseconds": 222027.6842936198,
            "medianNanoseconds": 221365.4736328125,
            "minNanoseconds": 220191.38696289062,
            "maxNanoseconds": 225424.51586914062,
            "standardDeviationNanoseconds": 1705.290453485338,
            "operationsPerSecond": 4503.9428447019845
          },
          "gc": {
            "bytesAllocatedPerOperation": 14021,
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
            "meanNanoseconds": 487799.9126953125,
            "medianNanoseconds": 487667.49951171875,
            "minNanoseconds": 486925.72412109375,
            "maxNanoseconds": 488854.7392578125,
            "standardDeviationNanoseconds": 594.1926920067496,
            "operationsPerSecond": 2050.020867110355
          },
          "gc": {
            "bytesAllocatedPerOperation": 147552,
            "gen0Collections": 72,
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
            "meanNanoseconds": 409272.4467122396,
            "medianNanoseconds": 409336.7998046875,
            "minNanoseconds": 407661.55712890625,
            "maxNanoseconds": 410591.14697265625,
            "standardDeviationNanoseconds": 701.2675271971731,
            "operationsPerSecond": 2443.360182277558
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
            "sampleCount": 13,
            "meanNanoseconds": 581745.0684344952,
            "medianNanoseconds": 581723.8740234375,
            "minNanoseconds": 580318.7470703125,
            "maxNanoseconds": 583458.2451171875,
            "standardDeviationNanoseconds": 787.8877139132584,
            "operationsPerSecond": 1718.9660115057777
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.DeletionBenchmarks-20260430-105859",
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
            "meanNanoseconds": 9435244578.214285,
            "medianNanoseconds": 9441827207,
            "minNanoseconds": 9365561516,
            "maxNanoseconds": 9462419905,
            "standardDeviationNanoseconds": 29070302.74091977,
            "operationsPerSecond": 0.1059855938773407
          },
          "gc": {
            "bytesAllocatedPerOperation": 1012137256,
            "gen0Collections": 149,
            "gen1Collections": 70,
            "gen2Collections": 5
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
            "sampleCount": 15,
            "meanNanoseconds": 7192126355.466666,
            "medianNanoseconds": 7191399458,
            "minNanoseconds": 7161865883,
            "maxNanoseconds": 7224426439,
            "standardDeviationNanoseconds": 19755514.555510104,
            "operationsPerSecond": 0.1390409387398915
          },
          "gc": {
            "bytesAllocatedPerOperation": 2055204352,
            "gen0Collections": 338,
            "gen1Collections": 33,
            "gen2Collections": 1
          }
        }
      ]
    },
    {
      "suiteName": "fuzzy",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.FuzzyQueryBenchmarks-20260430-104844",
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
            "sampleCount": 12,
            "meanNanoseconds": 6937101.216796875,
            "medianNanoseconds": 6944765.55078125,
            "minNanoseconds": 6880093.4375,
            "maxNanoseconds": 6959000.7578125,
            "standardDeviationNanoseconds": 24488.946050816478,
            "operationsPerSecond": 144.15243035213174
          },
          "gc": {
            "bytesAllocatedPerOperation": 26499,
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
            "meanNanoseconds": 7555429.7671875,
            "medianNanoseconds": 7527272.109375,
            "minNanoseconds": 7500416.0546875,
            "maxNanoseconds": 7687531.4296875,
            "standardDeviationNanoseconds": 63353.07482703497,
            "operationsPerSecond": 132.35514468586592
          },
          "gc": {
            "bytesAllocatedPerOperation": 48810,
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
            "sampleCount": 14,
            "meanNanoseconds": 8019596.768973215,
            "medianNanoseconds": 8013868.5703125,
            "minNanoseconds": 7961397.515625,
            "maxNanoseconds": 8109961.109375,
            "standardDeviationNanoseconds": 48404.87154245672,
            "operationsPerSecond": 124.69454871707154
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
            "meanNanoseconds": 8693231.74375,
            "medianNanoseconds": 8691397.734375,
            "minNanoseconds": 8654107.15625,
            "maxNanoseconds": 8758397.1875,
            "standardDeviationNanoseconds": 30234.74462291413,
            "operationsPerSecond": 115.0320191014061
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
            "sampleCount": 15,
            "meanNanoseconds": 9261137.205208333,
            "medianNanoseconds": 9257149.84375,
            "minNanoseconds": 9226267.828125,
            "maxNanoseconds": 9301897.515625,
            "standardDeviationNanoseconds": 21259.173398497005,
            "operationsPerSecond": 107.9781000801515
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
            "sampleCount": 15,
            "meanNanoseconds": 8678936.770833334,
            "medianNanoseconds": 8666121.71875,
            "minNanoseconds": 8653949.484375,
            "maxNanoseconds": 8732888.53125,
            "standardDeviationNanoseconds": 25745.29824075581,
            "operationsPerSecond": 115.22148696377495
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergAnalysisBenchmarks-20260430-113623",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "GutenbergAnalysisBenchmarks.LeanLucene_English_Analyse",
          "displayInfo": "GutenbergAnalysisBenchmarks.LeanLucene_English_Analyse: DefaultJob",
          "typeName": "GutenbergAnalysisBenchmarks",
          "methodName": "LeanLucene_English_Analyse",
          "parameters": {},
          "statistics": {
            "sampleCount": 45,
            "meanNanoseconds": 355927569.9111111,
            "medianNanoseconds": 361751367,
            "minNanoseconds": 326404991,
            "maxNanoseconds": 364851688,
            "standardDeviationNanoseconds": 13490707.843826182,
            "operationsPerSecond": 2.8095603840122267
          },
          "gc": {
            "bytesAllocatedPerOperation": 117302512,
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
            "sampleCount": 14,
            "meanNanoseconds": 118244995.55714285,
            "medianNanoseconds": 118368431.69999999,
            "minNanoseconds": 117297632.8,
            "maxNanoseconds": 118756498.6,
            "standardDeviationNanoseconds": 440143.4161395754,
            "operationsPerSecond": 8.457017527788242
          },
          "gc": {
            "bytesAllocatedPerOperation": 7422736,
            "gen0Collections": 6,
            "gen1Collections": 4,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergIndexingBenchmarks-20260430-113826",
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
            "meanNanoseconds": 733213836.5333333,
            "medianNanoseconds": 733744982,
            "minNanoseconds": 725803253,
            "maxNanoseconds": 742204684,
            "standardDeviationNanoseconds": 5128451.531424538,
            "operationsPerSecond": 1.3638586046439647
          },
          "gc": {
            "bytesAllocatedPerOperation": 178029864,
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
            "meanNanoseconds": 727576435.5333333,
            "medianNanoseconds": 726852135,
            "minNanoseconds": 709410038,
            "maxNanoseconds": 742227757,
            "standardDeviationNanoseconds": 10017959.747120839,
            "operationsPerSecond": 1.3744260412543086
          },
          "gc": {
            "bytesAllocatedPerOperation": 80243248,
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
            "sampleCount": 14,
            "meanNanoseconds": 656561176,
            "medianNanoseconds": 654463015.5,
            "minNanoseconds": 651417798,
            "maxNanoseconds": 667353959,
            "standardDeviationNanoseconds": 5057177.788856335,
            "operationsPerSecond": 1.5230873169996881
          },
          "gc": {
            "bytesAllocatedPerOperation": 217770880,
            "gen0Collections": 41,
            "gen1Collections": 3,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergSearchBenchmarks-20260430-114100",
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
            "meanNanoseconds": 11508.15846862793,
            "medianNanoseconds": 11511.8955078125,
            "minNanoseconds": 11468.646408081055,
            "maxNanoseconds": 11543.352584838867,
            "standardDeviationNanoseconds": 21.02823628646079,
            "operationsPerSecond": 86894.87572890765
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
            "meanNanoseconds": 20573.104189046226,
            "medianNanoseconds": 20569.13247680664,
            "minNanoseconds": 20503.526794433594,
            "maxNanoseconds": 20641.534637451172,
            "standardDeviationNanoseconds": 39.41479855362386,
            "operationsPerSecond": 48607.15188194263
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
            "meanNanoseconds": 39594.83465983073,
            "medianNanoseconds": 39593.292053222656,
            "minNanoseconds": 39474.71112060547,
            "maxNanoseconds": 39687.012145996094,
            "standardDeviationNanoseconds": 56.77892201442959,
            "operationsPerSecond": 25255.819568164727
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
            "sampleCount": 14,
            "meanNanoseconds": 26257.558020455497,
            "medianNanoseconds": 26265.07894897461,
            "minNanoseconds": 26141.080322265625,
            "maxNanoseconds": 26336.651245117188,
            "standardDeviationNanoseconds": 63.412401789185644,
            "operationsPerSecond": 38084.272696682885
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
            "sampleCount": 15,
            "meanNanoseconds": 14240.88544514974,
            "medianNanoseconds": 14236.99966430664,
            "minNanoseconds": 14212.711883544922,
            "maxNanoseconds": 14292.363204956055,
            "standardDeviationNanoseconds": 21.744632747791293,
            "operationsPerSecond": 70220.35278997256
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
            "meanNanoseconds": 11307.896219889322,
            "medianNanoseconds": 11300.460952758789,
            "minNanoseconds": 11272.355911254883,
            "maxNanoseconds": 11356.869598388672,
            "standardDeviationNanoseconds": 24.742782407560718,
            "operationsPerSecond": 88433.77941876686
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
            "meanNanoseconds": 15456.912255859375,
            "medianNanoseconds": 15456.784759521484,
            "minNanoseconds": 15399.890533447266,
            "maxNanoseconds": 15512.998962402344,
            "standardDeviationNanoseconds": 37.239462408325295,
            "operationsPerSecond": 64695.974425352775
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
            "sampleCount": 14,
            "meanNanoseconds": 39602.685939243864,
            "medianNanoseconds": 39593.77651977539,
            "minNanoseconds": 39459.8291015625,
            "maxNanoseconds": 39774.423828125,
            "standardDeviationNanoseconds": 81.04681340327288,
            "operationsPerSecond": 25250.81257200943
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
            "meanNanoseconds": 25334.610329182942,
            "medianNanoseconds": 25333.857788085938,
            "minNanoseconds": 25217.647521972656,
            "maxNanoseconds": 25410.35302734375,
            "standardDeviationNanoseconds": 50.40993934946484,
            "operationsPerSecond": 39471.69453196996
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
            "sampleCount": 15,
            "meanNanoseconds": 12838.73405456543,
            "medianNanoseconds": 12843.90168762207,
            "minNanoseconds": 12793.659759521484,
            "maxNanoseconds": 12878.265853881836,
            "standardDeviationNanoseconds": 22.68531858317938,
            "operationsPerSecond": 77889.29934602095
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
            "meanNanoseconds": 22872.65352783203,
            "medianNanoseconds": 22963.535278320312,
            "minNanoseconds": 22462.676727294922,
            "maxNanoseconds": 23191.809692382812,
            "standardDeviationNanoseconds": 253.44455537942466,
            "operationsPerSecond": 43720.33173952355
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
            "sampleCount": 14,
            "meanNanoseconds": 30675.4916469029,
            "medianNanoseconds": 30678.779510498047,
            "minNanoseconds": 30575.938232421875,
            "maxNanoseconds": 30749.363159179688,
            "standardDeviationNanoseconds": 50.96124847428014,
            "operationsPerSecond": 32599.314511751705
          },
          "gc": {
            "bytesAllocatedPerOperation": 11175,
            "gen0Collections": 43,
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
            "meanNanoseconds": 49753.02168782552,
            "medianNanoseconds": 49853.38836669922,
            "minNanoseconds": 49404.521484375,
            "maxNanoseconds": 50127.93151855469,
            "standardDeviationNanoseconds": 234.65509808878187,
            "operationsPerSecond": 20099.281733569525
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
            "sampleCount": 15,
            "meanNanoseconds": 35936.406030273436,
            "medianNanoseconds": 35956.10125732422,
            "minNanoseconds": 35806.44641113281,
            "maxNanoseconds": 36072.628662109375,
            "standardDeviationNanoseconds": 69.10832798060494,
            "operationsPerSecond": 27826.934033347217
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
            "sampleCount": 15,
            "meanNanoseconds": 26181.723254394532,
            "medianNanoseconds": 26199.19564819336,
            "minNanoseconds": 25967.949493408203,
            "maxNanoseconds": 26302.04852294922,
            "standardDeviationNanoseconds": 83.81145152785452,
            "operationsPerSecond": 38194.58292655174
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexingBenchmarks-20260430-102114",
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
            "sampleCount": 14,
            "meanNanoseconds": 9386351036.142857,
            "medianNanoseconds": 9380476583.5,
            "minNanoseconds": 9310169023,
            "maxNanoseconds": 9479202986,
            "standardDeviationNanoseconds": 45527289.15278117,
            "operationsPerSecond": 0.10653767328213319
          },
          "gc": {
            "bytesAllocatedPerOperation": 954218232,
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
            "sampleCount": 13,
            "meanNanoseconds": 6942274501.153846,
            "medianNanoseconds": 6941874017,
            "minNanoseconds": 6909710402,
            "maxNanoseconds": 6993087439,
            "standardDeviationNanoseconds": 23442523.08093493,
            "operationsPerSecond": 0.14404501000843373
          },
          "gc": {
            "bytesAllocatedPerOperation": 2019247480,
            "gen0Collections": 330,
            "gen1Collections": 30,
            "gen2Collections": 1
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortIndexBenchmarks-20260430-112054",
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
            "meanNanoseconds": 10305006969.4,
            "medianNanoseconds": 10313566652,
            "minNanoseconds": 10221776614,
            "maxNanoseconds": 10366987484,
            "standardDeviationNanoseconds": 41104133.59452173,
            "operationsPerSecond": 0.09704020608325936
          },
          "gc": {
            "bytesAllocatedPerOperation": 998455984,
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
            "meanNanoseconds": 9492697834.2,
            "medianNanoseconds": 9502033140,
            "minNanoseconds": 9443562381,
            "maxNanoseconds": 9567676617,
            "standardDeviationNanoseconds": 41024573.84038899,
            "operationsPerSecond": 0.10534413055867328
          },
          "gc": {
            "bytesAllocatedPerOperation": 987887448,
            "gen0Collections": 154,
            "gen1Collections": 72,
            "gen2Collections": 6
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortSearchBenchmarks-20260430-113103",
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
            "sampleCount": 15,
            "meanNanoseconds": 315464.6940104167,
            "medianNanoseconds": 315541.82666015625,
            "minNanoseconds": 314497.81298828125,
            "maxNanoseconds": 316467.02001953125,
            "standardDeviationNanoseconds": 614.1553821288664,
            "operationsPerSecond": 3169.9268380473027
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
            "sampleCount": 15,
            "meanNanoseconds": 313581.4928059896,
            "medianNanoseconds": 313548.20947265625,
            "minNanoseconds": 312550.05712890625,
            "maxNanoseconds": 314684.75439453125,
            "standardDeviationNanoseconds": 685.940577428687,
            "operationsPerSecond": 3188.9637078126043
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PhraseQueryBenchmarks-20260430-103827",
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
            "sampleCount": 13,
            "meanNanoseconds": 430723.171875,
            "medianNanoseconds": 430228.37353515625,
            "minNanoseconds": 424430.814453125,
            "maxNanoseconds": 435198.24658203125,
            "standardDeviationNanoseconds": 3466.282705326906,
            "operationsPerSecond": 2321.6768107619005
          },
          "gc": {
            "bytesAllocatedPerOperation": 61204,
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
            "sampleCount": 14,
            "meanNanoseconds": 335162.3103027344,
            "medianNanoseconds": 333919.8171386719,
            "minNanoseconds": 329592.70166015625,
            "maxNanoseconds": 344916.96142578125,
            "standardDeviationNanoseconds": 4410.9412712178955,
            "operationsPerSecond": 2983.6290336367265
          },
          "gc": {
            "bytesAllocatedPerOperation": 43951,
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
            "meanNanoseconds": 983358.0739583333,
            "medianNanoseconds": 981396.90234375,
            "minNanoseconds": 972241.982421875,
            "maxNanoseconds": 996874.90234375,
            "standardDeviationNanoseconds": 8560.259249112118,
            "operationsPerSecond": 1016.9235667885224
          },
          "gc": {
            "bytesAllocatedPerOperation": 49887,
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
            "sampleCount": 15,
            "meanNanoseconds": 350205.1783854167,
            "medianNanoseconds": 350208.89404296875,
            "minNanoseconds": 348777.1513671875,
            "maxNanoseconds": 351867.814453125,
            "standardDeviationNanoseconds": 805.1880289078002,
            "operationsPerSecond": 2855.468912853866
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
            "meanNanoseconds": 406653.80387369794,
            "medianNanoseconds": 406499.5498046875,
            "minNanoseconds": 405742.1591796875,
            "maxNanoseconds": 407592.22509765625,
            "standardDeviationNanoseconds": 580.8346571747065,
            "operationsPerSecond": 2459.094174145703
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
            "sampleCount": 14,
            "meanNanoseconds": 1034917.5922154018,
            "medianNanoseconds": 1034651.0908203125,
            "minNanoseconds": 1030046.599609375,
            "maxNanoseconds": 1041176.798828125,
            "standardDeviationNanoseconds": 2779.0077700156858,
            "operationsPerSecond": 966.2605095535623
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PrefixQueryBenchmarks-20260430-104329",
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
            "sampleCount": 12,
            "meanNanoseconds": 119669.28812662761,
            "medianNanoseconds": 119674.63006591797,
            "minNanoseconds": 118806.95324707031,
            "maxNanoseconds": 120338.041015625,
            "standardDeviationNanoseconds": 529.2031621572687,
            "operationsPerSecond": 8356.362903586873
          },
          "gc": {
            "bytesAllocatedPerOperation": 24248,
            "gen0Collections": 48,
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
            "meanNanoseconds": 195627.79033203126,
            "medianNanoseconds": 195094.25927734375,
            "minNanoseconds": 193874.32641601562,
            "maxNanoseconds": 198827.91479492188,
            "standardDeviationNanoseconds": 1717.3267946501683,
            "operationsPerSecond": 5111.748173931423
          },
          "gc": {
            "bytesAllocatedPerOperation": 35390,
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
            "meanNanoseconds": 247409.59315708705,
            "medianNanoseconds": 246926.48754882812,
            "minNanoseconds": 243935.06298828125,
            "maxNanoseconds": 253401.892578125,
            "standardDeviationNanoseconds": 2700.5700024026114,
            "operationsPerSecond": 4041.8804591989806
          },
          "gc": {
            "bytesAllocatedPerOperation": 64475,
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
            "meanNanoseconds": 190361.40509440104,
            "medianNanoseconds": 190335.76782226562,
            "minNanoseconds": 189822.1708984375,
            "maxNanoseconds": 190910.279296875,
            "standardDeviationNanoseconds": 334.31693007448047,
            "operationsPerSecond": 5253.165679797834
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
            "meanNanoseconds": 287423.57535807294,
            "medianNanoseconds": 287400.1875,
            "minNanoseconds": 285935.0390625,
            "maxNanoseconds": 289017.42822265625,
            "standardDeviationNanoseconds": 924.960233964467,
            "operationsPerSecond": 3479.185723558681
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
            "meanNanoseconds": 354637.96988932294,
            "medianNanoseconds": 354782.5439453125,
            "minNanoseconds": 353672.85009765625,
            "maxNanoseconds": 355685.611328125,
            "standardDeviationNanoseconds": 730.8230089185877,
            "operationsPerSecond": 2819.777025883846
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.TermQueryBenchmarks-20260430-101604",
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
            "sampleCount": 15,
            "meanNanoseconds": 106512.7733561198,
            "medianNanoseconds": 106473.29968261719,
            "minNanoseconds": 106254.69921875,
            "maxNanoseconds": 106919.87512207031,
            "standardDeviationNanoseconds": 179.96848287495072,
            "operationsPerSecond": 9388.545321757356
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
            "meanNanoseconds": 149404.88645833332,
            "medianNanoseconds": 149488.29370117188,
            "minNanoseconds": 148825.25219726562,
            "maxNanoseconds": 149997.501953125,
            "standardDeviationNanoseconds": 330.2046172246738,
            "operationsPerSecond": 6693.221511726689
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
            "sampleCount": 15,
            "meanNanoseconds": 681149.7967447917,
            "medianNanoseconds": 681071.6982421875,
            "minNanoseconds": 679405.220703125,
            "maxNanoseconds": 683484.0498046875,
            "standardDeviationNanoseconds": 1259.6104269201726,
            "operationsPerSecond": 1468.105848051325
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
            "sampleCount": 15,
            "meanNanoseconds": 136901.55348307293,
            "medianNanoseconds": 136909.60766601562,
            "minNanoseconds": 136312.33227539062,
            "maxNanoseconds": 137730.78002929688,
            "standardDeviationNanoseconds": 373.6830744218728,
            "operationsPerSecond": 7304.5190106162245
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
            "sampleCount": 14,
            "meanNanoseconds": 176701.79335239955,
            "medianNanoseconds": 176755.9189453125,
            "minNanoseconds": 176347.78735351562,
            "maxNanoseconds": 176992.68994140625,
            "standardDeviationNanoseconds": 205.65881207877067,
            "operationsPerSecond": 5659.252127711472
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
            "meanNanoseconds": 754983.5602213541,
            "medianNanoseconds": 755137.728515625,
            "minNanoseconds": 752333.53515625,
            "maxNanoseconds": 758006.5869140625,
            "standardDeviationNanoseconds": 1643.5118906483294,
            "operationsPerSecond": 1324.53215233827
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SchemaAndJsonBenchmarks-20260430-111118",
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
            "sampleCount": 14,
            "meanNanoseconds": 9167196639.142857,
            "medianNanoseconds": 9167832289,
            "minNanoseconds": 9085454842,
            "maxNanoseconds": 9216149189,
            "standardDeviationNanoseconds": 32590644.50052023,
            "operationsPerSecond": 0.10908460234507429
          },
          "gc": {
            "bytesAllocatedPerOperation": 954247360,
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
            "meanNanoseconds": 9307882986.4,
            "medianNanoseconds": 9298553589,
            "minNanoseconds": 9257799627,
            "maxNanoseconds": 9384180681,
            "standardDeviationNanoseconds": 40056422.42434856,
            "operationsPerSecond": 0.10743581558353571
          },
          "gc": {
            "bytesAllocatedPerOperation": 958246400,
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
            "meanNanoseconds": 420866802.06666666,
            "medianNanoseconds": 421210295,
            "minNanoseconds": 417781965,
            "maxNanoseconds": 422653302,
            "standardDeviationNanoseconds": 1342426.7997356488,
            "operationsPerSecond": 2.37604865741251
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SuggesterBenchmarks-20260430-110733",
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
            "sampleCount": 15,
            "meanNanoseconds": 4620166.81875,
            "medianNanoseconds": 4600917.8671875,
            "minNanoseconds": 4583015.0546875,
            "maxNanoseconds": 4710875.453125,
            "standardDeviationNanoseconds": 39647.51656507146,
            "operationsPerSecond": 216.44240115783376
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
            "sampleCount": 15,
            "meanNanoseconds": 4699802.474479167,
            "medianNanoseconds": 4675521.609375,
            "minNanoseconds": 4652321.5,
            "maxNanoseconds": 4783940.0703125,
            "standardDeviationNanoseconds": 45718.172844170804,
            "operationsPerSecond": 212.77489967508055
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
            "meanNanoseconds": 10150917.402901785,
            "medianNanoseconds": 10153623.84375,
            "minNanoseconds": 10109854.203125,
            "maxNanoseconds": 10182542.53125,
            "standardDeviationNanoseconds": 18763.461682418478,
            "operationsPerSecond": 98.51326341342661
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.WildcardQueryBenchmarks-20260430-105355",
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
            "sampleCount": 14,
            "meanNanoseconds": 125221.91493443081,
            "medianNanoseconds": 125062.52685546875,
            "minNanoseconds": 123804.54248046875,
            "maxNanoseconds": 126883.12646484375,
            "standardDeviationNanoseconds": 904.2158605145861,
            "operationsPerSecond": 7985.822613586639
          },
          "gc": {
            "bytesAllocatedPerOperation": 31280,
            "gen0Collections": 31,
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
            "sampleCount": 16,
            "meanNanoseconds": 1176897.1748046875,
            "medianNanoseconds": 1178824.7646484375,
            "minNanoseconds": 1130514.794921875,
            "maxNanoseconds": 1212406.841796875,
            "standardDeviationNanoseconds": 22466.303671932114,
            "operationsPerSecond": 849.6919029191785
          },
          "gc": {
            "bytesAllocatedPerOperation": 1547024,
            "gen0Collections": 190,
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
            "sampleCount": 15,
            "meanNanoseconds": 152606.961328125,
            "medianNanoseconds": 152331.02612304688,
            "minNanoseconds": 150199.20629882812,
            "maxNanoseconds": 155847.90600585938,
            "standardDeviationNanoseconds": 1634.728116902054,
            "operationsPerSecond": 6552.781021894989
          },
          "gc": {
            "bytesAllocatedPerOperation": 138396,
            "gen0Collections": 136,
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
            "sampleCount": 15,
            "meanNanoseconds": 203906.26287434896,
            "medianNanoseconds": 203713.31030273438,
            "minNanoseconds": 203290.07592773438,
            "maxNanoseconds": 204800.6298828125,
            "standardDeviationNanoseconds": 488.6443229210609,
            "operationsPerSecond": 4904.2142497419
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
            "meanNanoseconds": 1173414.95078125,
            "medianNanoseconds": 1173658.8359375,
            "minNanoseconds": 1169374.966796875,
            "maxNanoseconds": 1178350.994140625,
            "standardDeviationNanoseconds": 2401.529052322893,
            "operationsPerSecond": 852.2134470284432
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
            "meanNanoseconds": 407826.6268833705,
            "medianNanoseconds": 407804.1862792969,
            "minNanoseconds": 406512.677734375,
            "maxNanoseconds": 408686.37158203125,
            "standardDeviationNanoseconds": 615.3743135985178,
            "operationsPerSecond": 2452.022340086142
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

