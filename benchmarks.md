# Benchmarks (06-03-2026 11:56)

Benchmarked at 1,000,000 documents (full benchmarks (100, 1,000, 10,000, 100,000, ...) takes about 12 hours.

```
Dell R210 II, 26 GB
BenchmarkDotNet v0.16.0-nightly.20260220.441, Linux Debian GNU/Linux 13 (trixie)
Intel Xeon CPU E3-1220 V2 3.10GHz (Max: 3.29GHz), 1 CPU, 4 logical and 4 physical cores
.NET SDK 11.0.100-preview.1.26104.118
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v2
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v2
```

## Analysis

| Method             | DocumentCount | Mean    | Error    | StdDev   | Ratio | Gen0        | Allocated | Alloc Ratio |
|------------------- |-------------- |--------:|---------:|---------:|------:|------------:|----------:|------------:|
| LeanLucene_Analyse | 1000000       | 1.314 s | 0.0031 s | 0.0027 s |  1.00 |   9000.0000 |  37.26 MB |        1.00 |
| LuceneNet_Analyse  | 1000000       | 2.338 s | 0.0060 s | 0.0056 s |  1.78 | 164000.0000 | 656.95 MB |       17.63 |

## Block Join

| Method                           | BlockCount | Mean         | Error     | StdDev    | Ratio | Gen0         | Gen1        | Allocated      | Alloc Ratio |
|--------------------------------- |----------- |-------------:|----------:|----------:|------:|-------------:|------------:|---------------:|------------:|
| LeanLucene_IndexBlocks           | 1000000    | 14,984.55 ms | 37.437 ms | 35.019 ms | 1.000 |  374000.0000 | 295000.0000 |  2206086.13 KB |       1.000 |
| LuceneNet_IndexBlocks            | 1000000    | 32,080.65 ms | 65.729 ms | 61.483 ms | 2.141 | 3075000.0000 |  44000.0000 | 13448835.61 KB |       6.096 |
| LeanLucene_BlockJoinQuery        | 1000000    |     41.38 ms |  0.331 ms |  0.310 ms | 0.003 |      76.9231 |           - |      531.91 KB |       0.000 |
| LuceneNet_ToParentBlockJoinQuery | 1000000    |     63.84 ms |  0.321 ms |  0.300 ms | 0.004 |            - |           - |       89.84 KB |       0.000 |

## Boolean

| Method                  | BooleanType | DocumentCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Allocated  | Alloc Ratio |
|------------------------ |------------ |-------------- |----------:|----------:|----------:|------:|--------:|---------:|-----------:|------------:|
| **LeanLucene_BooleanQuery** | **Must**        | **1000000**       |  **6.921 ms** | **0.0719 ms** | **0.0673 ms** |  **1.00** |    **0.00** |  **23.4375** |    **98.9 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Must        | 1000000       | 17.816 ms | 0.0821 ms | 0.0728 ms |  2.57 |    0.03 |  31.2500 |  246.19 KB |        2.49 |
|                         |             |               |           |           |           |       |         |          |            |             |
| **LeanLucene_BooleanQuery** | **MustNot**     | **1000000**       | **10.572 ms** | **0.1237 ms** | **0.1033 ms** |  **1.00** |    **0.00** |  **15.6250** |   **99.69 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | MustNot     | 1000000       | 43.121 ms | 0.4524 ms | 0.4232 ms |  4.08 |    0.05 |        - |  239.67 KB |        2.40 |
|                         |             |               |           |           |           |       |         |          |            |             |
| **LeanLucene_BooleanQuery** | **Should**      | **1000000**       |  **6.878 ms** | **0.1169 ms** | **0.1093 ms** |  **1.00** |    **0.00** |  **23.4375** |   **98.85 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Should      | 1000000       | 18.601 ms | 0.0745 ms | 0.0660 ms |  2.71 |    0.04 | 281.2500 | 1219.93 KB |       12.34 |

## Compound Index

| Method                      | DocumentCount | Mean    | Error   | StdDev  | Ratio | Gen0         | Gen1        | Gen2      | Allocated | Alloc Ratio |
|---------------------------- |-------------- |--------:|--------:|--------:|------:|-------------:|------------:|----------:|----------:|------------:|
| LeanLucene_Index_NoCompound | 1000000       | 13.95 s | 0.082 s | 0.077 s |  1.00 |  298000.0000 | 157000.0000 | 2000.0000 |   1.98 GB |        1.00 |
| LeanLucene_Index_Compound   | 1000000       | 14.40 s | 0.055 s | 0.052 s |  1.03 |  300000.0000 | 156000.0000 | 3000.0000 |   2.15 GB |        1.09 |
| LuceneNet_Index_Compound    | 1000000       | 13.17 s | 0.028 s | 0.026 s |  0.94 | 1067000.0000 |  42000.0000 | 1000.0000 |   5.28 GB |        2.67 |

## Compound Search

| Method                       | DocumentCount | Mean     | Error     | StdDev    | Ratio | Gen0    | Allocated | Alloc Ratio |
|----------------------------- |-------------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
| LeanLucene_Search_NoCompound | 1000000       | 6.423 ms | 0.0259 ms | 0.0242 ms |  1.00 |  7.8125 |  37.63 KB |        1.00 |
| LeanLucene_Search_Compound   | 1000000       | 6.415 ms | 0.0166 ms | 0.0156 ms |  1.00 |  7.8125 |  37.63 KB |        1.00 |
| LuceneNet_Search_Compound    | 1000000       | 5.880 ms | 0.0295 ms | 0.0261 ms |  0.92 | 23.4375 |  103.5 KB |        2.75 |

## Deletion

| Method                     | DocumentCount | Mean    | Error   | StdDev  | Ratio | Gen0         | Gen1        | Gen2      | Allocated | Alloc Ratio |
|--------------------------- |-------------- |--------:|--------:|--------:|------:|-------------:|------------:|----------:|----------:|------------:|
| LeanLucene_DeleteDocuments | 1000000       | 15.11 s | 0.257 s | 0.241 s |  1.00 |  338000.0000 | 167000.0000 | 2000.0000 |   2.33 GB |        1.00 |
| LuceneNet_DeleteDocuments  | 1000000       | 13.28 s | 0.024 s | 0.021 s |  0.88 | 1135000.0000 |  48000.0000 | 1000.0000 |   5.61 GB |        2.41 |

## Diagnostics

| Method                         | DocumentCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |-------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| LeanLucene_Search_NoHooks      | 1000000       | 6.422 ms | 0.0257 ms | 0.0227 ms |  1.00 | 7.8125 |  37.63 KB |        1.00 |
| LeanLucene_Search_SlowQueryLog | 1000000       | 6.401 ms | 0.0275 ms | 0.0257 ms |  1.00 | 7.8125 |  37.63 KB |        1.00 |
| LeanLucene_Search_Analytics    | 1000000       | 6.414 ms | 0.0284 ms | 0.0266 ms |  1.00 | 7.8125 |  37.68 KB |        1.00 |
| LeanLucene_Search_AllHooks     | 1000000       | 6.427 ms | 0.0150 ms | 0.0141 ms |  1.00 | 7.8125 |  37.65 KB |        1.00 |

## Fuzzy

| Method                | QueryTerm | DocumentCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Allocated  | Alloc Ratio |
|---------------------- |---------- |-------------- |----------:|----------:|----------:|------:|--------:|---------:|---------:|-----------:|------------:|
| **LeanLucene_FuzzyQuery** | **banchmark** | **1000000**       |  **6.966 ms** | **0.1066 ms** | **0.0997 ms** |  **1.00** |    **0.00** |  **39.0625** |        **-** |  **179.49 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | banchmark | 1000000       | 19.064 ms | 0.0937 ms | 0.0876 ms |  2.74 |    0.04 | 312.5000 | 125.0000 | 1710.87 KB |        9.53 |
|                       |           |               |           |           |           |       |         |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **serch**     | **1000000**       | **11.493 ms** | **0.1813 ms** | **0.1695 ms** |  **1.00** |    **0.00** |  **31.2500** |        **-** |  **133.37 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | serch     | 1000000       |  6.733 ms | 0.0265 ms | 0.0235 ms |  0.59 |    0.01 | 210.9375 |  62.5000 |  960.36 KB |        7.20 |
|                       |           |               |           |           |           |       |         |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **vectr**     | **1000000**       | **11.405 ms** | **0.2176 ms** | **0.2137 ms** |  **1.00** |    **0.00** |  **31.2500** |        **-** |   **133.4 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | vectr     | 1000000       |  6.724 ms | 0.0345 ms | 0.0322 ms |  0.59 |    0.01 | 203.1250 |  62.5000 |  970.51 KB |        7.28 |

## Index

| Method                    | DocumentCount | Mean    | Error   | StdDev  | Ratio | Gen0         | Gen1        | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |--------:|--------:|--------:|------:|-------------:|------------:|----------:|----------:|------------:|
| LeanLucene_IndexDocuments | 1000000       | 14.38 s | 0.025 s | 0.022 s |  1.00 |  297000.0000 | 156000.0000 | 2000.0000 |   1.98 GB |        1.00 |
| LuceneNet_IndexDocuments  | 1000000       | 12.80 s | 0.037 s | 0.034 s |  0.89 | 1072000.0000 |  44000.0000 | 1000.0000 |   5.28 GB |        2.67 |

## Index Sort - Index

| Method                    | DocumentCount | Mean    | Error   | StdDev  | Ratio | Gen0        | Gen1        | Gen2      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |--------:|--------:|--------:|------:|------------:|------------:|----------:|----------:|------------:|
| LeanLucene_Index_Unsorted | 1000000       | 12.81 s | 0.054 s | 0.045 s |  1.00 | 333000.0000 | 179000.0000 | 4000.0000 |   2.29 GB |        1.00 |
| LeanLucene_Index_Sorted   | 1000000       | 15.25 s | 0.074 s | 0.069 s |  1.19 | 341000.0000 | 175000.0000 | 5000.0000 |   2.38 GB |        1.04 |

## Index Sort - Search

| Method                                   | DocumentCount | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0       | Allocated   | Alloc Ratio |
|----------------------------------------- |-------------- |----------:|---------:|---------:|------:|--------:|-----------:|------------:|------------:|
| LeanLucene_SortedSearch_EarlyTermination | 1000000       |  18.89 ms | 0.079 ms | 0.074 ms |  1.00 |    0.00 |          - |    52.51 KB |        1.00 |
| LeanLucene_SortedSearch_PostSort         | 1000000       | 338.96 ms | 1.695 ms | 1.585 ms | 17.94 |    0.11 | 15000.0000 | 70319.02 KB |    1,339.06 |

## Phrase

| Method                 | PhraseType     | DocumentCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|----------------------- |--------------- |-------------- |---------:|---------:|---------:|------:|--------:|---------:|--------:|-----------:|------------:|
| **LeanLucene_PhraseQuery** | **ExactThreeWord** | **1000000**       | **47.05 ms** | **0.795 ms** | **0.915 ms** |  **1.00** |    **0.00** | **363.6364** |       **-** | **1626.95 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactThreeWord | 1000000       | 75.52 ms | 0.353 ms | 0.331 ms |  1.61 |    0.03 | 142.8571 |       - |  625.36 KB |        0.38 |
|                        |                |               |          |          |          |       |         |          |         |            |             |
| **LeanLucene_PhraseQuery** | **ExactTwoWord**   | **1000000**       | **13.00 ms** | **0.247 ms** | **0.231 ms** |  **1.00** |    **0.00** | **187.5000** |       **-** |  **801.06 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactTwoWord   | 1000000       | 23.18 ms | 0.069 ms | 0.064 ms |  1.78 |    0.03 | 125.0000 | 31.2500 |   519.7 KB |        0.65 |
|                        |                |               |          |          |          |       |         |          |         |            |             |
| **LeanLucene_PhraseQuery** | **SlopTwoWord**    | **1000000**       | **12.99 ms** | **0.260 ms** | **0.230 ms** |  **1.00** |    **0.00** | **187.5000** |       **-** |  **778.98 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | SlopTwoWord    | 1000000       | 26.56 ms | 0.100 ms | 0.094 ms |  2.05 |    0.04 |  62.5000 |       - |  294.25 KB |        0.38 |

## Prefix

| Method                 | PhraseType     | DocumentCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|----------------------- |--------------- |-------------- |---------:|---------:|---------:|------:|--------:|---------:|--------:|-----------:|------------:|
| **LeanLucene_PhraseQuery** | **ExactThreeWord** | **1000000**       | **47.05 ms** | **0.795 ms** | **0.915 ms** |  **1.00** |    **0.00** | **363.6364** |       **-** | **1626.95 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactThreeWord | 1000000       | 75.52 ms | 0.353 ms | 0.331 ms |  1.61 |    0.03 | 142.8571 |       - |  625.36 KB |        0.38 |
|                        |                |               |          |          |          |       |         |          |         |            |             |
| **LeanLucene_PhraseQuery** | **ExactTwoWord**   | **1000000**       | **13.00 ms** | **0.247 ms** | **0.231 ms** |  **1.00** |    **0.00** | **187.5000** |       **-** |  **801.06 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactTwoWord   | 1000000       | 23.18 ms | 0.069 ms | 0.064 ms |  1.78 |    0.03 | 125.0000 | 31.2500 |   519.7 KB |        0.65 |
|                        |                |               |          |          |          |       |         |          |         |            |             |
| **LeanLucene_PhraseQuery** | **SlopTwoWord**    | **1000000**       | **12.99 ms** | **0.260 ms** | **0.230 ms** |  **1.00** |    **0.00** | **187.5000** |       **-** |  **778.98 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | SlopTwoWord    | 1000000       | 26.56 ms | 0.100 ms | 0.094 ms |  2.05 |    0.04 |  62.5000 |       - |  294.25 KB |        0.38 |

## Query

| Method                 | PhraseType     | DocumentCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|----------------------- |--------------- |-------------- |---------:|---------:|---------:|------:|--------:|---------:|--------:|-----------:|------------:|
| **LeanLucene_PhraseQuery** | **ExactThreeWord** | **1000000**       | **47.05 ms** | **0.795 ms** | **0.915 ms** |  **1.00** |    **0.00** | **363.6364** |       **-** | **1626.95 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactThreeWord | 1000000       | 75.52 ms | 0.353 ms | 0.331 ms |  1.61 |    0.03 | 142.8571 |       - |  625.36 KB |        0.38 |
|                        |                |               |          |          |          |       |         |          |         |            |             |
| **LeanLucene_PhraseQuery** | **ExactTwoWord**   | **1000000**       | **13.00 ms** | **0.247 ms** | **0.231 ms** |  **1.00** |    **0.00** | **187.5000** |       **-** |  **801.06 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactTwoWord   | 1000000       | 23.18 ms | 0.069 ms | 0.064 ms |  1.78 |    0.03 | 125.0000 | 31.2500 |   519.7 KB |        0.65 |
|                        |                |               |          |          |          |       |         |          |         |            |             |
| **LeanLucene_PhraseQuery** | **SlopTwoWord**    | **1000000**       | **12.99 ms** | **0.260 ms** | **0.230 ms** |  **1.00** |    **0.00** | **187.5000** |       **-** |  **778.98 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | SlopTwoWord    | 1000000       | 26.56 ms | 0.100 ms | 0.094 ms |  2.05 |    0.04 |  62.5000 |       - |  294.25 KB |        0.38 |

## JSON Schema

| Method                      | DocumentCount | Mean     | Error    | StdDev   | Ratio | Gen0        | Gen1        | Allocated  | Alloc Ratio |
|---------------------------- |-------------- |---------:|---------:|---------:|------:|------------:|------------:|-----------:|------------:|
| LeanLucene_Index_NoSchema   | 1000000       | 13.727 s | 0.0689 s | 0.0644 s |  1.00 | 295000.0000 | 153000.0000 | 2022.63 MB |        1.00 |
| LeanLucene_Index_WithSchema | 1000000       | 13.937 s | 0.0314 s | 0.0294 s |  1.02 | 302000.0000 | 156000.0000 | 2060.78 MB |        1.02 |
| LeanLucene_JsonMapping      | 1000000       |  1.524 s | 0.0051 s | 0.0045 s |  0.11 | 232000.0000 |           - |  928.58 MB |        0.46 |

## Small Index

| Method                  | DocumentCount | Mean    | Error   | StdDev  | Gen0        | Gen1       | Allocated |
|------------------------ |-------------- |--------:|--------:|--------:|------------:|-----------:|----------:|
| IndexAndQuery_Roundtrip | 1000000       | 15.52 s | 0.046 s | 0.041 s | 606000.0000 | 35000.0000 |   2.49 GB |

## Suggestion

| Method                 | DocumentCount | Mean       | Error     | StdDev    | Ratio | Gen0      | Gen1   | Allocated  | Alloc Ratio |
|----------------------- |-------------- |-----------:|----------:|----------:|------:|----------:|-------:|-----------:|------------:|
| LeanLucene_DidYouMean  | 1000000       | 604.824 ms | 2.7521 ms | 2.5744 ms | 1.000 |         - |      - | 2543.91 KB |       1.000 |
| LeanLucene_SpellIndex  | 1000000       |  41.032 ms | 0.1241 ms | 0.1160 ms | 0.068 |         - |      - |    5.78 KB |       0.002 |
| LuceneNet_SpellChecker | 1000000       |   2.948 ms | 0.0569 ms | 0.0558 ms | 0.005 | 1199.2188 | 3.9063 | 4906.52 KB |       1.929 |

## Token Budget

| Method                               | DocumentCount | Mean    | Error   | StdDev  | Ratio | Gen0        | Gen1        | Gen2      | Allocated | Alloc Ratio |
|------------------------------------- |-------------- |--------:|--------:|--------:|------:|------------:|------------:|----------:|----------:|------------:|
| LeanLucene_Index_NoBudget            | 1000000       | 14.31 s | 0.030 s | 0.028 s |  1.00 | 298000.0000 | 158000.0000 | 2000.0000 |   1.98 GB |        1.00 |
| LeanLucene_Index_WithBudget_Truncate | 1000000       | 14.00 s | 0.137 s | 0.128 s |  0.98 | 297000.0000 | 155000.0000 | 2000.0000 |   1.98 GB |        1.00 |
| LeanLucene_Index_WithBudget_Reject   | 1000000       | 14.00 s | 0.054 s | 0.051 s |  0.98 | 298000.0000 | 158000.0000 | 2000.0000 |   1.98 GB |        1.00 |

## Wildcard

| Method                               | DocumentCount | Mean    | Error   | StdDev  | Ratio | Gen0        | Gen1        | Gen2      | Allocated | Alloc Ratio |
|------------------------------------- |-------------- |--------:|--------:|--------:|------:|------------:|------------:|----------:|----------:|------------:|
| LeanLucene_Index_NoBudget            | 1000000       | 14.31 s | 0.030 s | 0.028 s |  1.00 | 298000.0000 | 158000.0000 | 2000.0000 |   1.98 GB |        1.00 |
| LeanLucene_Index_WithBudget_Truncate | 1000000       | 14.00 s | 0.137 s | 0.128 s |  0.98 | 297000.0000 | 155000.0000 | 2000.0000 |   1.98 GB |        1.00 |
| LeanLucene_Index_WithBudget_Reject   | 1000000       | 14.00 s | 0.054 s | 0.051 s |  0.98 | 298000.0000 | 158000.0000 | 2000.0000 |   1.98 GB |        1.00 |


