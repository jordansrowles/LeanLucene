---
title: Benchmarks - debian
---

# Benchmarks: debian

**.NET** 10.0.3 &nbsp;&middot;&nbsp; **Commit** `d68b625` &nbsp;&middot;&nbsp; 4 May 2026 07:41 UTC &nbsp;&middot;&nbsp; 76 benchmarks

## Analysis

| Method             | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1      | Allocated | Alloc Ratio |
|------------------- |-------------- |--------:|---------:|---------:|------:|------------:|----------:|----------:|------------:|
| LeanLucene_Analyse | 100000        | 1.560 s | 0.0029 s | 0.0027 s |  1.00 |  49000.0000 | 2000.0000 | 199.73 MB |        1.00 |
| LuceneNet_Analyse  | 100000        | 2.238 s | 0.0035 s | 0.0033 s |  1.43 | 144000.0000 |         - | 576.92 MB |        2.89 |

## Block-Join

| Method                           | BlockCount | Mean          | Error       | StdDev      | Ratio | Gen0      | Gen1     | Allocated  | Alloc Ratio |
|--------------------------------- |----------- |--------------:|------------:|------------:|------:|----------:|---------:|-----------:|------------:|
| LeanLucene_IndexBlocks           | 500        | 71,825.950 μs | 375.5504 μs | 351.2901 μs | 1.000 | 1285.7143 | 571.4286 | 11041424 B |       1.000 |
| LeanLucene_BlockJoinQuery        | 500        |      7.173 μs |   0.0143 μs |   0.0133 μs | 0.000 |    0.1678 |        - |      720 B |       0.000 |
| LuceneNet_IndexBlocks            | 500        | 55,373.948 μs | 281.7646 μs | 263.5627 μs | 0.771 | 5100.0000 | 600.0000 | 28715434 B |       2.601 |
| LuceneNet_ToParentBlockJoinQuery | 500        |     21.861 μs |   0.0810 μs |   0.0758 μs | 0.000 |    3.0518 |        - |    12888 B |       0.001 |

## Boolean queries

| Method                  | BooleanType | DocumentCount | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------------------------ |------------ |-------------- |---------:|--------:|--------:|------:|--------:|---------:|--------:|----------:|------------:|
| **LeanLucene_BooleanQuery** | **Must**        | **100000**        | **266.9 μs** | **3.11 μs** | **2.76 μs** |  **1.00** |    **0.00** |   **2.9297** |       **-** |  **12.94 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Must        | 100000        | 484.3 μs | 0.75 μs | 0.70 μs |  1.81 |    0.02 |  35.1563 |       - | 144.09 KB |       11.14 |
|                         |             |               |          |         |         |       |         |          |         |           |             |
| **LeanLucene_BooleanQuery** | **MustNot**     | **100000**        | **174.7 μs** | **1.44 μs** | **1.34 μs** |  **1.00** |    **0.00** |   **3.1738** |       **-** |   **13.3 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | MustNot     | 100000        | 362.1 μs | 1.13 μs | 1.06 μs |  2.07 |    0.02 |  36.1328 |       - | 149.06 KB |       11.21 |
|                         |             |               |          |         |         |       |         |          |         |           |             |
| **LeanLucene_BooleanQuery** | **Should**      | **100000**        | **223.0 μs** | **2.03 μs** | **1.90 μs** |  **1.00** |    **0.00** |   **3.1738** |       **-** |  **13.69 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Should      | 100000        | 586.5 μs | 1.53 μs | 1.35 μs |  2.63 |    0.02 | 169.9219 | 40.0391 | 695.01 KB |       50.76 |

## Deletion

| Method                     | DocumentCount | Mean     | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated  | Alloc Ratio |
|--------------------------- |-------------- |---------:|---------:|---------:|------:|------------:|-----------:|----------:|-----------:|------------:|
| LeanLucene_DeleteDocuments | 100000        | 10.129 s | 0.0528 s | 0.0494 s |  1.00 | 153000.0000 | 75000.0000 | 5000.0000 |  962.69 MB |        1.00 |
| LuceneNet_DeleteDocuments  | 100000        |  7.221 s | 0.0214 s | 0.0200 s |  0.71 | 338000.0000 | 33000.0000 | 1000.0000 | 1959.99 MB |        2.04 |

## Fuzzy queries

| Method                | QueryTerm | DocumentCount | Mean     | Error     | StdDev    | Ratio | Gen0     | Gen1     | Allocated  | Alloc Ratio |
|---------------------- |---------- |-------------- |---------:|----------:|----------:|------:|---------:|---------:|-----------:|------------:|
| **LeanLucene_FuzzyQuery** | **goverment** | **100000**        | **6.902 ms** | **0.0468 ms** | **0.0438 ms** |  **1.00** |        **-** |        **-** |   **25.88 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | goverment | 100000        | 8.633 ms | 0.0094 ms | 0.0078 ms |  1.25 | 593.7500 | 203.1250 | 2870.85 KB |      110.93 |
|                       |           |               |          |           |           |       |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **markts**    | **100000**        | **7.531 ms** | **0.0319 ms** | **0.0283 ms** |  **1.00** |   **7.8125** |        **-** |   **47.67 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | markts    | 100000        | 9.225 ms | 0.0224 ms | 0.0209 ms |  1.23 | 625.0000 | 187.5000 | 2806.02 KB |       58.87 |
|                       |           |               |          |           |           |       |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **presiden**  | **100000**        | **7.954 ms** | **0.0671 ms** | **0.0561 ms** |  **1.00** |        **-** |        **-** |   **30.61 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | presiden  | 100000        | 8.713 ms | 0.0298 ms | 0.0279 ms |  1.10 | 593.7500 | 218.7500 | 2844.58 KB |       92.92 |

## gutenberg-analysis

| Method                      | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0       | Gen1      | Gen2      | Allocated | Alloc Ratio |
|---------------------------- |---------:|--------:|--------:|------:|--------:|-----------:|----------:|----------:|----------:|------------:|
| LeanLucene_Standard_Analyse | 124.1 ms | 0.47 ms | 0.41 ms |  1.00 |    0.00 |  1400.0000 |  600.0000 |         - |   7.23 MB |        1.00 |
| LeanLucene_English_Analyse  | 396.1 ms | 2.28 ms | 2.13 ms |  3.19 |    0.02 | 11000.0000 | 6000.0000 | 2000.0000 | 113.03 MB |       15.62 |

