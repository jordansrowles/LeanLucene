---
title: Benchmarks - debian
---

# Benchmarks: debian

**.NET** 10.0.3 &nbsp;&middot;&nbsp; **Commit** `7db7058` &nbsp;&middot;&nbsp; 29 April 2026 09:59 UTC &nbsp;&middot;&nbsp; 76 benchmarks

## Analysis

| Method             | DocumentCount | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0       | Allocated | Alloc Ratio |
|------------------- |-------------- |---------:|--------:|--------:|------:|--------:|-----------:|----------:|------------:|
| LeanLucene_Analyse | 100000        | 439.9 ms | 2.16 ms | 2.02 ms |  1.00 |    0.00 |  9000.0000 |  38.67 MB |        1.00 |
| LuceneNet_Analyse  | 100000        | 686.1 ms | 6.71 ms | 6.27 ms |  1.56 |    0.02 | 43000.0000 | 171.61 MB |        4.44 |

## Block-Join

| Method                           | BlockCount | Mean          | Error       | StdDev      | Ratio | Gen0      | Gen1     | Allocated  | Alloc Ratio |
|--------------------------------- |----------- |--------------:|------------:|------------:|------:|----------:|---------:|-----------:|------------:|
| LeanLucene_IndexBlocks           | 500        | 33,143.746 μs | 176.3469 μs | 164.9550 μs | 1.000 |  687.5000 | 250.0000 |  5225108 B |       1.000 |
| LeanLucene_BlockJoinQuery        | 500        |      6.612 μs |   0.0150 μs |   0.0140 μs | 0.000 |    0.1678 |        - |      720 B |       0.000 |
| LuceneNet_IndexBlocks            | 500        | 36,902.857 μs | 130.6531 μs | 109.1012 μs | 1.113 | 3000.0000 | 571.4286 | 17778766 B |       3.403 |
| LuceneNet_ToParentBlockJoinQuery | 500        |     22.108 μs |   0.0725 μs |   0.0678 μs | 0.001 |    3.1738 |        - |    13344 B |       0.003 |

## Boolean queries

| Method                  | BooleanType | DocumentCount | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------ |------------ |-------------- |----------:|---------:|---------:|------:|--------:|--------:|--------:|----------:|------------:|
| **LeanLucene_BooleanQuery** | **Must**        | **100000**        |  **29.04 μs** | **0.421 μs** | **0.394 μs** |  **1.00** |    **0.00** |  **2.9297** |       **-** |  **11.84 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Must        | 100000        |  67.85 μs | 0.211 μs | 0.197 μs |  2.34 |    0.03 | 14.0381 |       - |  57.55 KB |        4.86 |
|                         |             |               |           |          |          |       |         |         |         |           |             |
| **LeanLucene_BooleanQuery** | **MustNot**     | **100000**        |  **19.78 μs** | **0.348 μs** | **0.326 μs** |  **1.00** |    **0.00** |  **3.1433** |       **-** |  **12.78 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | MustNot     | 100000        |  43.40 μs | 0.118 μs | 0.093 μs |  2.19 |    0.03 | 12.4512 |       - |  50.96 KB |        3.99 |
|                         |             |               |           |          |          |       |         |         |         |           |             |
| **LeanLucene_BooleanQuery** | **Should**      | **100000**        |  **83.37 μs** | **1.069 μs** | **0.947 μs** |  **1.00** |    **0.00** |  **3.4180** |       **-** |  **13.69 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Should      | 100000        | 205.67 μs | 0.540 μs | 0.479 μs |  2.47 |    0.03 | 67.1387 | 14.8926 | 274.37 KB |       20.04 |

## Deletion

| Method                     | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1       | Allocated | Alloc Ratio |
|--------------------------- |-------------- |--------:|---------:|---------:|------:|------------:|-----------:|----------:|------------:|
| LeanLucene_DeleteDocuments | 100000        | 3.144 s | 0.0204 s | 0.0191 s |  1.00 |  52000.0000 | 25000.0000 | 375.55 MB |        1.00 |
| LuceneNet_DeleteDocuments  | 100000        | 2.577 s | 0.0091 s | 0.0081 s |  0.82 | 151000.0000 | 14000.0000 | 818.49 MB |        2.18 |

## Fuzzy queries

| Method                | QueryTerm | DocumentCount | Mean     | Error     | StdDev    | Ratio | Gen0     | Gen1     | Allocated  | Alloc Ratio |
|---------------------- |---------- |-------------- |---------:|----------:|----------:|------:|---------:|---------:|-----------:|------------:|
| **LeanLucene_FuzzyQuery** | **goverment** | **100000**        | **3.385 ms** | **0.0217 ms** | **0.0203 ms** |  **1.00** |   **3.9063** |        **-** |      **21 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | goverment | 100000        | 2.629 ms | 0.0068 ms | 0.0064 ms |  0.78 | 414.0625 | 183.5938 | 1987.71 KB |       94.64 |
|                       |           |               |          |           |           |       |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **markts**    | **100000**        | **3.495 ms** | **0.0239 ms** | **0.0212 ms** |  **1.00** |   **7.8125** |        **-** |    **32.2 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | markts    | 100000        | 2.415 ms | 0.0066 ms | 0.0059 ms |  0.69 | 410.1563 | 128.9063 | 1757.45 KB |       54.58 |
|                       |           |               |          |           |           |       |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **presiden**  | **100000**        | **3.975 ms** | **0.0335 ms** | **0.0313 ms** |  **1.00** |        **-** |        **-** |   **21.39 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | presiden  | 100000        | 2.496 ms | 0.0104 ms | 0.0097 ms |  0.63 | 398.4375 | 101.5625 | 1844.47 KB |       86.21 |

## gutenberg-analysis

| Method                      | Mean    | Error    | StdDev   | Ratio | RatioSD | Gen0        | Gen1        | Gen2       | Allocated  | Alloc Ratio |
|---------------------------- |--------:|---------:|---------:|------:|--------:|------------:|------------:|-----------:|-----------:|------------:|
| LeanLucene_Standard_Analyse | 2.077 s | 0.0051 s | 0.0048 s |  1.00 |    0.00 |  42000.0000 |  29000.0000 |  2000.0000 |  219.42 MB |        1.00 |
| LeanLucene_English_Analyse  | 6.244 s | 0.0327 s | 0.0306 s |  3.01 |    0.02 | 201000.0000 | 135000.0000 | 43000.0000 | 1832.01 MB |        8.35 |

## gutenberg-index

| Method                    | Mean    | Error   | StdDev  | Ratio | Gen0         | Gen1        | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |--------:|--------:|--------:|------:|-------------:|------------:|----------:|----------:|------------:|
| LeanLucene_Standard_Index | 14.93 s | 0.084 s | 0.079 s |  1.00 |  255000.0000 | 129000.0000 | 3000.0000 |    1.7 GB |        1.00 |
| LeanLucene_English_Index  | 15.20 s | 0.024 s | 0.022 s |  1.02 |  501000.0000 | 168000.0000 | 3000.0000 |   3.12 GB |        1.84 |
| LuceneNet_Index           | 12.87 s | 0.038 s | 0.035 s |  0.86 | 1130000.0000 |  25000.0000 | 1000.0000 |   5.12 GB |        3.02 |