## gutenberg-index

| Method                    | Mean     | Error   | StdDev  | Ratio | Gen0       | Gen1       | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |---------:|--------:|--------:|------:|-----------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Standard_Index | 813.2 ms | 6.65 ms | 6.22 ms |  1.00 | 12000.0000 |  6000.0000 | 1000.0000 |  79.53 MB |        1.00 |
| LeanLucene_English_Index  | 844.4 ms | 6.95 ms | 6.50 ms |  1.04 | 28000.0000 | 10000.0000 | 1000.0000 | 173.92 MB |        2.19 |
| LuceneNet_Index           | 648.7 ms | 2.99 ms | 2.65 ms |  0.80 | 41000.0000 |  3000.0000 |         - | 207.68 MB |        2.61 |

## gutenberg-search

| Method                     | SearchTerm | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------- |----------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| **LeanLucene_Standard_Search** | **death**      | **11.42 μs** | **0.032 μs** | **0.030 μs** |  **1.00** |    **0.00** | **0.1068** |      **-** |     **472 B** |        **1.00** |
| LeanLucene_English_Search  | death      | 11.83 μs | 0.039 μs | 0.036 μs |  1.04 |    0.00 | 0.1068 |      - |     472 B |        1.00 |
| LuceneNet_Search           | death      | 22.71 μs | 0.426 μs | 0.398 μs |  1.99 |    0.03 | 2.6550 | 0.0305 |   11231 B |       23.79 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **love**       | **15.33 μs** | **0.037 μs** | **0.034 μs** |  **1.00** |    **0.00** | **0.0916** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | love       | 20.02 μs | 0.057 μs | 0.053 μs |  1.31 |    0.00 | 0.0916 |      - |     464 B |        1.00 |
| LuceneNet_Search           | love       | 31.41 μs | 0.055 μs | 0.052 μs |  2.05 |    0.01 | 2.6245 | 0.0610 |   11175 B |       24.08 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **man**        | **39.40 μs** | **0.081 μs** | **0.075 μs** |  **1.00** |    **0.00** | **0.0610** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | man        | 40.10 μs | 0.099 μs | 0.093 μs |  1.02 |    0.00 | 0.0610 |      - |     464 B |        1.00 |
| LuceneNet_Search           | man        | 49.33 μs | 0.172 μs | 0.161 μs |  1.25 |    0.00 | 2.6245 | 0.0610 |   11038 B |       23.79 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **night**      | **25.40 μs** | **0.043 μs** | **0.040 μs** |  **1.00** |    **0.00** | **0.0916** |      **-** |     **472 B** |        **1.00** |
| LeanLucene_English_Search  | night      | 26.26 μs | 0.041 μs | 0.037 μs |  1.03 |    0.00 | 0.0916 |      - |     472 B |        1.00 |
| LuceneNet_Search           | night      | 37.07 μs | 0.081 μs | 0.075 μs |  1.46 |    0.00 | 2.6245 | 0.0610 |   11223 B |       23.78 |
|                            |            |          |          |          |       |         |        |        |           |             |
| **LeanLucene_Standard_Search** | **sea**        | **12.86 μs** | **0.032 μs** | **0.030 μs** |  **1.00** |    **0.00** | **0.1068** |      **-** |     **464 B** |        **1.00** |
| LeanLucene_English_Search  | sea        | 13.79 μs | 0.018 μs | 0.015 μs |  1.07 |    0.00 | 0.1068 |      - |     464 B |        1.00 |
| LuceneNet_Search           | sea        | 26.85 μs | 0.038 μs | 0.034 μs |  2.09 |    0.01 | 2.6550 | 0.0305 |   11271 B |       24.29 |

## Indexing

| Method                    | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |--------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_IndexDocuments | 100000        | 9.682 s | 0.0285 s | 0.0253 s |  1.00 | 154000.0000 | 76000.0000 | 6000.0000 |  936.9 MB |        1.00 |
| LuceneNet_IndexDocuments  | 100000        | 7.093 s | 0.0134 s | 0.0119 s |  0.73 | 330000.0000 | 33000.0000 | 1000.0000 | 1925.7 MB |        2.06 |

## Index-sort (index)

| Method                    | DocumentCount | Mean    | Error   | StdDev  | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |--------:|--------:|--------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Index_Unsorted | 100000        | 10.26 s | 0.066 s | 0.062 s |  1.00 | 160000.0000 | 77000.0000 | 8000.0000 | 967.81 MB |        1.00 |
| LeanLucene_Index_Sorted   | 100000        | 10.97 s | 0.063 s | 0.059 s |  1.07 | 159000.0000 | 72000.0000 | 5000.0000 | 978.21 MB |        1.01 |

## Index-sort (search)

| Method                                   | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |-------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| LeanLucene_SortedSearch_EarlyTermination | 100000        | 249.7 μs | 0.49 μs | 0.46 μs |  1.00 | 28.3203 | 0.9766 | 117.66 KB |        1.00 |
| LeanLucene_SortedSearch_PostSort         | 100000        | 240.4 μs | 0.42 μs | 0.40 μs |  0.96 | 28.5645 | 0.4883 | 117.66 KB |        1.00 |

## Phrase queries

| Method                 | PhraseType     | DocumentCount | Mean       | Error   | StdDev  | Ratio | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------------------- |--------------- |-------------- |-----------:|--------:|--------:|------:|--------:|--------:|----------:|------------:|
| **LeanLucene_PhraseQuery** | **ExactThreeWord** | **100000**        |   **448.3 μs** | **4.44 μs** | **3.94 μs** |  **1.00** | **14.6484** |       **-** |  **59.77 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactThreeWord | 100000        |   346.3 μs | 0.56 μs | 0.50 μs |  0.77 | 90.3320 |  0.4883 | 369.88 KB |        6.19 |
|                        |                |               |            |         |         |       |         |         |           |             |
| **LeanLucene_PhraseQuery** | **ExactTwoWord**   | **100000**        |   **339.7 μs** | **4.21 μs** | **3.94 μs** |  **1.00** | **10.2539** |       **-** |  **42.92 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactTwoWord   | 100000        |   405.6 μs | 1.89 μs | 1.77 μs |  1.19 | 72.2656 | 18.0664 | 297.27 KB |        6.93 |
|                        |                |               |            |         |         |       |         |         |           |             |
| **LeanLucene_PhraseQuery** | **SlopTwoWord**    | **100000**        |   **995.3 μs** | **6.77 μs** | **6.00 μs** |  **1.00** | **11.7188** |       **-** |  **48.69 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | SlopTwoWord    | 100000        | 1,038.9 μs | 2.73 μs | 2.56 μs |  1.04 | 37.1094 |       - | 155.61 KB |        3.20 |