## gutenberg-search

| Method                     | SearchTerm | Mean     | Error   | StdDev  | Ratio | Gen0    | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| **LeanLucene_Standard_Search** | **death**      | **171.4 μs** | **0.52 μs** | **0.46 μs** |  **1.00** |       **-** |      **-** |     **472 B** |        **1.00** |
| LeanLucene_English_Search  | death      | 184.0 μs | 0.44 μs | 0.41 μs |  1.07 |       - |      - |     472 B |        1.00 |
| LuceneNet_Search           | death      | 221.7 μs | 0.76 μs | 0.71 μs |  1.29 | 17.8223 | 0.2441 |   75458 B |      159.87 |
|                            |            |          |         |         |       |         |        |           |             |
| **LeanLucene_Standard_Search** | **love**       | **197.6 μs** | **0.61 μs** | **0.57 μs** |  **1.00** |       **-** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | love       | 256.4 μs | 0.83 μs | 0.77 μs |  1.30 |       - |      - |     464 B |        1.00 |
| LuceneNet_Search           | love       | 251.8 μs | 0.81 μs | 0.76 μs |  1.27 | 18.0664 | 0.4883 |   76637 B |      165.17 |
|                            |            |          |         |         |       |         |        |           |             |
| **LeanLucene_Standard_Search** | **man**        | **582.2 μs** | **1.14 μs** | **1.07 μs** |  **1.00** |       **-** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | man        | 586.5 μs | 1.76 μs | 1.65 μs |  1.01 |       - |      - |     464 B |        1.00 |
| LuceneNet_Search           | man        | 622.9 μs | 1.54 μs | 1.44 μs |  1.07 | 17.5781 | 0.9766 |   76099 B |      164.01 |
|                            |            |          |         |         |       |         |        |           |             |
| **LeanLucene_Standard_Search** | **night**      | **223.3 μs** | **0.60 μs** | **0.56 μs** |  **1.00** |       **-** |      **-** |     **472 B** |        **1.00** |
| LeanLucene_English_Search  | night      | 234.5 μs | 0.37 μs | 0.35 μs |  1.05 |       - |      - |     472 B |        1.00 |
| LuceneNet_Search           | night      | 268.7 μs | 0.78 μs | 0.73 μs |  1.20 | 18.0664 | 0.4883 |   77180 B |      163.52 |
|                            |            |          |         |         |       |         |        |           |             |
| **LeanLucene_Standard_Search** | **sea**        | **125.3 μs** | **0.47 μs** | **0.44 μs** |  **1.00** |       **-** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | sea        | 135.1 μs | 0.27 μs | 0.24 μs |  1.08 |       - |      - |     464 B |        1.00 |
| LuceneNet_Search           | sea        | 179.8 μs | 0.35 μs | 0.29 μs |  1.43 | 17.8223 | 0.2441 |   75394 B |      162.49 |

## Indexing

| Method                    | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1       | Allocated | Alloc Ratio |
|-------------------------- |-------------- |--------:|---------:|---------:|------:|------------:|-----------:|----------:|------------:|
| LeanLucene_IndexDocuments | 100000        | 3.138 s | 0.0177 s | 0.0165 s |  1.00 |  52000.0000 | 26000.0000 |  357.7 MB |        1.00 |
| LuceneNet_IndexDocuments  | 100000        | 2.507 s | 0.0089 s | 0.0079 s |  0.80 | 145000.0000 | 14000.0000 | 784.72 MB |        2.19 |

## Index-sort (index)

| Method                    | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0       | Gen1       | Allocated | Alloc Ratio |
|-------------------------- |-------------- |--------:|---------:|---------:|------:|-----------:|-----------:|----------:|------------:|
| LeanLucene_Index_Unsorted | 100000        | 3.237 s | 0.0089 s | 0.0083 s |  1.00 | 55000.0000 | 26000.0000 | 389.83 MB |        1.00 |
| LeanLucene_Index_Sorted   | 100000        | 3.570 s | 0.0116 s | 0.0103 s |  1.10 | 56000.0000 | 27000.0000 | 400.92 MB |        1.03 |

## Index-sort (search)

| Method                                   | DocumentCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------------- |-------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| LeanLucene_SortedSearch_EarlyTermination | 100000        | 5.116 μs | 0.0118 μs | 0.0104 μs |  1.00 | 0.4730 |   1.95 KB |        1.00 |
| LeanLucene_SortedSearch_PostSort         | 100000        | 5.478 μs | 0.0175 μs | 0.0163 μs |  1.07 | 0.4349 |    1.8 KB |        0.93 |

## Phrase queries

| Method                 | PhraseType     | DocumentCount | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------- |--------------- |-------------- |----------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **LeanLucene_PhraseQuery** | **ExactThreeWord** | **100000**        |  **42.66 μs** | **0.739 μs** | **1.037 μs** |  **1.00** |    **0.00** | **13.2446** |      **-** |  **52.38 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactThreeWord | 100000        |  44.64 μs | 0.132 μs | 0.124 μs |  1.05 |    0.02 | 34.8511 | 5.7983 | 142.59 KB |        2.72 |
|                        |                |               |           |          |          |       |         |         |        |           |             |
| **LeanLucene_PhraseQuery** | **ExactTwoWord**   | **100000**        |  **40.91 μs** | **0.501 μs** | **0.391 μs** |  **1.00** |    **0.00** |  **9.7046** |      **-** |  **38.73 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactTwoWord   | 100000        |  59.59 μs | 0.117 μs | 0.104 μs |  1.46 |    0.01 | 29.9072 | 5.9204 | 122.47 KB |        3.16 |
|                        |                |               |           |          |          |       |         |         |        |           |             |
| **LeanLucene_PhraseQuery** | **SlopTwoWord**    | **100000**        | **151.29 μs** | **0.958 μs** | **0.849 μs** |  **1.00** |    **0.00** | **10.7422** |      **-** |  **42.58 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | SlopTwoWord    | 100000        |  74.36 μs | 0.245 μs | 0.229 μs |  0.49 |    0.00 | 14.8926 | 0.2441 |  61.23 KB |        1.44 |

## Prefix queries

| Method                 | QueryPrefix | DocumentCount | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-------------- |----------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **LeanLucene_PrefixQuery** | **gov**         | **100000**        |  **27.84 μs** | **0.369 μs** | **0.345 μs** |  **1.00** |    **0.00** |  **3.6926** |      **-** |     **15 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | gov         | 100000        |  39.09 μs | 0.138 μs | 0.122 μs |  1.40 |    0.02 | 18.6768 | 0.1221 |  76.36 KB |        5.09 |
|                        |             |               |           |          |          |       |         |         |        |           |             |
| **LeanLucene_PrefixQuery** | **mark**        | **100000**        |  **36.12 μs** | **0.721 μs** | **2.046 μs** |  **1.00** |    **0.00** |  **3.9673** |      **-** |  **16.09 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | mark        | 100000        |  49.22 μs | 0.124 μs | 0.116 μs |  1.37 |    0.08 | 18.1274 |      - |  74.87 KB |        4.65 |
|                        |             |               |           |          |          |       |         |         |        |           |             |
| **LeanLucene_PrefixQuery** | **pres**        | **100000**        | **102.21 μs** | **1.651 μs** | **1.544 μs** |  **1.00** |    **0.00** |  **9.7656** | **0.1221** |  **39.41 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | pres        | 100000        | 127.49 μs | 0.523 μs | 0.489 μs |  1.25 |    0.02 | 20.9961 | 2.1973 |  86.16 KB |        2.19 |

## Term queries

| Method               | QueryTerm  | DocumentCount | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |-------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| **LeanLucene_TermQuery** | **government** | **100000**        |  **11.65 μs** | **0.041 μs** | **0.038 μs** |  **1.00** | **0.1068** |     **480 B** |        **1.00** |
| LuceneNet_TermQuery  | government | 100000        |  22.58 μs | 0.073 μs | 0.068 μs |  1.94 | 6.1646 |   25816 B |       53.78 |
|                      |            |               |           |          |          |       |        |           |             |
| **LeanLucene_TermQuery** | **people**     | **100000**        |  **84.70 μs** | **0.291 μs** | **0.272 μs** |  **1.00** |      **-** |     **472 B** |        **1.00** |
| LuceneNet_TermQuery  | people     | 100000        |  96.30 μs | 0.232 μs | 0.217 μs |  1.14 | 5.9814 |   25400 B |       53.81 |
|                      |            |               |           |          |          |       |        |           |             |
| **LeanLucene_TermQuery** | **said**       | **100000**        | **284.32 μs** | **0.725 μs** | **0.678 μs** |  **1.00** |      **-** |     **464 B** |        **1.00** |
| LuceneNet_TermQuery  | said       | 100000        | 318.61 μs | 0.650 μs | 0.543 μs |  1.12 | 5.8594 |   25216 B |       54.34 |

## Schema and JSON

| Method                      | DocumentCount | Mean       | Error    | StdDev   | Ratio | Gen0       | Gen1       | Allocated | Alloc Ratio |
|---------------------------- |-------------- |-----------:|---------:|---------:|------:|-----------:|-----------:|----------:|------------:|
| LeanLucene_Index_NoSchema   | 100000        | 3,131.9 ms | 17.59 ms | 16.45 ms |  1.00 | 51000.0000 | 24000.0000 | 357.75 MB |        1.00 |
| LeanLucene_Index_WithSchema | 100000        | 3,175.9 ms | 16.79 ms | 15.71 ms |  1.01 | 52000.0000 | 27000.0000 | 361.51 MB |        1.01 |
| LeanLucene_JsonMapping      | 100000        |   217.0 ms |  0.76 ms |  0.71 ms |  0.07 | 23666.6667 |          - |  94.87 MB |        0.27 |

## Suggester

| Method                 | DocumentCount | Mean     | Error     | StdDev    | Ratio | Gen0      | Gen1    | Allocated | Alloc Ratio |
|----------------------- |-------------- |---------:|----------:|----------:|------:|----------:|--------:|----------:|------------:|
| LeanLucene_DidYouMean  | 100000        | 2.612 ms | 0.0093 ms | 0.0087 ms |  1.00 |         - |       - |  12.87 KB |        1.00 |
| LeanLucene_SpellIndex  | 100000        | 2.754 ms | 0.0108 ms | 0.0096 ms |  1.05 |         - |       - |  11.15 KB |        0.87 |
| LuceneNet_SpellChecker | 100000        | 9.032 ms | 0.0251 ms | 0.0222 ms |  3.46 | 1265.6250 | 46.8750 | 5224.6 KB |      406.04 |

## Wildcard queries

| Method                   | WildcardPattern | DocumentCount | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0     | Gen1   | Allocated | Alloc Ratio |
|------------------------- |---------------- |-------------- |----------:|---------:|---------:|------:|--------:|---------:|-------:|----------:|------------:|
| **LeanLucene_WildcardQuery** | **gov***            | **100000**        |  **30.45 μs** | **0.362 μs** | **0.283 μs** |  **1.00** |    **0.00** |   **4.5776** |      **-** |  **18.52 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | gov*            | 100000        |  52.93 μs | 0.128 μs | 0.113 μs |  1.74 |    0.02 |  23.2544 | 0.0610 |  95.38 KB |        5.15 |
|                          |                 |               |           |          |          |       |         |          |        |           |             |
| **LeanLucene_WildcardQuery** | **m*rket**          | **100000**        | **338.93 μs** | **3.060 μs** | **2.713 μs** |  **1.00** |    **0.00** | **100.0977** |      **-** | **407.46 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | m*rket          | 100000        | 352.86 μs | 0.761 μs | 0.595 μs |  1.04 |    0.01 |  77.1484 | 1.9531 | 316.92 KB |        0.78 |
|                          |                 |               |           |          |          |       |         |          |        |           |             |
| **LeanLucene_WildcardQuery** | **pre*dent**        | **100000**        |  **50.14 μs** | **0.965 μs** | **1.072 μs** |  **1.00** |    **0.00** |  **15.7471** |      **-** |  **64.08 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | pre*dent        | 100000        | 275.89 μs | 1.009 μs | 0.944 μs |  5.50 |    0.12 |  83.0078 |      - | 342.77 KB |        5.35 |

<details>
<summary>Full data (report.json)</summary>