## Prefix queries

| Method                 | QueryPrefix | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| **LeanLucene_PrefixQuery** | **gov**         | **100000**        | **151.4 μs** | **1.50 μs** | **1.33 μs** |  **1.00** |  **5.8594** |      **-** |  **23.67 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | gov         | 100000        | 185.2 μs | 0.30 μs | 0.28 μs |  1.22 | 26.8555 | 0.2441 | 110.04 KB |        4.65 |
|                        |             |               |          |         |         |       |         |        |           |             |
| **LeanLucene_PrefixQuery** | **mark**        | **100000**        | **242.8 μs** | **1.46 μs** | **1.22 μs** |  **1.00** |  **8.5449** |      **-** |  **34.51 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | mark        | 100000        | 283.4 μs | 0.51 μs | 0.43 μs |  1.17 | 30.7617 |      - | 126.09 KB |        3.65 |
|                        |             |               |          |         |         |       |         |        |           |             |
| **LeanLucene_PrefixQuery** | **pres**        | **100000**        | **291.0 μs** | **2.96 μs** | **2.77 μs** |  **1.00** | **15.6250** |      **-** |     **63 KB** |        **1.00** |
| LuceneNet_PrefixQuery  | pres        | 100000        | 357.6 μs | 0.82 μs | 0.77 μs |  1.23 | 32.2266 |      - | 133.65 KB |        2.12 |

## Term queries

| Method               | QueryTerm  | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0    | Gen1   | Allocated | Alloc Ratio |
|--------------------- |----------- |-------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| **LeanLucene_TermQuery** | **government** | **100000**        | **105.9 μs** | **0.16 μs** | **0.14 μs** |  **1.00** |       **-** |      **-** |     **480 B** |        **1.00** |
| LuceneNet_TermQuery  | government | 100000        | 135.5 μs | 0.42 μs | 0.39 μs |  1.28 | 14.4043 |      - |   60896 B |      126.87 |
|                      |            |               |          |         |         |       |         |        |           |             |
| **LeanLucene_TermQuery** | **people**     | **100000**        | **149.0 μs** | **0.16 μs** | **0.15 μs** |  **1.00** |       **-** |      **-** |     **472 B** |        **1.00** |
| LuceneNet_TermQuery  | people     | 100000        | 177.8 μs | 0.27 μs | 0.21 μs |  1.19 | 13.9160 | 0.2441 |   58688 B |      124.34 |
|                      |            |               |          |         |         |       |         |        |           |             |
| **LeanLucene_TermQuery** | **said**       | **100000**        | **682.7 μs** | **1.27 μs** | **1.13 μs** |  **1.00** |       **-** |      **-** |     **464 B** |        **1.00** |
| LuceneNet_TermQuery  | said       | 100000        | 755.2 μs | 1.03 μs | 0.86 μs |  1.11 | 13.6719 |      - |   58720 B |      126.55 |

## Schema and JSON

| Method                      | DocumentCount | Mean       | Error    | StdDev   | Ratio | Gen0        | Gen1       | Gen2      | Allocated | Alloc Ratio |
|---------------------------- |-------------- |-----------:|---------:|---------:|------:|------------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Index_NoSchema   | 100000        | 9,932.6 ms | 35.31 ms | 33.03 ms |  1.00 | 150000.0000 | 70000.0000 | 2000.0000 | 936.87 MB |        1.00 |
| LeanLucene_Index_WithSchema | 100000        | 9,920.2 ms | 47.30 ms | 44.24 ms |  1.00 | 151000.0000 | 70000.0000 | 2000.0000 | 940.74 MB |        1.00 |
| LeanLucene_JsonMapping      | 100000        |   431.2 ms |  2.32 ms |  2.06 ms |  0.04 |  51000.0000 |  1000.0000 |         - | 215.88 MB |        0.23 |

## Suggester

| Method                 | DocumentCount | Mean      | Error     | StdDev    | Ratio | Gen0      | Gen1    | Allocated  | Alloc Ratio |
|----------------------- |-------------- |----------:|----------:|----------:|------:|----------:|--------:|-----------:|------------:|
| LeanLucene_DidYouMean  | 100000        |  4.684 ms | 0.0246 ms | 0.0218 ms |  1.00 |         - |       - |   24.91 KB |        1.00 |
| LeanLucene_SpellIndex  | 100000        |  4.694 ms | 0.0201 ms | 0.0168 ms |  1.00 |         - |       - |    23.2 KB |        0.93 |
| LuceneNet_SpellChecker | 100000        | 10.308 ms | 0.0253 ms | 0.0237 ms |  2.20 | 1296.8750 | 31.2500 | 5351.15 KB |      214.78 |

## Wildcard queries

| Method                   | WildcardPattern | DocumentCount | Mean       | Error   | StdDev  | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------- |---------------- |-------------- |-----------:|--------:|--------:|------:|--------:|--------:|-------:|----------:|------------:|
| **LeanLucene_WildcardQuery** | **gov***            | **100000**        |   **150.0 μs** | **0.72 μs** | **0.56 μs** |  **1.00** |    **0.00** |  **6.1035** |      **-** |  **24.37 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | gov*            | 100000        |   205.5 μs | 0.35 μs | 0.31 μs |  1.37 |    0.01 | 31.4941 |      - | 129.06 KB |        5.30 |
|                          |                 |               |            |         |         |       |         |         |        |           |             |
| **LeanLucene_WildcardQuery** | **m*rket**          | **100000**        |   **538.2 μs** | **7.62 μs** | **7.13 μs** |  **1.00** |    **0.00** |  **1.9531** |      **-** |  **10.17 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | m*rket          | 100000        | 1,172.9 μs | 2.83 μs | 2.64 μs |  2.18 |    0.03 | 97.6563 | 9.7656 | 404.52 KB |       39.77 |
|                          |                 |               |            |         |         |       |         |         |        |           |             |
| **LeanLucene_WildcardQuery** | **pre*dent**        | **100000**        |   **107.3 μs** | **0.99 μs** | **0.88 μs** |  **1.00** |    **0.00** |  **2.0752** |      **-** |   **8.62 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | pre*dent        | 100000        |   411.2 μs | 1.14 μs | 1.07 μs |  3.83 |    0.03 | 92.7734 |      - |  379.5 KB |       44.00 |