<pre><code class="lang-json">{
  "schemaVersion": 2,
  "runId": "2026-04-29 09-59 (7db7058)",
  "runType": "full",
  "generatedAtUtc": "2026-04-29T09:59:13.0436923\u002B00:00",
  "commandLineArgs": [],
  "hostMachineName": "debian",
  "commitHash": "7db7058",
  "dotnetVersion": "10.0.3",
  "totalBenchmarkCount": 76,
  "suites": [
    {
      "suiteName": "analysis",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.AnalysisBenchmarks-20260429-110818",
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
            "meanNanoseconds": 439912093.2,
            "medianNanoseconds": 439306408,
            "minNanoseconds": 436704773,
            "maxNanoseconds": 442995633,
            "standardDeviationNanoseconds": 2023991.999463832,
            "operationsPerSecond": 2.2731814275116182
          },
          "gc": {
            "bytesAllocatedPerOperation": 40550400,
            "gen0Collections": 9,
            "gen1Collections": 0,
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
            "meanNanoseconds": 686104860,
            "medianNanoseconds": 685636965,
            "minNanoseconds": 678410583,
            "maxNanoseconds": 695503804,
            "standardDeviationNanoseconds": 6272429.535582547,
            "operationsPerSecond": 1.4575031577534665
          },
          "gc": {
            "bytesAllocatedPerOperation": 179945480,
            "gen0Collections": 43,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "blockjoin",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BlockJoinBenchmarks-20260429-115330",
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
            "meanNanoseconds": 6611.557213338217,
            "medianNanoseconds": 6614.867057800293,
            "minNanoseconds": 6582.236595153809,
            "maxNanoseconds": 6629.375061035156,
            "standardDeviationNanoseconds": 14.012509692820831,
            "operationsPerSecond": 151250.29818733034
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
            "meanNanoseconds": 33143745.641666666,
            "medianNanoseconds": 33168865.25,
            "minNanoseconds": 32854400.9375,
            "maxNanoseconds": 33378466.75,
            "standardDeviationNanoseconds": 164954.97347587493,
            "operationsPerSecond": 30.17160494807955
          },
          "gc": {
            "bytesAllocatedPerOperation": 5225108,
            "gen0Collections": 11,
            "gen1Collections": 4,
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
            "meanNanoseconds": 36902857.28571428,
            "medianNanoseconds": 36905693.571428575,
            "minNanoseconds": 36740503.5,
            "maxNanoseconds": 37081552.071428575,
            "standardDeviationNanoseconds": 109101.23270007933,
            "operationsPerSecond": 27.098172704017603
          },
          "gc": {
            "bytesAllocatedPerOperation": 17778766,
            "gen0Collections": 42,
            "gen1Collections": 8,
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
            "meanNanoseconds": 22107.514280192056,
            "medianNanoseconds": 22093.13931274414,
            "minNanoseconds": 22004.281127929688,
            "maxNanoseconds": 22250.099822998047,
            "standardDeviationNanoseconds": 67.84650672576643,
            "operationsPerSecond": 45233.48881859515
          },
          "gc": {
            "bytesAllocatedPerOperation": 13344,
            "gen0Collections": 104,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "boolean",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BooleanQueryBenchmarks-20260429-111024",
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
            "sampleCount": 15,
            "meanNanoseconds": 29040.459025065105,
            "medianNanoseconds": 28886.845764160156,
            "minNanoseconds": 28564.44012451172,
            "maxNanoseconds": 29807.395599365234,
            "standardDeviationNanoseconds": 393.8629335432318,
            "operationsPerSecond": 34434.71741052337
          },
          "gc": {
            "bytesAllocatedPerOperation": 12121,
            "gen0Collections": 96,
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
            "meanNanoseconds": 19780.80352376302,
            "medianNanoseconds": 19724.902069091797,
            "minNanoseconds": 19353.516876220703,
            "maxNanoseconds": 20316.48471069336,
            "standardDeviationNanoseconds": 325.5651841004414,
            "operationsPerSecond": 50554.063630361765
          },
          "gc": {
            "bytesAllocatedPerOperation": 13087,
            "gen0Collections": 103,
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
            "sampleCount": 14,
            "meanNanoseconds": 83368.07439313616,
            "medianNanoseconds": 83231.37060546875,
            "minNanoseconds": 81796.00207519531,
            "maxNanoseconds": 85178.62646484375,
            "standardDeviationNanoseconds": 947.3874318260775,
            "operationsPerSecond": 11994.999372113742
          },
          "gc": {
            "bytesAllocatedPerOperation": 14021,
            "gen0Collections": 28,
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
            "meanNanoseconds": 67846.37734375,
            "medianNanoseconds": 67855.66064453125,
            "minNanoseconds": 67440.28466796875,
            "maxNanoseconds": 68150.98181152344,
            "standardDeviationNanoseconds": 197.0122838589049,
            "operationsPerSecond": 14739.180471396528
          },
          "gc": {
            "bytesAllocatedPerOperation": 58936,
            "gen0Collections": 115,
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
            "sampleCount": 12,
            "meanNanoseconds": 43404.47196960449,
            "medianNanoseconds": 43427.72277832031,
            "minNanoseconds": 43180.76904296875,
            "maxNanoseconds": 43511.256774902344,
            "standardDeviationNanoseconds": 92.50873387338748,
            "operationsPerSecond": 23039.10068760392
          },
          "gc": {
            "bytesAllocatedPerOperation": 52184,
            "gen0Collections": 204,
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
            "meanNanoseconds": 205673.73301478795,
            "medianNanoseconds": 205549.89208984375,
            "minNanoseconds": 204942.56567382812,
            "maxNanoseconds": 206610.29028320312,
            "standardDeviationNanoseconds": 478.59580646927253,
            "operationsPerSecond": 4862.069576614822
          },
          "gc": {
            "bytesAllocatedPerOperation": 280952,
            "gen0Collections": 275,
            "gen1Collections": 61,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "deletion",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.DeletionBenchmarks-20260429-113409",
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
            "sampleCount": 15,
            "meanNanoseconds": 3144148197.4,
            "medianNanoseconds": 3135663388,
            "minNanoseconds": 3119641524,
            "maxNanoseconds": 3186907523,
            "standardDeviationNanoseconds": 19085284.914935734,
            "operationsPerSecond": 0.3180511659173486
          },
          "gc": {
            "bytesAllocatedPerOperation": 393795920,
            "gen0Collections": 52,
            "gen1Collections": 25,
            "gen2Collections": 0
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
            "meanNanoseconds": 2576737837.714286,
            "medianNanoseconds": 2574393719.5,
            "minNanoseconds": 2564246989,
            "maxNanoseconds": 2595842147,
            "standardDeviationNanoseconds": 8058557.861338441,
            "operationsPerSecond": 0.3880875987318358
          },
          "gc": {
            "bytesAllocatedPerOperation": 858253544,
            "gen0Collections": 151,
            "gen1Collections": 14,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "fuzzy",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.FuzzyQueryBenchmarks-20260429-112512",
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
            "meanNanoseconds": 3384703.8510416667,
            "medianNanoseconds": 3381087.15234375,
            "minNanoseconds": 3355950.34375,
            "maxNanoseconds": 3423368.14453125,
            "standardDeviationNanoseconds": 20337.586243310398,
            "operationsPerSecond": 295.44682312227786
          },
          "gc": {
            "bytesAllocatedPerOperation": 21508,
            "gen0Collections": 1,
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
            "sampleCount": 14,
            "meanNanoseconds": 3495082.759207589,
            "medianNanoseconds": 3496834.498046875,
            "minNanoseconds": 3468557.47265625,
            "maxNanoseconds": 3542115.3203125,
            "standardDeviationNanoseconds": 21208.337146169226,
            "operationsPerSecond": 286.1162578670159
          },
          "gc": {
            "bytesAllocatedPerOperation": 32970,
            "gen0Collections": 2,
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
            "sampleCount": 15,
            "meanNanoseconds": 3974957.6614583335,
            "medianNanoseconds": 3971501.0234375,
            "minNanoseconds": 3918254.5078125,
            "maxNanoseconds": 4041497.578125,
            "standardDeviationNanoseconds": 31345.185376517114,
            "operationsPerSecond": 251.57500662110695
          },
          "gc": {
            "bytesAllocatedPerOperation": 21908,
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
            "meanNanoseconds": 2628968.244010417,
            "medianNanoseconds": 2627838.7421875,
            "minNanoseconds": 2620942.484375,
            "maxNanoseconds": 2642675.8046875,
            "standardDeviationNanoseconds": 6386.680865540092,
            "operationsPerSecond": 380.377359931335
          },
          "gc": {
            "bytesAllocatedPerOperation": 2035417,
            "gen0Collections": 106,
            "gen1Collections": 47,
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
            "meanNanoseconds": 2415041.7594866073,
            "medianNanoseconds": 2414272.484375,
            "minNanoseconds": 2402622.37109375,
            "maxNanoseconds": 2424055.875,
            "standardDeviationNanoseconds": 5878.713965640112,
            "operationsPerSecond": 414.07151494249166
          },
          "gc": {
            "bytesAllocatedPerOperation": 1799628,
            "gen0Collections": 105,
            "gen1Collections": 33,
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
            "meanNanoseconds": 2495702.115625,
            "medianNanoseconds": 2496986.16796875,
            "minNanoseconds": 2479300.15625,
            "maxNanoseconds": 2514040.75390625,
            "standardDeviationNanoseconds": 9696.856552216183,
            "operationsPerSecond": 400.68884573172284
          },
          "gc": {
            "bytesAllocatedPerOperation": 1888742,
            "gen0Collections": 102,
            "gen1Collections": 26,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-analysis",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergAnalysisBenchmarks-20260429-115642",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "GutenbergAnalysisBenchmarks.LeanLucene_English_Analyse",
          "displayInfo": "GutenbergAnalysisBenchmarks.LeanLucene_English_Analyse: DefaultJob",
          "typeName": "GutenbergAnalysisBenchmarks",
          "methodName": "LeanLucene_English_Analyse",
          "parameters": {},
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 6243627431.933333,
            "medianNanoseconds": 6250856936,
            "minNanoseconds": 6180168898,
            "maxNanoseconds": 6280582861,
            "standardDeviationNanoseconds": 30607514.334319696,
            "operationsPerSecond": 0.16016330424929773
          },
          "gc": {
            "bytesAllocatedPerOperation": 1920999792,
            "gen0Collections": 201,
            "gen1Collections": 135,
            "gen2Collections": 43
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
            "meanNanoseconds": 2076734824.8666666,
            "medianNanoseconds": 2076279822,
            "minNanoseconds": 2064270051,
            "maxNanoseconds": 2084182710,
            "standardDeviationNanoseconds": 4795550.453636026,
            "operationsPerSecond": 0.4815251268607216
          },
          "gc": {
            "bytesAllocatedPerOperation": 230081392,
            "gen0Collections": 42,
            "gen1Collections": 29,
            "gen2Collections": 2
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergIndexingBenchmarks-20260429-120136",
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
            "meanNanoseconds": 15200110429.466667,
            "medianNanoseconds": 15198184796,
            "minNanoseconds": 15162427347,
            "maxNanoseconds": 15242279034,
            "standardDeviationNanoseconds": 22390013.625038944,
            "operationsPerSecond": 0.06578899572080855
          },
          "gc": {
            "bytesAllocatedPerOperation": 3345537312,
            "gen0Collections": 501,
            "gen1Collections": 168,
            "gen2Collections": 3
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
            "meanNanoseconds": 14927127894.666666,
            "medianNanoseconds": 14929448546,
            "minNanoseconds": 14791190845,
            "maxNanoseconds": 15079189668,
            "standardDeviationNanoseconds": 78610988.72301525,
            "operationsPerSecond": 0.06699212380683704
          },
          "gc": {
            "bytesAllocatedPerOperation": 1822932016,
            "gen0Collections": 255,
            "gen1Collections": 129,
            "gen2Collections": 3
          }
        },
        {
          "key": "GutenbergIndexingBenchmarks.LuceneNet_Index",
          "displayInfo": "GutenbergIndexingBenchmarks.LuceneNet_Index: DefaultJob",
          "typeName": "GutenbergIndexingBenchmarks",
          "methodName": "LuceneNet_Index",
          "parameters": {},
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 12869073503.933332,
            "medianNanoseconds": 12854618186,
            "minNanoseconds": 12815927517,
            "maxNanoseconds": 12933956639,
            "standardDeviationNanoseconds": 35292039.283192925,
            "operationsPerSecond": 0.07770567163940417
          },
          "gc": {
            "bytesAllocatedPerOperation": 5498019896,
            "gen0Collections": 1130,
            "gen1Collections": 25,
            "gen2Collections": 1
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergSearchBenchmarks-20260429-122413",
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
            "meanNanoseconds": 183967.83776041667,
            "medianNanoseconds": 183900.587890625,
            "minNanoseconds": 183004.69750976562,
            "maxNanoseconds": 184547.1669921875,
            "standardDeviationNanoseconds": 414.78816172715096,
            "operationsPerSecond": 5435.73274640707
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 0,
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
            "meanNanoseconds": 256388.98854166668,
            "medianNanoseconds": 256480.82568359375,
            "minNanoseconds": 254647.126953125,
            "maxNanoseconds": 257476.47412109375,
            "standardDeviationNanoseconds": 772.8418673734402,
            "operationsPerSecond": 3900.3235111147783
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 0,
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
            "meanNanoseconds": 586491.1658854167,
            "medianNanoseconds": 587226.162109375,
            "minNanoseconds": 583547.16015625,
            "maxNanoseconds": 589094.193359375,
            "standardDeviationNanoseconds": 1648.9356107476851,
            "operationsPerSecond": 1705.0555202998078
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 0,
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
            "meanNanoseconds": 234489.51326497397,
            "medianNanoseconds": 234531.48974609375,
            "minNanoseconds": 233922.94409179688,
            "maxNanoseconds": 235299.0205078125,
            "standardDeviationNanoseconds": 347.51590233734674,
            "operationsPerSecond": 4264.583034338071
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 0,
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
            "meanNanoseconds": 135072.69330705915,
            "medianNanoseconds": 135131.40954589844,
            "minNanoseconds": 134525.216796875,
            "maxNanoseconds": 135416.1376953125,
            "standardDeviationNanoseconds": 238.53760605281173,
            "operationsPerSecond": 7403.420895196869
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 0,
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
            "sampleCount": 14,
            "meanNanoseconds": 171448.83700125557,
            "medianNanoseconds": 171491.26135253906,
            "minNanoseconds": 170569.19970703125,
            "maxNanoseconds": 172032.92065429688,
            "standardDeviationNanoseconds": 463.700581511827,
            "operationsPerSecond": 5832.643822440607
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 0,
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
            "meanNanoseconds": 197629.10779622395,
            "medianNanoseconds": 197636.45483398438,
            "minNanoseconds": 196741.40649414062,
            "maxNanoseconds": 198613.19946289062,
            "standardDeviationNanoseconds": 569.0682162478731,
            "operationsPerSecond": 5059.983375683219
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 0,
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
            "meanNanoseconds": 582192.7974609375,
            "medianNanoseconds": 582055.5751953125,
            "minNanoseconds": 580259.587890625,
            "maxNanoseconds": 583944.470703125,
            "standardDeviationNanoseconds": 1065.0015267822866,
            "operationsPerSecond": 1717.6440594270584
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 0,
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
            "meanNanoseconds": 223340.75270182293,
            "medianNanoseconds": 223441.9384765625,
            "minNanoseconds": 222441.35327148438,
            "maxNanoseconds": 224123.724609375,
            "standardDeviationNanoseconds": 563.5923372408056,
            "operationsPerSecond": 4477.463194256701
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 0,
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
            "meanNanoseconds": 125300.98395182291,
            "medianNanoseconds": 125380.41528320312,
            "minNanoseconds": 124475.82861328125,
            "maxNanoseconds": 125938.97631835938,
            "standardDeviationNanoseconds": 437.44620416238894,
            "operationsPerSecond": 7980.783298433561
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 0,
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
            "meanNanoseconds": 221689.92088216144,
            "medianNanoseconds": 221686.32641601562,
            "minNanoseconds": 220322.42895507812,
            "maxNanoseconds": 222740.34790039062,
            "standardDeviationNanoseconds": 710.6357988211911,
            "operationsPerSecond": 4510.804983919619
          },
          "gc": {
            "bytesAllocatedPerOperation": 75458,
            "gen0Collections": 73,
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
            "meanNanoseconds": 251801.10745442708,
            "medianNanoseconds": 251757.4599609375,
            "minNanoseconds": 250519.72216796875,
            "maxNanoseconds": 253478.2236328125,
            "standardDeviationNanoseconds": 760.2654365874591,
            "operationsPerSecond": 3971.3884109147048
          },
          "gc": {
            "bytesAllocatedPerOperation": 76637,
            "gen0Collections": 37,
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
            "meanNanoseconds": 622942.7251302083,
            "medianNanoseconds": 623274.666015625,
            "minNanoseconds": 620188.986328125,
            "maxNanoseconds": 625204.8671875,
            "standardDeviationNanoseconds": 1436.964452376327,
            "operationsPerSecond": 1605.2840167464492
          },
          "gc": {
            "bytesAllocatedPerOperation": 76099,
            "gen0Collections": 18,
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
            "meanNanoseconds": 268723.81744791666,
            "medianNanoseconds": 268564.2412109375,
            "minNanoseconds": 267514.65576171875,
            "maxNanoseconds": 270218.3974609375,
            "standardDeviationNanoseconds": 732.6441748754542,
            "operationsPerSecond": 3721.2927737371747
          },
          "gc": {
            "bytesAllocatedPerOperation": 77180,
            "gen0Collections": 37,
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
            "sampleCount": 13,
            "meanNanoseconds": 179760.3852351262,
            "medianNanoseconds": 179780.54028320312,
            "minNanoseconds": 179155.77392578125,
            "maxNanoseconds": 180265.07202148438,
            "standardDeviationNanoseconds": 293.7082758924744,
            "operationsPerSecond": 5562.960930974877
          },
          "gc": {
            "bytesAllocatedPerOperation": 75394,
            "gen0Collections": 73,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexingBenchmarks-20260429-110423",
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
            "meanNanoseconds": 3137958905.0666666,
            "medianNanoseconds": 3141617128,
            "minNanoseconds": 3094813971,
            "maxNanoseconds": 3156933119,
            "standardDeviationNanoseconds": 16546691.625392295,
            "operationsPerSecond": 0.3186784882317491
          },
          "gc": {
            "bytesAllocatedPerOperation": 375075048,
            "gen0Collections": 52,
            "gen1Collections": 26,
            "gen2Collections": 0
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
            "meanNanoseconds": 2506672801.571429,
            "medianNanoseconds": 2507078424,
            "minNanoseconds": 2491466292,
            "maxNanoseconds": 2523866669,
            "standardDeviationNanoseconds": 7916179.500927634,
            "operationsPerSecond": 0.3989351938446461
          },
          "gc": {
            "bytesAllocatedPerOperation": 822838296,
            "gen0Collections": 145,
            "gen1Collections": 14,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortIndexBenchmarks-20260429-114635",
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
            "sampleCount": 14,
            "meanNanoseconds": 3570472570.928571,
            "medianNanoseconds": 3569187567,
            "minNanoseconds": 3552684175,
            "maxNanoseconds": 3590442301,
            "standardDeviationNanoseconds": 10258935.20530342,
            "operationsPerSecond": 0.2800749705073159
          },
          "gc": {
            "bytesAllocatedPerOperation": 420394560,
            "gen0Collections": 56,
            "gen1Collections": 27,
            "gen2Collections": 0
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
            "meanNanoseconds": 3236678435.4,
            "medianNanoseconds": 3234749704,
            "minNanoseconds": 3225369870,
            "maxNanoseconds": 3253058061,
            "standardDeviationNanoseconds": 8333757.69532885,
            "operationsPerSecond": 0.3089587118271811
          },
          "gc": {
            "bytesAllocatedPerOperation": 408769112,
            "gen0Collections": 55,
            "gen1Collections": 26,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortSearchBenchmarks-20260429-115115",
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
            "sampleCount": 14,
            "meanNanoseconds": 5115.523245130266,
            "medianNanoseconds": 5115.357368469238,
            "minNanoseconds": 5097.908546447754,
            "maxNanoseconds": 5133.956039428711,
            "standardDeviationNanoseconds": 10.4400663302376,
            "operationsPerSecond": 195483.42409585416
          },
          "gc": {
            "bytesAllocatedPerOperation": 1992,
            "gen0Collections": 62,
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
            "meanNanoseconds": 5477.631571451823,
            "medianNanoseconds": 5474.559776306152,
            "minNanoseconds": 5454.393569946289,
            "maxNanoseconds": 5506.556289672852,
            "standardDeviationNanoseconds": 16.337920759056377,
            "operationsPerSecond": 182560.65362478443
          },
          "gc": {
            "bytesAllocatedPerOperation": 1848,
            "gen0Collections": 57,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "phrase",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PhraseQueryBenchmarks-20260429-111459",
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
            "sampleCount": 27,
            "meanNanoseconds": 42662.48986364294,
            "medianNanoseconds": 42581.04229736328,
            "minNanoseconds": 40894.92303466797,
            "maxNanoseconds": 45272.57879638672,
            "standardDeviationNanoseconds": 1036.655480941157,
            "operationsPerSecond": 23439.79461105485
          },
          "gc": {
            "bytesAllocatedPerOperation": 53636,
            "gen0Collections": 217,
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
            "sampleCount": 12,
            "meanNanoseconds": 40908.78769938151,
            "medianNanoseconds": 41033.540618896484,
            "minNanoseconds": 40189.14141845703,
            "maxNanoseconds": 41337.641845703125,
            "standardDeviationNanoseconds": 391.18136180999915,
            "operationsPerSecond": 24444.625622946995
          },
          "gc": {
            "bytesAllocatedPerOperation": 39661,
            "gen0Collections": 159,
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
            "sampleCount": 14,
            "meanNanoseconds": 151289.8838065011,
            "medianNanoseconds": 151299.10119628906,
            "minNanoseconds": 149756.78979492188,
            "maxNanoseconds": 152892.91479492188,
            "standardDeviationNanoseconds": 849.0503781072198,
            "operationsPerSecond": 6609.827272251688
          },
          "gc": {
            "bytesAllocatedPerOperation": 43600,
            "gen0Collections": 44,
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
            "meanNanoseconds": 44638.679170735675,
            "medianNanoseconds": 44658.85418701172,
            "minNanoseconds": 44334.16320800781,
            "maxNanoseconds": 44836.46075439453,
            "standardDeviationNanoseconds": 123.62636704489199,
            "operationsPerSecond": 22402.096535499244
          },
          "gc": {
            "bytesAllocatedPerOperation": 146008,
            "gen0Collections": 571,
            "gen1Collections": 95,
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
            "sampleCount": 14,
            "meanNanoseconds": 59585.917868477954,
            "medianNanoseconds": 59581.535064697266,
            "minNanoseconds": 59381.1083984375,
            "maxNanoseconds": 59747.78369140625,
            "standardDeviationNanoseconds": 104.10623341084815,
            "operationsPerSecond": 16782.48881232756
          },
          "gc": {
            "bytesAllocatedPerOperation": 125408,
            "gen0Collections": 490,
            "gen1Collections": 97,
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
            "meanNanoseconds": 74360.49682617188,
            "medianNanoseconds": 74445.833984375,
            "minNanoseconds": 73948.81201171875,
            "maxNanoseconds": 74649.71350097656,
            "standardDeviationNanoseconds": 229.10175774357015,
            "operationsPerSecond": 13448.000520190722
          },
          "gc": {
            "bytesAllocatedPerOperation": 62704,
            "gen0Collections": 122,
            "gen1Collections": 2,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "prefix",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PrefixQueryBenchmarks-20260429-111940",
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
            "meanNanoseconds": 27840.382257080077,
            "medianNanoseconds": 27834.295043945312,
            "minNanoseconds": 27183.283325195312,
            "maxNanoseconds": 28532.275665283203,
            "standardDeviationNanoseconds": 344.90434742533307,
            "operationsPerSecond": 35919.047043461134
          },
          "gc": {
            "bytesAllocatedPerOperation": 15361,
            "gen0Collections": 121,
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
            "sampleCount": 93,
            "meanNanoseconds": 36115.494975428424,
            "medianNanoseconds": 36018.27850341797,
            "minNanoseconds": 33147.976135253906,
            "maxNanoseconds": 41892.882263183594,
            "standardDeviationNanoseconds": 2046.0518955677796,
            "operationsPerSecond": 27688.94627307091
          },
          "gc": {
            "bytesAllocatedPerOperation": 16473,
            "gen0Collections": 65,
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
            "sampleCount": 15,
            "meanNanoseconds": 102205.19136555989,
            "medianNanoseconds": 102502.7646484375,
            "minNanoseconds": 98947.61486816406,
            "maxNanoseconds": 104936.12060546875,
            "standardDeviationNanoseconds": 1544.4266927171602,
            "operationsPerSecond": 9784.23881056369
          },
          "gc": {
            "bytesAllocatedPerOperation": 40360,
            "gen0Collections": 80,
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
            "sampleCount": 14,
            "meanNanoseconds": 39087.451171875,
            "medianNanoseconds": 39080.57809448242,
            "minNanoseconds": 38903.434020996094,
            "maxNanoseconds": 39332.8525390625,
            "standardDeviationNanoseconds": 122.07087519585839,
            "operationsPerSecond": 25583.658438172617
          },
          "gc": {
            "bytesAllocatedPerOperation": 78192,
            "gen0Collections": 306,
            "gen1Collections": 2,
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
            "meanNanoseconds": 49224.4806640625,
            "medianNanoseconds": 49216.255859375,
            "minNanoseconds": 49026.422912597656,
            "maxNanoseconds": 49428.827209472656,
            "standardDeviationNanoseconds": 116.12293654488803,
            "operationsPerSecond": 20315.09497935798
          },
          "gc": {
            "bytesAllocatedPerOperation": 76664,
            "gen0Collections": 297,
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
            "meanNanoseconds": 127493.39612630209,
            "medianNanoseconds": 127457.48461914062,
            "minNanoseconds": 126642.4482421875,
            "maxNanoseconds": 128359.38354492188,
            "standardDeviationNanoseconds": 489.1738651929011,
            "operationsPerSecond": 7843.543511927034
          },
          "gc": {
            "bytesAllocatedPerOperation": 88224,
            "gen0Collections": 86,
            "gen1Collections": 9,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "query",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.TermQueryBenchmarks-20260429-105953",
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
            "meanNanoseconds": 11649.436268107096,
            "medianNanoseconds": 11646.790939331055,
            "minNanoseconds": 11592.743911743164,
            "maxNanoseconds": 11709.635116577148,
            "standardDeviationNanoseconds": 37.955519748326346,
            "operationsPerSecond": 85841.063634789
          },
          "gc": {
            "bytesAllocatedPerOperation": 480,
            "gen0Collections": 7,
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
            "meanNanoseconds": 84697.8396891276,
            "medianNanoseconds": 84631.91589355469,
            "minNanoseconds": 84335.1025390625,
            "maxNanoseconds": 85204.28894042969,
            "standardDeviationNanoseconds": 271.7755324522536,
            "operationsPerSecond": 11806.676577234672
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
            "meanNanoseconds": 284323.8845377604,
            "medianNanoseconds": 284280.18994140625,
            "minNanoseconds": 283385.013671875,
            "maxNanoseconds": 285431.32470703125,
            "standardDeviationNanoseconds": 677.7686421351774,
            "operationsPerSecond": 3517.1157063563273
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
            "meanNanoseconds": 22575.78525797526,
            "medianNanoseconds": 22591.219848632812,
            "minNanoseconds": 22468.887969970703,
            "maxNanoseconds": 22677.460540771484,
            "standardDeviationNanoseconds": 68.20676246291573,
            "operationsPerSecond": 44295.24769893591
          },
          "gc": {
            "bytesAllocatedPerOperation": 25816,
            "gen0Collections": 202,
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
            "meanNanoseconds": 96300.25434570313,
            "medianNanoseconds": 96336.09008789062,
            "minNanoseconds": 95877.73718261719,
            "maxNanoseconds": 96644.10119628906,
            "standardDeviationNanoseconds": 216.87678974223985,
            "operationsPerSecond": 10384.188565174018
          },
          "gc": {
            "bytesAllocatedPerOperation": 25400,
            "gen0Collections": 49,
            "gen1Collections": 0,
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
            "sampleCount": 13,
            "meanNanoseconds": 318612.6748046875,
            "medianNanoseconds": 318721.505859375,
            "minNanoseconds": 317629.6552734375,
            "maxNanoseconds": 319417.39453125,
            "standardDeviationNanoseconds": 542.6784832092683,
            "operationsPerSecond": 3138.607089667758
          },
          "gc": {
            "bytesAllocatedPerOperation": 25216,
            "gen0Collections": 12,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "schemajson",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SchemaAndJsonBenchmarks-20260429-114106",
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
            "meanNanoseconds": 3131877324.9333334,
            "medianNanoseconds": 3133526924,
            "minNanoseconds": 3104906495,
            "maxNanoseconds": 3154615833,
            "standardDeviationNanoseconds": 16454531.609245272,
            "operationsPerSecond": 0.31929730837119763
          },
          "gc": {
            "bytesAllocatedPerOperation": 375125600,
            "gen0Collections": 51,
            "gen1Collections": 24,
            "gen2Collections": 0
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
            "meanNanoseconds": 3175945754.866667,
            "medianNanoseconds": 3175478145,
            "minNanoseconds": 3149869377,
            "maxNanoseconds": 3203228795,
            "standardDeviationNanoseconds": 15709258.326702178,
            "operationsPerSecond": 0.31486683878893335
          },
          "gc": {
            "bytesAllocatedPerOperation": 379075904,
            "gen0Collections": 52,
            "gen1Collections": 27,
            "gen2Collections": 0
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
            "meanNanoseconds": 217013232.24444446,
            "medianNanoseconds": 216905887.66666666,
            "minNanoseconds": 216096795,
            "maxNanoseconds": 218265579.33333334,
            "standardDeviationNanoseconds": 709891.6215112935,
            "operationsPerSecond": 4.60801394300969
          },
          "gc": {
            "bytesAllocatedPerOperation": 99482744,
            "gen0Collections": 71,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "suggester",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SuggesterBenchmarks-20260429-113803",
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
            "meanNanoseconds": 2612384.1744791665,
            "medianNanoseconds": 2609239.515625,
            "minNanoseconds": 2598427.48828125,
            "maxNanoseconds": 2633367.93359375,
            "standardDeviationNanoseconds": 8691.533332964334,
            "operationsPerSecond": 382.7920907534095
          },
          "gc": {
            "bytesAllocatedPerOperation": 13176,
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
            "meanNanoseconds": 2753505.916573661,
            "medianNanoseconds": 2752754.765625,
            "minNanoseconds": 2739457.19140625,
            "maxNanoseconds": 2774764.609375,
            "standardDeviationNanoseconds": 9606.848612925394,
            "operationsPerSecond": 363.173361633577
          },
          "gc": {
            "bytesAllocatedPerOperation": 11416,
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
            "meanNanoseconds": 9032340.51450893,
            "medianNanoseconds": 9038632.5390625,
            "minNanoseconds": 8988939.421875,
            "maxNanoseconds": 9067387.078125,
            "standardDeviationNanoseconds": 22228.709029412286,
            "operationsPerSecond": 110.71327508010454
          },
          "gc": {
            "bytesAllocatedPerOperation": 5349992,
            "gen0Collections": 81,
            "gen1Collections": 3,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "wildcard",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.WildcardQueryBenchmarks-20260429-112938",
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
            "sampleCount": 12,
            "meanNanoseconds": 30446.06883239746,
            "medianNanoseconds": 30426.495819091797,
            "minNanoseconds": 29942.80828857422,
            "maxNanoseconds": 30775.006896972656,
            "standardDeviationNanoseconds": 282.5818166962954,
            "operationsPerSecond": 32844.96285891289
          },
          "gc": {
            "bytesAllocatedPerOperation": 18962,
            "gen0Collections": 75,
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
            "sampleCount": 14,
            "meanNanoseconds": 338934.17731584824,
            "medianNanoseconds": 339221.4807128906,
            "minNanoseconds": 333506.7294921875,
            "maxNanoseconds": 343402.7900390625,
            "standardDeviationNanoseconds": 2712.573136611698,
            "operationsPerSecond": 2950.425383239275
          },
          "gc": {
            "bytesAllocatedPerOperation": 417234,
            "gen0Collections": 205,
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
            "sampleCount": 19,
            "meanNanoseconds": 50144.21352667557,
            "medianNanoseconds": 50127.17297363281,
            "minNanoseconds": 48222.79797363281,
            "maxNanoseconds": 52034.535217285156,
            "standardDeviationNanoseconds": 1072.1411988520438,
            "operationsPerSecond": 19942.480491154238
          },
          "gc": {
            "bytesAllocatedPerOperation": 65619,
            "gen0Collections": 258,
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
            "meanNanoseconds": 52925.60423060826,
            "medianNanoseconds": 52948.83190917969,
            "minNanoseconds": 52718.18115234375,
            "maxNanoseconds": 53095.19775390625,
            "standardDeviationNanoseconds": 113.03243715537653,
            "operationsPerSecond": 18894.44654505567
          },
          "gc": {
            "bytesAllocatedPerOperation": 97672,
            "gen0Collections": 381,
            "gen1Collections": 1,
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
            "sampleCount": 12,
            "meanNanoseconds": 352863.73506673175,
            "medianNanoseconds": 353018.0568847656,
            "minNanoseconds": 351458.56787109375,
            "maxNanoseconds": 353536.58740234375,
            "standardDeviationNanoseconds": 594.5269911786463,
            "operationsPerSecond": 2833.9551521521053
          },
          "gc": {
            "bytesAllocatedPerOperation": 324528,
            "gen0Collections": 158,
            "gen1Collections": 4,
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
            "sampleCount": 15,
            "meanNanoseconds": 275892.1685872396,
            "medianNanoseconds": 276012.61865234375,
            "minNanoseconds": 274610.97021484375,
            "maxNanoseconds": 277449.8857421875,
            "standardDeviationNanoseconds": 943.7977515783124,
            "operationsPerSecond": 3624.604515310086
          },
          "gc": {
            "bytesAllocatedPerOperation": 351000,
            "gen0Collections": 170,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    }
  ]
}</code></pre>

</details>