<details>
<summary>Full data (report.json)</summary>

<pre><code class="lang-json">{
  "schemaVersion": 2,
  "runId": "2026-05-04 07-41 (d68b625)",
  "runType": "full",
  "generatedAtUtc": "2026-05-04T07:41:56.9316786\u002B00:00",
  "commandLineArgs": [],
  "hostMachineName": "debian",
  "commitHash": "d68b625",
  "dotnetVersion": "10.0.3",
  "provenance": {
    "sourceCommit": "d68b625",
    "sourceRef": "",
    "sourceManifestPath": "",
    "gitCommitHash": "d68b625",
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.AnalysisBenchmarks-20260504-085749",
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
            "meanNanoseconds": 1560418439.8,
            "medianNanoseconds": 1560922613,
            "minNanoseconds": 1553045237,
            "maxNanoseconds": 1563773178,
            "standardDeviationNanoseconds": 2749688.1230703034,
            "operationsPerSecond": 0.6408537444149729
          },
          "gc": {
            "bytesAllocatedPerOperation": 209429200,
            "gen0Collections": 49,
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
            "sampleCount": 15,
            "meanNanoseconds": 2237735253.733333,
            "medianNanoseconds": 2238531833,
            "minNanoseconds": 2231767422,
            "maxNanoseconds": 2241889329,
            "standardDeviationNanoseconds": 3259410.6834062254,
            "operationsPerSecond": 0.4468803887017674
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BlockJoinBenchmarks-20260504-100406",
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
            "meanNanoseconds": 7173.223811340332,
            "medianNanoseconds": 7173.393768310547,
            "minNanoseconds": 7147.521583557129,
            "maxNanoseconds": 7197.641792297363,
            "standardDeviationNanoseconds": 13.346611236241413,
            "operationsPerSecond": 139407.33292317946
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
            "meanNanoseconds": 71825949.86666666,
            "medianNanoseconds": 71753474.14285715,
            "minNanoseconds": 71212941.42857143,
            "maxNanoseconds": 72471430.14285715,
            "standardDeviationNanoseconds": 351290.1035693232,
            "operationsPerSecond": 13.922544732876341
          },
          "gc": {
            "bytesAllocatedPerOperation": 11041424,
            "gen0Collections": 9,
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
            "sampleCount": 15,
            "meanNanoseconds": 55373948.36,
            "medianNanoseconds": 55421769.9,
            "minNanoseconds": 54983908.7,
            "maxNanoseconds": 55758680.3,
            "standardDeviationNanoseconds": 263562.74038592697,
            "operationsPerSecond": 18.05903370839204
          },
          "gc": {
            "bytesAllocatedPerOperation": 28715434,
            "gen0Collections": 51,
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
            "meanNanoseconds": 21861.00103149414,
            "medianNanoseconds": 21858.42788696289,
            "minNanoseconds": 21752.319366455078,
            "maxNanoseconds": 21999.926635742188,
            "standardDeviationNanoseconds": 75.7602397974976,
            "operationsPerSecond": 45743.559435331714
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BooleanQueryBenchmarks-20260504-090054",
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
            "meanNanoseconds": 266892.92124720983,
            "medianNanoseconds": 266117.3645019531,
            "minNanoseconds": 263618.84716796875,
            "maxNanoseconds": 273198.58837890625,
            "standardDeviationNanoseconds": 2756.0323665259907,
            "operationsPerSecond": 3746.8209922051437
          },
          "gc": {
            "bytesAllocatedPerOperation": 13247,
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
            "meanNanoseconds": 174673.3627278646,
            "medianNanoseconds": 174698.6650390625,
            "minNanoseconds": 172668.5712890625,
            "maxNanoseconds": 177208.80102539062,
            "standardDeviationNanoseconds": 1344.0046456729872,
            "operationsPerSecond": 5724.97136588575
          },
          "gc": {
            "bytesAllocatedPerOperation": 13621,
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
            "meanNanoseconds": 222994.621484375,
            "medianNanoseconds": 221927.39233398438,
            "minNanoseconds": 220919.71826171875,
            "maxNanoseconds": 226998.54638671875,
            "standardDeviationNanoseconds": 1901.2586273421132,
            "operationsPerSecond": 4484.413091864949
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
            "meanNanoseconds": 484314.34791666665,
            "medianNanoseconds": 484181.267578125,
            "minNanoseconds": 483411.7626953125,
            "maxNanoseconds": 485789.828125,
            "standardDeviationNanoseconds": 700.1152740488092,
            "operationsPerSecond": 2064.7746743444914
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
            "meanNanoseconds": 362106.98463541665,
            "medianNanoseconds": 362176.11962890625,
            "minNanoseconds": 359539.83544921875,
            "maxNanoseconds": 363771.28076171875,
            "standardDeviationNanoseconds": 1055.724417453724,
            "operationsPerSecond": 2761.6147780381502
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
            "meanNanoseconds": 586525.6107003348,
            "medianNanoseconds": 586527.3911132812,
            "minNanoseconds": 584535.630859375,
            "maxNanoseconds": 589495.328125,
            "standardDeviationNanoseconds": 1352.4047972545673,
            "operationsPerSecond": 1704.9553877211952
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.DeletionBenchmarks-20260504-092657",
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
            "meanNanoseconds": 10129035915.4,
            "medianNanoseconds": 10145039216,
            "minNanoseconds": 10026701205,
            "maxNanoseconds": 10206884116,
            "standardDeviationNanoseconds": 49376224.217098646,
            "operationsPerSecond": 0.09872607900220971
          },
          "gc": {
            "bytesAllocatedPerOperation": 1009450344,
            "gen0Collections": 153,
            "gen1Collections": 75,
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
            "meanNanoseconds": 7221195861.266666,
            "medianNanoseconds": 7223373025,
            "minNanoseconds": 7166746953,
            "maxNanoseconds": 7245555591,
            "standardDeviationNanoseconds": 20044930.327429064,
            "operationsPerSecond": 0.13848121823752757
          },
          "gc": {
            "bytesAllocatedPerOperation": 2055200944,
            "gen0Collections": 338,
            "gen1Collections": 33,
            "gen2Collections": 1
          }
        }
      ]
    },
    {
      "suiteName": "fuzzy",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.FuzzyQueryBenchmarks-20260504-091630",
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
            "meanNanoseconds": 6902384.900520833,
            "medianNanoseconds": 6885163.6484375,
            "minNanoseconds": 6838227,
            "maxNanoseconds": 6977327.5078125,
            "standardDeviationNanoseconds": 43810.33550608578,
            "operationsPerSecond": 144.87746111123752
          },
          "gc": {
            "bytesAllocatedPerOperation": 26502,
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
            "sampleCount": 14,
            "meanNanoseconds": 7530569.005580357,
            "medianNanoseconds": 7532593.46875,
            "minNanoseconds": 7483547.46875,
            "maxNanoseconds": 7579124.625,
            "standardDeviationNanoseconds": 28262.380832794064,
            "operationsPerSecond": 132.79209037975386
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
            "meanNanoseconds": 7954493.967548077,
            "medianNanoseconds": 7944599.4375,
            "minNanoseconds": 7896816.21875,
            "maxNanoseconds": 8088962.5625,
            "standardDeviationNanoseconds": 56057.3357065048,
            "operationsPerSecond": 125.71509942426215
          },
          "gc": {
            "bytesAllocatedPerOperation": 31348,
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
            "sampleCount": 13,
            "meanNanoseconds": 8633291.15625,
            "medianNanoseconds": 8635480,
            "minNanoseconds": 8618986.53125,
            "maxNanoseconds": 8644380.890625,
            "standardDeviationNanoseconds": 7818.343294627064,
            "operationsPerSecond": 115.83068170660017
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
            "meanNanoseconds": 9224985.638541667,
            "medianNanoseconds": 9220252.4375,
            "minNanoseconds": 9186158.40625,
            "maxNanoseconds": 9266213.25,
            "standardDeviationNanoseconds": 20944.923938399155,
            "operationsPerSecond": 108.40125276965581
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
            "meanNanoseconds": 8712656.053125,
            "medianNanoseconds": 8705244.25,
            "minNanoseconds": 8677074.484375,
            "maxNanoseconds": 8767133.296875,
            "standardDeviationNanoseconds": 27892.5797290698,
            "operationsPerSecond": 114.77556257271586
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergAnalysisBenchmarks-20260504-100700",
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
            "meanNanoseconds": 396106326.3333333,
            "medianNanoseconds": 395647138,
            "minNanoseconds": 393269908,
            "maxNanoseconds": 400834400,
            "standardDeviationNanoseconds": 2131081.9650833863,
            "operationsPerSecond": 2.5245746748272713
          },
          "gc": {
            "bytesAllocatedPerOperation": 118518472,
            "gen0Collections": 11,
            "gen1Collections": 6,
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
            "meanNanoseconds": 124086340.72857141,
            "medianNanoseconds": 124137505.7,
            "minNanoseconds": 123461229,
            "maxNanoseconds": 124664450.2,
            "standardDeviationNanoseconds": 413986.59849615436,
            "operationsPerSecond": 8.058904744297498
          },
          "gc": {
            "bytesAllocatedPerOperation": 7585864,
            "gen0Collections": 7,
            "gen1Collections": 3,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergIndexingBenchmarks-20260504-101150",
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
            "meanNanoseconds": 844415314.6666666,
            "medianNanoseconds": 844500615,
            "minNanoseconds": 831285917,
            "maxNanoseconds": 857122977,
            "standardDeviationNanoseconds": 6498040.571008768,
            "operationsPerSecond": 1.184251378002009
          },
          "gc": {
            "bytesAllocatedPerOperation": 182372176,
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
            "meanNanoseconds": 813218227.4,
            "medianNanoseconds": 811114912,
            "minNanoseconds": 802212469,
            "maxNanoseconds": 823224513,
            "standardDeviationNanoseconds": 6219402.594758298,
            "operationsPerSecond": 1.229682225885632
          },
          "gc": {
            "bytesAllocatedPerOperation": 83390744,
            "gen0Collections": 12,
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
            "meanNanoseconds": 648657386,
            "medianNanoseconds": 648561231,
            "minNanoseconds": 645033997,
            "maxNanoseconds": 652552292,
            "standardDeviationNanoseconds": 2648243.674701114,
            "operationsPerSecond": 1.5416459005679155
          },
          "gc": {
            "bytesAllocatedPerOperation": 217768544,
            "gen0Collections": 41,
            "gen1Collections": 3,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "gutenberg-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.GutenbergSearchBenchmarks-20260504-101748",
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
            "meanNanoseconds": 11831.331879679363,
            "medianNanoseconds": 11826.690399169922,
            "minNanoseconds": 11784.416931152344,
            "maxNanoseconds": 11897.637664794922,
            "standardDeviationNanoseconds": 36.14488777473316,
            "operationsPerSecond": 84521.33793301221
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
            "meanNanoseconds": 20015.517006429036,
            "medianNanoseconds": 20001.230560302734,
            "minNanoseconds": 19932.61883544922,
            "maxNanoseconds": 20107.420501708984,
            "standardDeviationNanoseconds": 53.34755913464084,
            "operationsPerSecond": 49961.23755778067
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
            "meanNanoseconds": 40097.540889485674,
            "medianNanoseconds": 40088.66174316406,
            "minNanoseconds": 39952.007080078125,
            "maxNanoseconds": 40228.703125,
            "standardDeviationNanoseconds": 92.62923449538538,
            "operationsPerSecond": 24939.18524221067
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
            "meanNanoseconds": 26259.473068237305,
            "medianNanoseconds": 26259.686325073242,
            "minNanoseconds": 26189.34048461914,
            "maxNanoseconds": 26339.442108154297,
            "standardDeviationNanoseconds": 36.71349609347647,
            "operationsPerSecond": 38081.49529129626
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
            "sampleCount": 13,
            "meanNanoseconds": 13789.206899789664,
            "medianNanoseconds": 13790.417694091797,
            "minNanoseconds": 13766.245086669922,
            "maxNanoseconds": 13818.791473388672,
            "standardDeviationNanoseconds": 14.75164641325201,
            "operationsPerSecond": 72520.48702055907
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
            "meanNanoseconds": 11420.916916910808,
            "medianNanoseconds": 11426.619323730469,
            "minNanoseconds": 11364.767471313477,
            "maxNanoseconds": 11481.435668945312,
            "standardDeviationNanoseconds": 29.57434386521991,
            "operationsPerSecond": 87558.6441329691
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
            "meanNanoseconds": 15332.511130777995,
            "medianNanoseconds": 15329.477722167969,
            "minNanoseconds": 15284.213806152344,
            "maxNanoseconds": 15410.579467773438,
            "standardDeviationNanoseconds": 34.45150020062657,
            "operationsPerSecond": 65220.888572689946
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
            "meanNanoseconds": 39398.93123372396,
            "medianNanoseconds": 39392.07238769531,
            "minNanoseconds": 39280.286376953125,
            "maxNanoseconds": 39549.100158691406,
            "standardDeviationNanoseconds": 75.40275070132286,
            "operationsPerSecond": 25381.399156940548
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
            "meanNanoseconds": 25399.159849039712,
            "medianNanoseconds": 25402.506134033203,
            "minNanoseconds": 25350.274047851562,
            "maxNanoseconds": 25498.11962890625,
            "standardDeviationNanoseconds": 40.27233029791831,
            "operationsPerSecond": 39371.381019825654
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
            "meanNanoseconds": 12856.095587158203,
            "medianNanoseconds": 12855.24479675293,
            "minNanoseconds": 12803.984237670898,
            "maxNanoseconds": 12903.683135986328,
            "standardDeviationNanoseconds": 29.722433580620308,
            "operationsPerSecond": 77784.11363080466
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
            "meanNanoseconds": 22708.827270507812,
            "medianNanoseconds": 22944.973236083984,
            "minNanoseconds": 22025.75360107422,
            "maxNanoseconds": 23143.351104736328,
            "standardDeviationNanoseconds": 398.23230504546706,
            "operationsPerSecond": 44035.73941040585
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
            "meanNanoseconds": 31413.10256754557,
            "medianNanoseconds": 31417.75469970703,
            "minNanoseconds": 31323.179321289062,
            "maxNanoseconds": 31477.25164794922,
            "standardDeviationNanoseconds": 51.53298904474375,
            "operationsPerSecond": 31833.850153761934
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
            "meanNanoseconds": 49330.96542154948,
            "medianNanoseconds": 49365.831298828125,
            "minNanoseconds": 48987.77575683594,
            "maxNanoseconds": 49496.321716308594,
            "standardDeviationNanoseconds": 161.33949098066336,
            "operationsPerSecond": 20271.243253698118
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
            "meanNanoseconds": 37069.383296712236,
            "medianNanoseconds": 37050.14373779297,
            "minNanoseconds": 36942.47863769531,
            "maxNanoseconds": 37226.06640625,
            "standardDeviationNanoseconds": 75.44206845909265,
            "operationsPerSecond": 26976.440152666153
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
            "meanNanoseconds": 26853.836663382393,
            "medianNanoseconds": 26853.20295715332,
            "minNanoseconds": 26812.242065429688,
            "maxNanoseconds": 26914.189544677734,
            "standardDeviationNanoseconds": 33.633473115739946,
            "operationsPerSecond": 37238.626738338266
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexingBenchmarks-20260504-084817",
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
            "meanNanoseconds": 9682349340.857143,
            "medianNanoseconds": 9687387513.5,
            "minNanoseconds": 9630540310,
            "maxNanoseconds": 9721240263,
            "standardDeviationNanoseconds": 25307864.310894515,
            "operationsPerSecond": 0.1032807188416808
          },
          "gc": {
            "bytesAllocatedPerOperation": 982408072,
            "gen0Collections": 154,
            "gen1Collections": 76,
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
            "meanNanoseconds": 7092540229.857142,
            "medianNanoseconds": 7095067062.5,
            "minNanoseconds": 7070382000,
            "maxNanoseconds": 7108439399,
            "standardDeviationNanoseconds": 11906470.698737951,
            "operationsPerSecond": 0.14099320801739632
          },
          "gc": {
            "bytesAllocatedPerOperation": 2019242488,
            "gen0Collections": 330,
            "gen1Collections": 33,
            "gen2Collections": 1
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortIndexBenchmarks-20260504-095115",
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
            "meanNanoseconds": 10972221659.933332,
            "medianNanoseconds": 10978628084,
            "minNanoseconds": 10860023385,
            "maxNanoseconds": 11063277516,
            "standardDeviationNanoseconds": 58546247.651596844,
            "operationsPerSecond": 0.09113924517690394
          },
          "gc": {
            "bytesAllocatedPerOperation": 1025724744,
            "gen0Collections": 159,
            "gen1Collections": 72,
            "gen2Collections": 5
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
            "meanNanoseconds": 10263957367.066668,
            "medianNanoseconds": 10247167830,
            "minNanoseconds": 10168289943,
            "maxNanoseconds": 10352209378,
            "standardDeviationNanoseconds": 61614459.91956963,
            "operationsPerSecond": 0.097428308033375
          },
          "gc": {
            "bytesAllocatedPerOperation": 1014818928,
            "gen0Collections": 160,
            "gen1Collections": 77,
            "gen2Collections": 8
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortSearchBenchmarks-20260504-100130",
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
            "meanNanoseconds": 249663.99075520833,
            "medianNanoseconds": 249629.23095703125,
            "minNanoseconds": 248659.9482421875,
            "maxNanoseconds": 250454.42822265625,
            "standardDeviationNanoseconds": 456.7779565931826,
            "operationsPerSecond": 4005.3833833830063
          },
          "gc": {
            "bytesAllocatedPerOperation": 120488,
            "gen0Collections": 58,
            "gen1Collections": 2,
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
            "meanNanoseconds": 240449.0866048177,
            "medianNanoseconds": 240339.9931640625,
            "minNanoseconds": 239845.33276367188,
            "maxNanoseconds": 241351.12109375,
            "standardDeviationNanoseconds": 396.74529687680285,
            "operationsPerSecond": 4158.884586006007
          },
          "gc": {
            "bytesAllocatedPerOperation": 120488,
            "gen0Collections": 117,
            "gen1Collections": 2,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "phrase",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PhraseQueryBenchmarks-20260504-090611",
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
            "sampleCount": 14,
            "meanNanoseconds": 448343.5174734933,
            "medianNanoseconds": 448786.46875,
            "minNanoseconds": 442336.60888671875,
            "maxNanoseconds": 455000.5966796875,
            "standardDeviationNanoseconds": 3938.623683504685,
            "operationsPerSecond": 2230.4326058625825
          },
          "gc": {
            "bytesAllocatedPerOperation": 61202,
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
            "meanNanoseconds": 339656.2081380208,
            "medianNanoseconds": 340183.29931640625,
            "minNanoseconds": 332624.45263671875,
            "maxNanoseconds": 345152.36767578125,
            "standardDeviationNanoseconds": 3935.6019053457885,
            "operationsPerSecond": 2944.153458822238
          },
          "gc": {
            "bytesAllocatedPerOperation": 43945,
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
            "sampleCount": 14,
            "meanNanoseconds": 995349.0881696428,
            "medianNanoseconds": 995101.5634765625,
            "minNanoseconds": 985608.9453125,
            "maxNanoseconds": 1006063.880859375,
            "standardDeviationNanoseconds": 6000.9370492428325,
            "operationsPerSecond": 1004.6726438850814
          },
          "gc": {
            "bytesAllocatedPerOperation": 49856,
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
            "meanNanoseconds": 346309.9668317522,
            "medianNanoseconds": 346312.3125,
            "minNanoseconds": 345438.10498046875,
            "maxNanoseconds": 347166.630859375,
            "standardDeviationNanoseconds": 495.67876277466513,
            "operationsPerSecond": 2887.586543201715
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
            "meanNanoseconds": 405625.75634765625,
            "medianNanoseconds": 404834.7958984375,
            "minNanoseconds": 403780.41015625,
            "maxNanoseconds": 409249.37890625,
            "standardDeviationNanoseconds": 1769.3488009812697,
            "operationsPerSecond": 2465.3266819252813
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
            "meanNanoseconds": 1038937.9166666666,
            "medianNanoseconds": 1039427.87890625,
            "minNanoseconds": 1033875.787109375,
            "maxNanoseconds": 1042172.25,
            "standardDeviationNanoseconds": 2555.2276011455583,
            "operationsPerSecond": 962.5214211147522
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PrefixQueryBenchmarks-20260504-091123",
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
            "sampleCount": 14,
            "meanNanoseconds": 151353.95734514509,
            "medianNanoseconds": 151402.01135253906,
            "minNanoseconds": 148481.56518554688,
            "maxNanoseconds": 153507.01123046875,
            "standardDeviationNanoseconds": 1326.4725016251325,
            "operationsPerSecond": 6607.029096171013
          },
          "gc": {
            "bytesAllocatedPerOperation": 24239,
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
            "sampleCount": 13,
            "meanNanoseconds": 242803.90795898438,
            "medianNanoseconds": 242635.24047851562,
            "minNanoseconds": 241444.3955078125,
            "maxNanoseconds": 245896.55493164062,
            "standardDeviationNanoseconds": 1222.8283950592577,
            "operationsPerSecond": 4118.5498553380985
          },
          "gc": {
            "bytesAllocatedPerOperation": 35338,
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
            "sampleCount": 15,
            "meanNanoseconds": 290984.72900390625,
            "medianNanoseconds": 289572.95947265625,
            "minNanoseconds": 286827.02734375,
            "maxNanoseconds": 296498.30078125,
            "standardDeviationNanoseconds": 2769.753220971904,
            "operationsPerSecond": 3436.6064618689174
          },
          "gc": {
            "bytesAllocatedPerOperation": 64507,
            "gen0Collections": 32,
            "gen1Collections": 0,
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
            "meanNanoseconds": 185219.3930175781,
            "medianNanoseconds": 185238.2548828125,
            "minNanoseconds": 184778.12280273438,
            "maxNanoseconds": 185613.53784179688,
            "standardDeviationNanoseconds": 278.41951063426495,
            "operationsPerSecond": 5399.002683833954
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
            "sampleCount": 13,
            "meanNanoseconds": 283386.3494591346,
            "medianNanoseconds": 283355.240234375,
            "minNanoseconds": 282817.7294921875,
            "maxNanoseconds": 284304.7001953125,
            "standardDeviationNanoseconds": 426.1685849585962,
            "operationsPerSecond": 3528.7514797680956
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
            "meanNanoseconds": 357603.83203125,
            "medianNanoseconds": 357401.5537109375,
            "minNanoseconds": 356801.2783203125,
            "maxNanoseconds": 359165.7578125,
            "standardDeviationNanoseconds": 767.5290254086291,
            "operationsPerSecond": 2796.39061561458
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.TermQueryBenchmarks-20260504-084302",
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
            "sampleCount": 14,
            "meanNanoseconds": 105938.26934814453,
            "medianNanoseconds": 105957.73852539062,
            "minNanoseconds": 105666.17822265625,
            "maxNanoseconds": 106155.03674316406,
            "standardDeviationNanoseconds": 142.8304819893959,
            "operationsPerSecond": 9439.459471569277
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
            "meanNanoseconds": 149026.36713867186,
            "medianNanoseconds": 149068.298828125,
            "minNanoseconds": 148780.16748046875,
            "maxNanoseconds": 149306.59936523438,
            "standardDeviationNanoseconds": 151.23759883812954,
            "operationsPerSecond": 6710.221950653075
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
            "sampleCount": 14,
            "meanNanoseconds": 682681.812360491,
            "medianNanoseconds": 682881.5581054688,
            "minNanoseconds": 680050.4111328125,
            "maxNanoseconds": 684762.880859375,
            "standardDeviationNanoseconds": 1127.8526735427763,
            "operationsPerSecond": 1464.8112515878022
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
            "meanNanoseconds": 135451.73666992187,
            "medianNanoseconds": 135378.52880859375,
            "minNanoseconds": 134940.5546875,
            "maxNanoseconds": 136330.72509765625,
            "standardDeviationNanoseconds": 389.8581016799518,
            "operationsPerSecond": 7382.703423263365
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
            "sampleCount": 12,
            "meanNanoseconds": 177822.84381103516,
            "medianNanoseconds": 177868.4697265625,
            "minNanoseconds": 177378.75146484375,
            "maxNanoseconds": 178034.55322265625,
            "standardDeviationNanoseconds": 207.37738562717158,
            "operationsPerSecond": 5623.574443914854
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
            "sampleCount": 13,
            "meanNanoseconds": 755156.026968149,
            "medianNanoseconds": 755064.03125,
            "minNanoseconds": 753576.90625,
            "maxNanoseconds": 756773.72265625,
            "standardDeviationNanoseconds": 857.6392166781862,
            "operationsPerSecond": 1324.229648295157
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SchemaAndJsonBenchmarks-20260504-094116",
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
            "meanNanoseconds": 9932641367.533333,
            "medianNanoseconds": 9933789531,
            "minNanoseconds": 9878422969,
            "maxNanoseconds": 10005743038,
            "standardDeviationNanoseconds": 33027579.12718017,
            "operationsPerSecond": 0.10067815427915118
          },
          "gc": {
            "bytesAllocatedPerOperation": 982380480,
            "gen0Collections": 150,
            "gen1Collections": 70,
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
            "meanNanoseconds": 9920218885.533333,
            "medianNanoseconds": 9913885890,
            "minNanoseconds": 9833085804,
            "maxNanoseconds": 9995805020,
            "standardDeviationNanoseconds": 44242250.362769686,
            "operationsPerSecond": 0.10080422736017461
          },
          "gc": {
            "bytesAllocatedPerOperation": 986434344,
            "gen0Collections": 151,
            "gen1Collections": 70,
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
            "sampleCount": 14,
            "meanNanoseconds": 431168029.5714286,
            "medianNanoseconds": 430538316.5,
            "minNanoseconds": 428446780,
            "maxNanoseconds": 435221271,
            "standardDeviationNanoseconds": 2056204.3984911658,
            "operationsPerSecond": 2.3192814202712984
          },
          "gc": {
            "bytesAllocatedPerOperation": 226364856,
            "gen0Collections": 51,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "suggester",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SuggesterBenchmarks-20260504-093731",
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
            "meanNanoseconds": 4684329.892857143,
            "medianNanoseconds": 4676125.82421875,
            "minNanoseconds": 4658826.8046875,
            "maxNanoseconds": 4730367.578125,
            "standardDeviationNanoseconds": 21790.30438101867,
            "operationsPerSecond": 213.47770606951505
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
            "sampleCount": 13,
            "meanNanoseconds": 4693508.307091346,
            "medianNanoseconds": 4692399.0703125,
            "minNanoseconds": 4675139.2109375,
            "maxNanoseconds": 4738569.9453125,
            "standardDeviationNanoseconds": 16815.815981354826,
            "operationsPerSecond": 213.06023864688086
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
            "sampleCount": 15,
            "meanNanoseconds": 10308337.055208333,
            "medianNanoseconds": 10307064.359375,
            "minNanoseconds": 10271001.640625,
            "maxNanoseconds": 10345617.984375,
            "standardDeviationNanoseconds": 23679.549714712506,
            "operationsPerSecond": 97.00885745628055
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
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.WildcardQueryBenchmarks-20260504-092143",
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
            "meanNanoseconds": 149959.92024739584,
            "medianNanoseconds": 150171.54455566406,
            "minNanoseconds": 148788.54760742188,
            "maxNanoseconds": 150499.42700195312,
            "standardDeviationNanoseconds": 561.3532948185695,
            "operationsPerSecond": 6668.448465098231
          },
          "gc": {
            "bytesAllocatedPerOperation": 24959,
            "gen0Collections": 25,
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
            "meanNanoseconds": 538238.2990234375,
            "medianNanoseconds": 536953.91796875,
            "minNanoseconds": 525732.47265625,
            "maxNanoseconds": 550923.2578125,
            "standardDeviationNanoseconds": 7125.3909187773315,
            "operationsPerSecond": 1857.9131247523044
          },
          "gc": {
            "bytesAllocatedPerOperation": 10416,
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
            "sampleCount": 14,
            "meanNanoseconds": 107301.44583565848,
            "medianNanoseconds": 107197.96380615234,
            "minNanoseconds": 105950.88513183594,
            "maxNanoseconds": 108872.89233398438,
            "standardDeviationNanoseconds": 879.7169705500087,
            "operationsPerSecond": 9319.538914057013
          },
          "gc": {
            "bytesAllocatedPerOperation": 8831,
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
            "meanNanoseconds": 205533.04042271205,
            "medianNanoseconds": 205581.7752685547,
            "minNanoseconds": 204913.212890625,
            "maxNanoseconds": 206076.01098632812,
            "standardDeviationNanoseconds": 309.30963123570086,
            "operationsPerSecond": 4865.397786863551
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
            "meanNanoseconds": 1172911.5533854167,
            "medianNanoseconds": 1172360.486328125,
            "minNanoseconds": 1168497.22265625,
            "maxNanoseconds": 1177919.53515625,
            "standardDeviationNanoseconds": 2643.6661124631705,
            "operationsPerSecond": 852.5792052381648
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
            "sampleCount": 15,
            "meanNanoseconds": 411200.0958984375,
            "medianNanoseconds": 411147.36279296875,
            "minNanoseconds": 409639.125,
            "maxNanoseconds": 413475.11865234375,
            "standardDeviationNanoseconds": 1067.546661887899,
            "operationsPerSecond": 2431.906047626483
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

