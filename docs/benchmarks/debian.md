---
title: Benchmarks - debian
---

# Benchmarks: debian

**.NET** 10.0.3 &nbsp;&middot;&nbsp; **Commit** `ffa3bb8` &nbsp;&middot;&nbsp; 28 April 2026 13:20 UTC &nbsp;&middot;&nbsp; 62 benchmarks

## Analysis

| Method             | DocumentCount | Mean     | Error    | StdDev   | Ratio | Gen0      | Allocated   | Alloc Ratio |
|------------------- |-------------- |---------:|---------:|---------:|------:|----------:|------------:|------------:|
| LeanLucene_Analyse | 10000         | 34.31 ms | 0.133 ms | 0.125 ms |  1.00 |  133.3333 |   683.06 KB |        1.00 |
| LuceneNet_Analyse  | 10000         | 54.44 ms | 0.390 ms | 0.365 ms |  1.59 | 3500.0000 | 14624.06 KB |       21.41 |

## Block-Join

| Method                           | BlockCount | Mean          | Error        | StdDev       | Ratio | Gen0       | Gen1      | Allocated   | Alloc Ratio |
|--------------------------------- |----------- |--------------:|-------------:|-------------:|------:|-----------:|----------:|------------:|------------:|
| LeanLucene_IndexBlocks           | 10000      | 676,645.32 μs | 3,800.322 μs | 3,368.887 μs | 1.000 | 10000.0000 | 5000.0000 |  72884832 B |       1.000 |
| LeanLucene_BlockJoinQuery        | 10000      |      45.09 μs |     0.200 μs |     0.178 μs | 0.000 |     0.1831 |         - |       840 B |       0.000 |
| LuceneNet_IndexBlocks            | 10000      | 640,937.94 μs | 3,537.617 μs | 3,136.006 μs | 0.947 | 40000.0000 | 3000.0000 | 225538992 B |       3.094 |
| LuceneNet_ToParentBlockJoinQuery | 10000      |      89.04 μs |     0.434 μs |     0.406 μs | 0.000 |     3.1738 |         - |     13312 B |       0.000 |

## Boolean queries

| Method                  | BooleanType | DocumentCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------ |-------------- |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **LeanLucene_BooleanQuery** | **Must**        | **10000**         |  **2.128 μs** | **0.0105 μs** | **0.0099 μs** |  **1.00** |    **0.00** |  **0.4044** |      **-** |   **1.66 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Must        | 10000         | 13.664 μs | 0.0896 μs | 0.0838 μs |  6.42 |    0.05 |  6.7139 |      - |  27.45 KB |       16.58 |
|                         |             |               |           |           |           |       |         |         |        |           |             |
| **LeanLucene_BooleanQuery** | **MustNot**     | **10000**         |  **1.449 μs** | **0.0068 μs** | **0.0060 μs** |  **1.00** |    **0.00** |  **0.4177** |      **-** |   **1.71 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | MustNot     | 10000         | 10.319 μs | 0.0591 μs | 0.0553 μs |  7.12 |    0.05 |  5.0507 |      - |  20.65 KB |       12.07 |
|                         |             |               |           |           |           |       |         |         |        |           |             |
| **LeanLucene_BooleanQuery** | **Should**      | **10000**         | **33.883 μs** | **0.1208 μs** | **0.1130 μs** |  **1.00** |    **0.00** |  **0.4272** |      **-** |   **1.88 KB** |        **1.00** |
| LuceneNet_BooleanQuery  | Should      | 10000         | 72.573 μs | 0.4325 μs | 0.4046 μs |  2.14 |    0.01 | 32.4707 | 7.3242 | 132.98 KB |       70.92 |

## Compound file (index)

| Method                      | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0       | Gen1      | Allocated | Alloc Ratio |
|---------------------------- |-------------- |---------:|--------:|--------:|------:|-----------:|----------:|----------:|------------:|
| LeanLucene_Index_NoCompound | 10000         | 222.2 ms | 1.44 ms | 1.35 ms |  1.00 |  3333.3333 | 1666.6667 |  27.15 MB |        1.00 |
| LeanLucene_Index_Compound   | 10000         | 221.4 ms | 1.15 ms | 1.07 ms |  1.00 |  3333.3333 | 1666.6667 |  30.28 MB |        1.12 |
| LuceneNet_Index_Compound    | 10000         | 185.7 ms | 1.17 ms | 1.09 ms |  0.84 | 11333.3333 | 1333.3333 |  66.03 MB |        2.43 |

## Compound file (search)

| Method                       | DocumentCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |-------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| LeanLucene_Search_NoCompound | 10000         | 1.219 μs | 0.0061 μs | 0.0057 μs |  1.00 |    0.00 | 0.1068 |     448 B |        1.00 |
| LeanLucene_Search_Compound   | 10000         | 1.220 μs | 0.0053 μs | 0.0049 μs |  1.00 |    0.01 | 0.1068 |     448 B |        1.00 |
| LuceneNet_Search_Compound    | 10000         | 7.712 μs | 0.0389 μs | 0.0364 μs |  6.33 |    0.04 | 3.0518 |   12824 B |       28.62 |

## Deletion

| Method                     | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0       | Gen1      | Allocated | Alloc Ratio |
|--------------------------- |-------------- |---------:|--------:|--------:|------:|-----------:|----------:|----------:|------------:|
| LeanLucene_DeleteDocuments | 10000         | 222.8 ms | 1.14 ms | 0.95 ms |  1.00 |  3333.3333 | 1666.6667 |  28.78 MB |        1.00 |
| LuceneNet_DeleteDocuments  | 10000         | 192.7 ms | 1.14 ms | 1.06 ms |  0.87 | 12000.0000 | 1333.3333 |  69.44 MB |        2.41 |

## Fuzzy queries

| Method                | QueryTerm | DocumentCount | Mean       | Error   | StdDev  | Ratio | RatioSD | Gen0     | Gen1     | Allocated  | Alloc Ratio |
|---------------------- |---------- |-------------- |-----------:|--------:|--------:|------:|--------:|---------:|---------:|-----------:|------------:|
| **LeanLucene_FuzzyQuery** | **goverment** | **10000**         |   **348.0 μs** | **1.91 μs** | **1.79 μs** |  **1.00** |    **0.00** |        **-** |        **-** |    **1.88 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | goverment | 10000         | 1,702.3 μs | 7.94 μs | 7.04 μs |  4.89 |    0.03 | 365.2344 | 148.4375 | 1776.82 KB |      943.70 |
|                       |           |               |            |         |         |       |         |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **markts**    | **10000**         |   **432.1 μs** | **1.63 μs** | **1.53 μs** |  **1.00** |    **0.00** |   **0.4883** |        **-** |    **2.15 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | markts    | 10000         | 1,117.6 μs | 5.58 μs | 5.22 μs |  2.59 |    0.01 | 263.6719 |  85.9375 | 1197.96 KB |      557.59 |
|                       |           |               |            |         |         |       |         |          |          |            |             |
| **LeanLucene_FuzzyQuery** | **presiden**  | **10000**         |   **444.6 μs** | **1.33 μs** | **1.24 μs** |  **1.00** |    **0.00** |        **-** |        **-** |    **1.52 KB** |        **1.00** |
| LuceneNet_FuzzyQuery  | presiden  | 10000         | 1,441.7 μs | 5.03 μs | 4.71 μs |  3.24 |    0.01 | 318.3594 | 156.2500 | 1536.34 KB |    1,013.67 |

## Indexing

| Method                    | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0       | Gen1      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |---------:|--------:|--------:|------:|-----------:|----------:|----------:|------------:|
| LeanLucene_IndexDocuments | 10000         | 220.9 ms | 1.58 ms | 1.48 ms |  1.00 |  3333.3333 | 1666.6667 |  27.15 MB |        1.00 |
| LuceneNet_IndexDocuments  | 10000         | 185.7 ms | 0.72 ms | 0.67 ms |  0.84 | 11333.3333 | 1333.3333 |  66.02 MB |        2.43 |

## Index-sort (index)

| Method                    | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0      | Gen1      | Allocated | Alloc Ratio |
|-------------------------- |-------------- |---------:|--------:|--------:|------:|----------:|----------:|----------:|------------:|
| LeanLucene_Index_Unsorted | 10000         | 231.7 ms | 1.67 ms | 1.48 ms |  1.00 | 3666.6667 | 1666.6667 |  30.59 MB |        1.00 |
| LeanLucene_Index_Sorted   | 10000         | 255.6 ms | 2.87 ms | 2.54 ms |  1.10 | 3500.0000 | 1500.0000 |  31.75 MB |        1.04 |

## Index-sort (search)

| Method                                   | DocumentCount | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------------- |-------------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| LeanLucene_SortedSearch_EarlyTermination | 10000         | 401.2 ns | 1.54 ns | 1.44 ns |  1.00 | 0.1030 |     432 B |        1.00 |
| LeanLucene_SortedSearch_PostSort         | 10000         | 291.9 ns | 1.10 ns | 1.03 ns |  0.73 | 0.0744 |     312 B |        0.72 |

## Phrase queries

| Method                 | PhraseType     | DocumentCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------- |--------------- |-------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **LeanLucene_PhraseQuery** | **ExactThreeWord** | **10000**         |  **2.142 μs** | **0.0132 μs** | **0.0117 μs** |  **1.00** |    **0.00** | **0.7744** |   **3.17 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactThreeWord | 10000         | 10.270 μs | 0.0605 μs | 0.0536 μs |  4.79 |    0.04 | 6.4240 |  26.27 KB |        8.28 |
|                        |                |               |           |           |           |       |         |        |           |             |
| **LeanLucene_PhraseQuery** | **ExactTwoWord**   | **10000**         |  **2.069 μs** | **0.0089 μs** | **0.0084 μs** |  **1.00** |    **0.00** | **0.7019** |   **2.87 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | ExactTwoWord   | 10000         |  7.407 μs | 0.0650 μs | 0.0608 μs |  3.58 |    0.03 | 4.7455 |  19.41 KB |        6.77 |
|                        |                |               |           |           |           |       |         |        |           |             |
| **LeanLucene_PhraseQuery** | **SlopTwoWord**    | **10000**         | **39.170 μs** | **0.1250 μs** | **0.1170 μs** |  **1.00** |    **0.00** | **1.0376** |   **4.47 KB** |        **1.00** |
| LuceneNet_PhraseQuery  | SlopTwoWord    | 10000         |  9.939 μs | 0.0886 μs | 0.0829 μs |  0.25 |    0.00 | 6.0883 |   24.9 KB |        5.57 |

## Prefix queries

| Method                 | QueryPrefix | DocumentCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-------------- |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **LeanLucene_PrefixQuery** | **gov**         | **10000**         |  **3.480 μs** | **0.0106 μs** | **0.0099 μs** |  **1.00** |    **0.00** |  **0.3014** |      **-** |    **1264 B** |        **1.00** |
| LuceneNet_PrefixQuery  | gov         | 10000         | 15.122 μs | 0.1077 μs | 0.1008 μs |  4.35 |    0.03 | 13.1531 | 1.4496 |   55088 B |       43.58 |
|                        |             |               |           |           |           |       |         |         |        |           |             |
| **LeanLucene_PrefixQuery** | **mark**        | **10000**         |  **1.693 μs** | **0.0068 μs** | **0.0060 μs** |  **1.00** |    **0.00** |  **0.1945** |      **-** |     **816 B** |        **1.00** |
| LuceneNet_PrefixQuery  | mark        | 10000         | 11.424 μs | 0.0865 μs | 0.0809 μs |  6.75 |    0.05 | 12.9852 | 0.0153 |   54632 B |       66.95 |
|                        |             |               |           |           |           |       |         |         |        |           |             |
| **LeanLucene_PrefixQuery** | **pres**        | **10000**         |  **9.559 μs** | **0.0368 μs** | **0.0327 μs** |  **1.00** |    **0.00** |  **0.4883** |      **-** |    **2064 B** |        **1.00** |
| LuceneNet_PrefixQuery  | pres        | 10000         | 16.485 μs | 0.0907 μs | 0.0849 μs |  1.72 |    0.01 | 12.9700 | 0.0610 |   54480 B |       26.40 |

## Term queries

| Method               | QueryTerm  | DocumentCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |-------------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **LeanLucene_TermQuery** | **government** | **10000**         |    **513.0 ns** |   **2.27 ns** |   **2.12 ns** |  **1.00** |    **0.00** | **0.0725** |     **304 B** |        **1.00** |
| LuceneNet_TermQuery  | government | 10000         |  6,700.0 ns |  44.92 ns |  42.02 ns | 13.06 |    0.09 | 3.1052 |   12992 B |       42.74 |
|                      |            |               |             |           |           |       |         |        |           |             |
| **LeanLucene_TermQuery** | **people**     | **10000**         | **22,948.5 ns** |  **86.00 ns** |  **80.44 ns** |  **1.00** |    **0.00** | **0.0916** |     **472 B** |        **1.00** |
| LuceneNet_TermQuery  | people     | 10000         | 32,601.7 ns | 140.29 ns | 131.23 ns |  1.42 |    0.01 | 3.1128 |   13192 B |       27.95 |
|                      |            |               |             |           |           |       |         |        |           |             |
| **LeanLucene_TermQuery** | **said**       | **10000**         | **40,732.2 ns** | **127.45 ns** | **119.22 ns** |  **1.00** |    **0.00** | **0.0610** |     **464 B** |        **1.00** |
| LuceneNet_TermQuery  | said       | 10000         | 51,535.0 ns | 273.80 ns | 256.11 ns |  1.27 |    0.01 | 3.0518 |   12888 B |       27.78 |

## Schema and JSON

| Method                      | DocumentCount | Mean      | Error    | StdDev   | Ratio | Gen0      | Gen1      | Allocated | Alloc Ratio |
|---------------------------- |-------------- |----------:|---------:|---------:|------:|----------:|----------:|----------:|------------:|
| LeanLucene_Index_NoSchema   | 10000         | 220.73 ms | 1.280 ms | 1.135 ms |  1.00 | 3333.3333 | 1666.6667 |  27.15 MB |        1.00 |
| LeanLucene_Index_WithSchema | 10000         | 221.83 ms | 0.972 ms | 0.909 ms |  1.01 | 3333.3333 | 1666.6667 |  27.54 MB |        1.01 |
| LeanLucene_JsonMapping      | 10000         |  16.08 ms | 0.096 ms | 0.085 ms |  0.07 | 2062.5000 |         - |    8.3 MB |        0.31 |

## Suggester

| Method                 | DocumentCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0      | Allocated  | Alloc Ratio |
|----------------------- |-------------- |-----------:|---------:|---------:|------:|--------:|----------:|-----------:|------------:|
| LeanLucene_DidYouMean  | 10000         |   325.9 μs |  0.99 μs |  0.93 μs |  1.00 |    0.00 |    1.4648 |     7.5 KB |        1.00 |
| LeanLucene_SpellIndex  | 10000         |   325.7 μs |  1.54 μs |  1.44 μs |  1.00 |    0.01 |    0.9766 |    5.78 KB |        0.77 |
| LuceneNet_SpellChecker | 10000         | 7,410.0 μs | 43.04 μs | 38.16 μs | 22.73 |    0.13 | 1226.5625 | 5030.86 KB |      670.78 |

## Wildcard queries

| Method                   | WildcardPattern | DocumentCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------- |---------------- |-------------- |-----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **LeanLucene_WildcardQuery** | **gov***            | **10000**         |   **3.760 μs** | **0.0145 μs** | **0.0136 μs** |  **1.00** |    **0.00** |  **0.3662** |      **-** |   **1.51 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | gov*            | 10000         |  28.348 μs | 0.2231 μs | 0.2087 μs |  7.54 |    0.06 | 17.7917 | 1.1597 |  72.82 KB |       48.30 |
|                          |                 |               |            |           |           |       |         |         |        |           |             |
| **LeanLucene_WildcardQuery** | **m*rket**          | **10000**         |  **31.624 μs** | **0.1562 μs** | **0.1462 μs** |  **1.00** |    **0.00** |  **5.4932** |      **-** |  **22.53 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | m*rket          | 10000         | 214.245 μs | 1.3215 μs | 1.2362 μs |  6.77 |    0.05 | 84.9609 | 1.7090 | 347.49 KB |       15.42 |
|                          |                 |               |            |           |           |       |         |         |        |           |             |
| **LeanLucene_WildcardQuery** | **pre*dent**        | **10000**         |   **2.553 μs** | **0.0122 μs** | **0.0114 μs** |  **1.00** |    **0.00** |  **0.5302** |      **-** |   **2.17 KB** |        **1.00** |
| LuceneNet_WildcardQuery  | pre*dent        | 10000         | 237.977 μs | 1.5316 μs | 1.4327 μs | 93.23 |    0.68 | 89.1113 | 0.7324 | 364.48 KB |      167.82 |

<details>
<summary>Full data (report.json)</summary>

<pre><code class="lang-json">{
  "schemaVersion": 2,
  "runId": "2026-04-28 13-20 (ffa3bb8)",
  "runType": "full",
  "generatedAtUtc": "2026-04-28T13:20:25.6357589\u002B00:00",
  "commandLineArgs": [],
  "hostMachineName": "debian",
  "commitHash": "ffa3bb8",
  "dotnetVersion": "10.0.3",
  "totalBenchmarkCount": 62,
  "suites": [
    {
      "suiteName": "analysis",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.AnalysisBenchmarks-20260428-142630",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "AnalysisBenchmarks.LeanLucene_Analyse|DocumentCount=10000",
          "displayInfo": "AnalysisBenchmarks.LeanLucene_Analyse: DefaultJob [DocumentCount=10000]",
          "typeName": "AnalysisBenchmarks",
          "methodName": "LeanLucene_Analyse",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 34305938.34222223,
            "medianNanoseconds": 34316202.666666664,
            "minNanoseconds": 34050822.93333333,
            "maxNanoseconds": 34482815.06666667,
            "standardDeviationNanoseconds": 124543.2146991591,
            "operationsPerSecond": 29.149472316553556
          },
          "gc": {
            "bytesAllocatedPerOperation": 699456,
            "gen0Collections": 2,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "AnalysisBenchmarks.LuceneNet_Analyse|DocumentCount=10000",
          "displayInfo": "AnalysisBenchmarks.LuceneNet_Analyse: DefaultJob [DocumentCount=10000]",
          "typeName": "AnalysisBenchmarks",
          "methodName": "LuceneNet_Analyse",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 54441111.54666666,
            "medianNanoseconds": 54318143.7,
            "minNanoseconds": 53938523.4,
            "maxNanoseconds": 55271416.1,
            "standardDeviationNanoseconds": 364659.53824227006,
            "operationsPerSecond": 18.368471392117055
          },
          "gc": {
            "bytesAllocatedPerOperation": 14975040,
            "gen0Collections": 35,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "blockjoin",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BlockJoinBenchmarks-20260428-150333",
      "benchmarkCount": 4,
      "benchmarks": [
        {
          "key": "BlockJoinBenchmarks.LeanLucene_BlockJoinQuery|BlockCount=10000",
          "displayInfo": "BlockJoinBenchmarks.LeanLucene_BlockJoinQuery: DefaultJob [BlockCount=10000]",
          "typeName": "BlockJoinBenchmarks",
          "methodName": "LeanLucene_BlockJoinQuery",
          "parameters": {
            "BlockCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 45090.14242989676,
            "medianNanoseconds": 45108.44241333008,
            "minNanoseconds": 44704.59637451172,
            "maxNanoseconds": 45346.697509765625,
            "standardDeviationNanoseconds": 177.6480410922087,
            "operationsPerSecond": 22177.79643421476
          },
          "gc": {
            "bytesAllocatedPerOperation": 840,
            "gen0Collections": 3,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BlockJoinBenchmarks.LeanLucene_IndexBlocks|BlockCount=10000",
          "displayInfo": "BlockJoinBenchmarks.LeanLucene_IndexBlocks: DefaultJob [BlockCount=10000]",
          "typeName": "BlockJoinBenchmarks",
          "methodName": "LeanLucene_IndexBlocks",
          "parameters": {
            "BlockCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 676645322.6428572,
            "medianNanoseconds": 676234356.5,
            "minNanoseconds": 672603459,
            "maxNanoseconds": 684290126,
            "standardDeviationNanoseconds": 3368886.803456678,
            "operationsPerSecond": 1.4778791288975095
          },
          "gc": {
            "bytesAllocatedPerOperation": 72884832,
            "gen0Collections": 10,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        },
        {
          "key": "BlockJoinBenchmarks.LuceneNet_IndexBlocks|BlockCount=10000",
          "displayInfo": "BlockJoinBenchmarks.LuceneNet_IndexBlocks: DefaultJob [BlockCount=10000]",
          "typeName": "BlockJoinBenchmarks",
          "methodName": "LuceneNet_IndexBlocks",
          "parameters": {
            "BlockCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 640937941.2142857,
            "medianNanoseconds": 641312262.5,
            "minNanoseconds": 635485381,
            "maxNanoseconds": 646864266,
            "standardDeviationNanoseconds": 3136005.688599559,
            "operationsPerSecond": 1.5602134554641205
          },
          "gc": {
            "bytesAllocatedPerOperation": 225538992,
            "gen0Collections": 40,
            "gen1Collections": 3,
            "gen2Collections": 0
          }
        },
        {
          "key": "BlockJoinBenchmarks.LuceneNet_ToParentBlockJoinQuery|BlockCount=10000",
          "displayInfo": "BlockJoinBenchmarks.LuceneNet_ToParentBlockJoinQuery: DefaultJob [BlockCount=10000]",
          "typeName": "BlockJoinBenchmarks",
          "methodName": "LuceneNet_ToParentBlockJoinQuery",
          "parameters": {
            "BlockCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 89036.28134765624,
            "medianNanoseconds": 89103.69470214844,
            "minNanoseconds": 88396.25952148438,
            "maxNanoseconds": 89778.81579589844,
            "standardDeviationNanoseconds": 405.78261557121954,
            "operationsPerSecond": 11231.37652273843
          },
          "gc": {
            "bytesAllocatedPerOperation": 13312,
            "gen0Collections": 26,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "boolean",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.BooleanQueryBenchmarks-20260428-142833",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery|BooleanType=Must, DocumentCount=10000",
          "displayInfo": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery: DefaultJob [BooleanType=Must, DocumentCount=10000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LeanLucene_BooleanQuery",
          "parameters": {
            "BooleanType": "Must",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 2128.227528889974,
            "medianNanoseconds": 2130.0249633789062,
            "minNanoseconds": 2110.5753784179688,
            "maxNanoseconds": 2141.4346313476562,
            "standardDeviationNanoseconds": 9.85627765031068,
            "operationsPerSecond": 469874.5723496834
          },
          "gc": {
            "bytesAllocatedPerOperation": 1696,
            "gen0Collections": 106,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery|BooleanType=MustNot, DocumentCount=10000",
          "displayInfo": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery: DefaultJob [BooleanType=MustNot, DocumentCount=10000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LeanLucene_BooleanQuery",
          "parameters": {
            "BooleanType": "MustNot",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 1448.776328086853,
            "medianNanoseconds": 1448.6110010147095,
            "minNanoseconds": 1437.5939598083496,
            "maxNanoseconds": 1456.790657043457,
            "standardDeviationNanoseconds": 6.029611630450978,
            "operationsPerSecond": 690237.672036322
          },
          "gc": {
            "bytesAllocatedPerOperation": 1752,
            "gen0Collections": 219,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery|BooleanType=Should, DocumentCount=10000",
          "displayInfo": "BooleanQueryBenchmarks.LeanLucene_BooleanQuery: DefaultJob [BooleanType=Should, DocumentCount=10000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LeanLucene_BooleanQuery",
          "parameters": {
            "BooleanType": "Should",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 33882.51776936849,
            "medianNanoseconds": 33925.604553222656,
            "minNanoseconds": 33660.46551513672,
            "maxNanoseconds": 33988.623596191406,
            "standardDeviationNanoseconds": 113.02702500605032,
            "operationsPerSecond": 29513.7453127539
          },
          "gc": {
            "bytesAllocatedPerOperation": 1920,
            "gen0Collections": 7,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery|BooleanType=Must, DocumentCount=10000",
          "displayInfo": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery: DefaultJob [BooleanType=Must, DocumentCount=10000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LuceneNet_BooleanQuery",
          "parameters": {
            "BooleanType": "Must",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 13664.111744181315,
            "medianNanoseconds": 13640.842559814453,
            "minNanoseconds": 13578.524658203125,
            "maxNanoseconds": 13830.677017211914,
            "standardDeviationNanoseconds": 83.7772172369851,
            "operationsPerSecond": 73184.41320752789
          },
          "gc": {
            "bytesAllocatedPerOperation": 28112,
            "gen0Collections": 440,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery|BooleanType=MustNot, DocumentCount=10000",
          "displayInfo": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery: DefaultJob [BooleanType=MustNot, DocumentCount=10000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LuceneNet_BooleanQuery",
          "parameters": {
            "BooleanType": "MustNot",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 10319.019318644207,
            "medianNanoseconds": 10313.945785522461,
            "minNanoseconds": 10215.338027954102,
            "maxNanoseconds": 10435.958389282227,
            "standardDeviationNanoseconds": 55.30040490111205,
            "operationsPerSecond": 96908.43374943771
          },
          "gc": {
            "bytesAllocatedPerOperation": 21144,
            "gen0Collections": 331,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery|BooleanType=Should, DocumentCount=10000",
          "displayInfo": "BooleanQueryBenchmarks.LuceneNet_BooleanQuery: DefaultJob [BooleanType=Should, DocumentCount=10000]",
          "typeName": "BooleanQueryBenchmarks",
          "methodName": "LuceneNet_BooleanQuery",
          "parameters": {
            "BooleanType": "Should",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 72573.39231770833,
            "medianNanoseconds": 72609.3984375,
            "minNanoseconds": 71606.04418945312,
            "maxNanoseconds": 73178.01672363281,
            "standardDeviationNanoseconds": 404.59727203002603,
            "operationsPerSecond": 13779.154702073836
          },
          "gc": {
            "bytesAllocatedPerOperation": 136168,
            "gen0Collections": 266,
            "gen1Collections": 60,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "compound-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.CompoundFileIndexBenchmarks-20260428-145542",
      "benchmarkCount": 3,
      "benchmarks": [
        {
          "key": "CompoundFileIndexBenchmarks.LeanLucene_Index_Compound|DocumentCount=10000",
          "displayInfo": "CompoundFileIndexBenchmarks.LeanLucene_Index_Compound: DefaultJob [DocumentCount=10000]",
          "typeName": "CompoundFileIndexBenchmarks",
          "methodName": "LeanLucene_Index_Compound",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 221380568.17777774,
            "medianNanoseconds": 221496963.66666666,
            "minNanoseconds": 219538922.66666666,
            "maxNanoseconds": 223363090.33333334,
            "standardDeviationNanoseconds": 1072463.6095968448,
            "operationsPerSecond": 4.517108291080718
          },
          "gc": {
            "bytesAllocatedPerOperation": 31746856,
            "gen0Collections": 10,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        },
        {
          "key": "CompoundFileIndexBenchmarks.LeanLucene_Index_NoCompound|DocumentCount=10000",
          "displayInfo": "CompoundFileIndexBenchmarks.LeanLucene_Index_NoCompound: DefaultJob [DocumentCount=10000]",
          "typeName": "CompoundFileIndexBenchmarks",
          "methodName": "LeanLucene_Index_NoCompound",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 222226691.8888889,
            "medianNanoseconds": 221951430.66666666,
            "minNanoseconds": 220313136.33333334,
            "maxNanoseconds": 224293569.33333334,
            "standardDeviationNanoseconds": 1345371.633807364,
            "operationsPerSecond": 4.499909491070452
          },
          "gc": {
            "bytesAllocatedPerOperation": 28472480,
            "gen0Collections": 10,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        },
        {
          "key": "CompoundFileIndexBenchmarks.LuceneNet_Index_Compound|DocumentCount=10000",
          "displayInfo": "CompoundFileIndexBenchmarks.LuceneNet_Index_Compound: DefaultJob [DocumentCount=10000]",
          "typeName": "CompoundFileIndexBenchmarks",
          "methodName": "LuceneNet_Index_Compound",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 185705783.5777778,
            "medianNanoseconds": 185936268.33333334,
            "minNanoseconds": 183368727.33333334,
            "maxNanoseconds": 187148334.33333334,
            "standardDeviationNanoseconds": 1091518.2085881268,
            "operationsPerSecond": 5.384861907551615
          },
          "gc": {
            "bytesAllocatedPerOperation": 69232901,
            "gen0Collections": 34,
            "gen1Collections": 4,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "compound-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.CompoundFileSearchBenchmarks-20260428-145802",
      "benchmarkCount": 3,
      "benchmarks": [
        {
          "key": "CompoundFileSearchBenchmarks.LeanLucene_Search_Compound|DocumentCount=10000",
          "displayInfo": "CompoundFileSearchBenchmarks.LeanLucene_Search_Compound: DefaultJob [DocumentCount=10000]",
          "typeName": "CompoundFileSearchBenchmarks",
          "methodName": "LeanLucene_Search_Compound",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 1219.7232601165772,
            "medianNanoseconds": 1217.5192756652832,
            "minNanoseconds": 1211.8032093048096,
            "maxNanoseconds": 1227.0807247161865,
            "standardDeviationNanoseconds": 4.919489908239656,
            "operationsPerSecond": 819858.104455943
          },
          "gc": {
            "bytesAllocatedPerOperation": 448,
            "gen0Collections": 56,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "CompoundFileSearchBenchmarks.LeanLucene_Search_NoCompound|DocumentCount=10000",
          "displayInfo": "CompoundFileSearchBenchmarks.LeanLucene_Search_NoCompound: DefaultJob [DocumentCount=10000]",
          "typeName": "CompoundFileSearchBenchmarks",
          "methodName": "LeanLucene_Search_NoCompound",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 1218.8829922993978,
            "medianNanoseconds": 1218.1921062469482,
            "minNanoseconds": 1208.160020828247,
            "maxNanoseconds": 1231.0814971923828,
            "standardDeviationNanoseconds": 5.709628406134338,
            "operationsPerSecond": 820423.2943750577
          },
          "gc": {
            "bytesAllocatedPerOperation": 448,
            "gen0Collections": 56,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "CompoundFileSearchBenchmarks.LuceneNet_Search_Compound|DocumentCount=10000",
          "displayInfo": "CompoundFileSearchBenchmarks.LuceneNet_Search_Compound: DefaultJob [DocumentCount=10000]",
          "typeName": "CompoundFileSearchBenchmarks",
          "methodName": "LuceneNet_Search_Compound",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 7712.327658081054,
            "medianNanoseconds": 7707.36376953125,
            "minNanoseconds": 7649.71337890625,
            "maxNanoseconds": 7771.207077026367,
            "standardDeviationNanoseconds": 36.374505236010705,
            "operationsPerSecond": 129662.54084811737
          },
          "gc": {
            "bytesAllocatedPerOperation": 12824,
            "gen0Collections": 200,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "deletion",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.DeletionBenchmarks-20260428-144925",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "DeletionBenchmarks.LeanLucene_DeleteDocuments|DocumentCount=10000",
          "displayInfo": "DeletionBenchmarks.LeanLucene_DeleteDocuments: DefaultJob [DocumentCount=10000]",
          "typeName": "DeletionBenchmarks",
          "methodName": "LeanLucene_DeleteDocuments",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 13,
            "meanNanoseconds": 222823061.58974355,
            "medianNanoseconds": 223055100,
            "minNanoseconds": 221577551.66666666,
            "maxNanoseconds": 224770921.33333334,
            "standardDeviationNanoseconds": 949413.3327925652,
            "operationsPerSecond": 4.487865810950825
          },
          "gc": {
            "bytesAllocatedPerOperation": 30176496,
            "gen0Collections": 10,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        },
        {
          "key": "DeletionBenchmarks.LuceneNet_DeleteDocuments|DocumentCount=10000",
          "displayInfo": "DeletionBenchmarks.LuceneNet_DeleteDocuments: DefaultJob [DocumentCount=10000]",
          "typeName": "DeletionBenchmarks",
          "methodName": "LuceneNet_DeleteDocuments",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 192749230.1777778,
            "medianNanoseconds": 192743244.66666666,
            "minNanoseconds": 190636559.66666666,
            "maxNanoseconds": 194205447,
            "standardDeviationNanoseconds": 1064832.84078568,
            "operationsPerSecond": 5.1880881655282005
          },
          "gc": {
            "bytesAllocatedPerOperation": 72817475,
            "gen0Collections": 36,
            "gen1Collections": 4,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "fuzzy",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.FuzzyQueryBenchmarks-20260428-144048",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery|DocumentCount=10000, QueryTerm=goverment",
          "displayInfo": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery: DefaultJob [QueryTerm=goverment, DocumentCount=10000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LeanLucene_FuzzyQuery",
          "parameters": {
            "QueryTerm": "goverment",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 348037.01536458335,
            "medianNanoseconds": 348564.2919921875,
            "minNanoseconds": 345090.34130859375,
            "maxNanoseconds": 350986.26318359375,
            "standardDeviationNanoseconds": 1787.1380767148862,
            "operationsPerSecond": 2873.2576015009727
          },
          "gc": {
            "bytesAllocatedPerOperation": 1928,
            "gen0Collections": 0,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery|DocumentCount=10000, QueryTerm=markts",
          "displayInfo": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery: DefaultJob [QueryTerm=markts, DocumentCount=10000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LeanLucene_FuzzyQuery",
          "parameters": {
            "QueryTerm": "markts",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 432117.86604817706,
            "medianNanoseconds": 432289.66748046875,
            "minNanoseconds": 430059.021484375,
            "maxNanoseconds": 434736.8876953125,
            "standardDeviationNanoseconds": 1526.7375356979792,
            "operationsPerSecond": 2314.1834174671903
          },
          "gc": {
            "bytesAllocatedPerOperation": 2200,
            "gen0Collections": 1,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery|DocumentCount=10000, QueryTerm=presiden",
          "displayInfo": "FuzzyQueryBenchmarks.LeanLucene_FuzzyQuery: DefaultJob [QueryTerm=presiden, DocumentCount=10000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LeanLucene_FuzzyQuery",
          "parameters": {
            "QueryTerm": "presiden",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 444550.5186523438,
            "medianNanoseconds": 444725.69921875,
            "minNanoseconds": 442766.63037109375,
            "maxNanoseconds": 446666.056640625,
            "standardDeviationNanoseconds": 1244.9972239891679,
            "operationsPerSecond": 2249.4631274562516
          },
          "gc": {
            "bytesAllocatedPerOperation": 1552,
            "gen0Collections": 0,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery|DocumentCount=10000, QueryTerm=goverment",
          "displayInfo": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery: DefaultJob [QueryTerm=goverment, DocumentCount=10000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LuceneNet_FuzzyQuery",
          "parameters": {
            "QueryTerm": "goverment",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 1702289.5048828125,
            "medianNanoseconds": 1704929.408203125,
            "minNanoseconds": 1685184.5,
            "maxNanoseconds": 1711545.43359375,
            "standardDeviationNanoseconds": 7039.8227944015225,
            "operationsPerSecond": 587.4441433913681
          },
          "gc": {
            "bytesAllocatedPerOperation": 1819462,
            "gen0Collections": 187,
            "gen1Collections": 76,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery|DocumentCount=10000, QueryTerm=markts",
          "displayInfo": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery: DefaultJob [QueryTerm=markts, DocumentCount=10000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LuceneNet_FuzzyQuery",
          "parameters": {
            "QueryTerm": "markts",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 1117606.66484375,
            "medianNanoseconds": 1117422.15625,
            "minNanoseconds": 1110871.05078125,
            "maxNanoseconds": 1126688.85546875,
            "standardDeviationNanoseconds": 5217.932996340406,
            "operationsPerSecond": 894.7691808368087
          },
          "gc": {
            "bytesAllocatedPerOperation": 1226706,
            "gen0Collections": 135,
            "gen1Collections": 44,
            "gen2Collections": 0
          }
        },
        {
          "key": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery|DocumentCount=10000, QueryTerm=presiden",
          "displayInfo": "FuzzyQueryBenchmarks.LuceneNet_FuzzyQuery: DefaultJob [QueryTerm=presiden, DocumentCount=10000]",
          "typeName": "FuzzyQueryBenchmarks",
          "methodName": "LuceneNet_FuzzyQuery",
          "parameters": {
            "QueryTerm": "presiden",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 1441738.7442708334,
            "medianNanoseconds": 1442969.724609375,
            "minNanoseconds": 1430623.953125,
            "maxNanoseconds": 1449004.08984375,
            "standardDeviationNanoseconds": 4708.938537634833,
            "operationsPerSecond": 693.6069409064504
          },
          "gc": {
            "bytesAllocatedPerOperation": 1573215,
            "gen0Collections": 163,
            "gen1Collections": 80,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexingBenchmarks-20260428-142443",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "IndexingBenchmarks.LeanLucene_IndexDocuments|DocumentCount=10000",
          "displayInfo": "IndexingBenchmarks.LeanLucene_IndexDocuments: DefaultJob [DocumentCount=10000]",
          "typeName": "IndexingBenchmarks",
          "methodName": "LeanLucene_IndexDocuments",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 220872261.5111111,
            "medianNanoseconds": 220653263,
            "minNanoseconds": 218279320.66666666,
            "maxNanoseconds": 222929072,
            "standardDeviationNanoseconds": 1476421.2711367845,
            "operationsPerSecond": 4.527503785031397
          },
          "gc": {
            "bytesAllocatedPerOperation": 28472696,
            "gen0Collections": 10,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        },
        {
          "key": "IndexingBenchmarks.LuceneNet_IndexDocuments|DocumentCount=10000",
          "displayInfo": "IndexingBenchmarks.LuceneNet_IndexDocuments: DefaultJob [DocumentCount=10000]",
          "typeName": "IndexingBenchmarks",
          "methodName": "LuceneNet_IndexDocuments",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 185684072.2666667,
            "medianNanoseconds": 185701841.33333334,
            "minNanoseconds": 184368387.33333334,
            "maxNanoseconds": 186647485.33333334,
            "standardDeviationNanoseconds": 674442.8947003206,
            "operationsPerSecond": 5.3854915383580595
          },
          "gc": {
            "bytesAllocatedPerOperation": 69231448,
            "gen0Collections": 34,
            "gen1Collections": 4,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-index",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortIndexBenchmarks-20260428-150004",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "IndexSortIndexBenchmarks.LeanLucene_Index_Sorted|DocumentCount=10000",
          "displayInfo": "IndexSortIndexBenchmarks.LeanLucene_Index_Sorted: DefaultJob [DocumentCount=10000]",
          "typeName": "IndexSortIndexBenchmarks",
          "methodName": "LeanLucene_Index_Sorted",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 255553374,
            "medianNanoseconds": 255785886,
            "minNanoseconds": 249151443.5,
            "maxNanoseconds": 259117443,
            "standardDeviationNanoseconds": 2542087.561567673,
            "operationsPerSecond": 3.9130768823267426
          },
          "gc": {
            "bytesAllocatedPerOperation": 33290816,
            "gen0Collections": 7,
            "gen1Collections": 3,
            "gen2Collections": 0
          }
        },
        {
          "key": "IndexSortIndexBenchmarks.LeanLucene_Index_Unsorted|DocumentCount=10000",
          "displayInfo": "IndexSortIndexBenchmarks.LeanLucene_Index_Unsorted: DefaultJob [DocumentCount=10000]",
          "typeName": "IndexSortIndexBenchmarks",
          "methodName": "LeanLucene_Index_Unsorted",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 231676359.1904762,
            "medianNanoseconds": 231526828.1666667,
            "minNanoseconds": 229524307.33333334,
            "maxNanoseconds": 234671699.33333334,
            "standardDeviationNanoseconds": 1476251.7148141398,
            "operationsPerSecond": 4.316366173459395
          },
          "gc": {
            "bytesAllocatedPerOperation": 32079245,
            "gen0Collections": 11,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "indexsort-search",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.IndexSortSearchBenchmarks-20260428-150150",
      "benchmarkCount": 2,
      "benchmarks": [
        {
          "key": "IndexSortSearchBenchmarks.LeanLucene_SortedSearch_EarlyTermination|DocumentCount=10000",
          "displayInfo": "IndexSortSearchBenchmarks.LeanLucene_SortedSearch_EarlyTermination: DefaultJob [DocumentCount=10000]",
          "typeName": "IndexSortSearchBenchmarks",
          "methodName": "LeanLucene_SortedSearch_EarlyTermination",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 401.195166460673,
            "medianNanoseconds": 401.29751348495483,
            "minNanoseconds": 398.2164535522461,
            "maxNanoseconds": 403.466769695282,
            "standardDeviationNanoseconds": 1.4435422384020797,
            "operationsPerSecond": 2492552.4622391593
          },
          "gc": {
            "bytesAllocatedPerOperation": 432,
            "gen0Collections": 216,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "IndexSortSearchBenchmarks.LeanLucene_SortedSearch_PostSort|DocumentCount=10000",
          "displayInfo": "IndexSortSearchBenchmarks.LeanLucene_SortedSearch_PostSort: DefaultJob [DocumentCount=10000]",
          "typeName": "IndexSortSearchBenchmarks",
          "methodName": "LeanLucene_SortedSearch_PostSort",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 291.8620505332947,
            "medianNanoseconds": 291.8273015022278,
            "minNanoseconds": 290.1446695327759,
            "maxNanoseconds": 293.91954278945923,
            "standardDeviationNanoseconds": 1.0313232803137558,
            "operationsPerSecond": 3426276.2088212054
          },
          "gc": {
            "bytesAllocatedPerOperation": 312,
            "gen0Collections": 156,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "phrase",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PhraseQueryBenchmarks-20260428-143233",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery|DocumentCount=10000, PhraseType=ExactThreeWord",
          "displayInfo": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery: DefaultJob [PhraseType=ExactThreeWord, DocumentCount=10000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LeanLucene_PhraseQuery",
          "parameters": {
            "PhraseType": "ExactThreeWord",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 2142.143982751029,
            "medianNanoseconds": 2144.6371269226074,
            "minNanoseconds": 2116.585147857666,
            "maxNanoseconds": 2156.0106658935547,
            "standardDeviationNanoseconds": 11.713994809840573,
            "operationsPerSecond": 466822.028795543
          },
          "gc": {
            "bytesAllocatedPerOperation": 3248,
            "gen0Collections": 203,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery|DocumentCount=10000, PhraseType=ExactTwoWord",
          "displayInfo": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery: DefaultJob [PhraseType=ExactTwoWord, DocumentCount=10000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LeanLucene_PhraseQuery",
          "parameters": {
            "PhraseType": "ExactTwoWord",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 2068.853173828125,
            "medianNanoseconds": 2072.770839691162,
            "minNanoseconds": 2050.994239807129,
            "maxNanoseconds": 2078.7267837524414,
            "standardDeviationNanoseconds": 8.351493659863012,
            "operationsPerSecond": 483359.5794280747
          },
          "gc": {
            "bytesAllocatedPerOperation": 2936,
            "gen0Collections": 184,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery|DocumentCount=10000, PhraseType=SlopTwoWord",
          "displayInfo": "PhraseQueryBenchmarks.LeanLucene_PhraseQuery: DefaultJob [PhraseType=SlopTwoWord, DocumentCount=10000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LeanLucene_PhraseQuery",
          "parameters": {
            "PhraseType": "SlopTwoWord",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 39170.143501790364,
            "medianNanoseconds": 39164.72930908203,
            "minNanoseconds": 38969.64129638672,
            "maxNanoseconds": 39348.00915527344,
            "standardDeviationNanoseconds": 116.96738367223362,
            "operationsPerSecond": 25529.648620110176
          },
          "gc": {
            "bytesAllocatedPerOperation": 4576,
            "gen0Collections": 17,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery|DocumentCount=10000, PhraseType=ExactThreeWord",
          "displayInfo": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery: DefaultJob [PhraseType=ExactThreeWord, DocumentCount=10000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LuceneNet_PhraseQuery",
          "parameters": {
            "PhraseType": "ExactThreeWord",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 10269.582908630371,
            "medianNanoseconds": 10263.48666381836,
            "minNanoseconds": 10201.400619506836,
            "maxNanoseconds": 10376.772689819336,
            "standardDeviationNanoseconds": 53.62141738520012,
            "operationsPerSecond": 97374.9380960368
          },
          "gc": {
            "bytesAllocatedPerOperation": 26896,
            "gen0Collections": 421,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery|DocumentCount=10000, PhraseType=ExactTwoWord",
          "displayInfo": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery: DefaultJob [PhraseType=ExactTwoWord, DocumentCount=10000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LuceneNet_PhraseQuery",
          "parameters": {
            "PhraseType": "ExactTwoWord",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 7406.9266774495445,
            "medianNanoseconds": 7393.61442565918,
            "minNanoseconds": 7316.151084899902,
            "maxNanoseconds": 7514.064147949219,
            "standardDeviationNanoseconds": 60.783233929716864,
            "operationsPerSecond": 135008.7618721148
          },
          "gc": {
            "bytesAllocatedPerOperation": 19872,
            "gen0Collections": 622,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery|DocumentCount=10000, PhraseType=SlopTwoWord",
          "displayInfo": "PhraseQueryBenchmarks.LuceneNet_PhraseQuery: DefaultJob [PhraseType=SlopTwoWord, DocumentCount=10000]",
          "typeName": "PhraseQueryBenchmarks",
          "methodName": "LuceneNet_PhraseQuery",
          "parameters": {
            "PhraseType": "SlopTwoWord",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 9938.870398966472,
            "medianNanoseconds": 9937.76335144043,
            "minNanoseconds": 9817.733337402344,
            "maxNanoseconds": 10071.171890258789,
            "standardDeviationNanoseconds": 82.90010617184487,
            "operationsPerSecond": 100615.05582203672
          },
          "gc": {
            "bytesAllocatedPerOperation": 25496,
            "gen0Collections": 399,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "prefix",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.PrefixQueryBenchmarks-20260428-143633",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery|DocumentCount=10000, QueryPrefix=gov",
          "displayInfo": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery: DefaultJob [QueryPrefix=gov, DocumentCount=10000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LeanLucene_PrefixQuery",
          "parameters": {
            "QueryPrefix": "gov",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 3479.519843292236,
            "medianNanoseconds": 3482.986888885498,
            "minNanoseconds": 3463.131103515625,
            "maxNanoseconds": 3495.435333251953,
            "standardDeviationNanoseconds": 9.926892865849794,
            "operationsPerSecond": 287395.9756050204
          },
          "gc": {
            "bytesAllocatedPerOperation": 1264,
            "gen0Collections": 79,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery|DocumentCount=10000, QueryPrefix=mark",
          "displayInfo": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery: DefaultJob [QueryPrefix=mark, DocumentCount=10000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LeanLucene_PrefixQuery",
          "parameters": {
            "QueryPrefix": "mark",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 1693.4567990984235,
            "medianNanoseconds": 1694.8531646728516,
            "minNanoseconds": 1680.9102764129639,
            "maxNanoseconds": 1700.5884990692139,
            "standardDeviationNanoseconds": 6.049977099392853,
            "operationsPerSecond": 590508.1254699784
          },
          "gc": {
            "bytesAllocatedPerOperation": 816,
            "gen0Collections": 102,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery|DocumentCount=10000, QueryPrefix=pres",
          "displayInfo": "PrefixQueryBenchmarks.LeanLucene_PrefixQuery: DefaultJob [QueryPrefix=pres, DocumentCount=10000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LeanLucene_PrefixQuery",
          "parameters": {
            "QueryPrefix": "pres",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 9558.716664995465,
            "medianNanoseconds": 9555.500968933105,
            "minNanoseconds": 9469.606552124023,
            "maxNanoseconds": 9605.498596191406,
            "standardDeviationNanoseconds": 32.66471243201574,
            "operationsPerSecond": 104616.55419310144
          },
          "gc": {
            "bytesAllocatedPerOperation": 2064,
            "gen0Collections": 32,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery|DocumentCount=10000, QueryPrefix=gov",
          "displayInfo": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery: DefaultJob [QueryPrefix=gov, DocumentCount=10000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LuceneNet_PrefixQuery",
          "parameters": {
            "QueryPrefix": "gov",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 15122.319864908854,
            "medianNanoseconds": 15084.543518066406,
            "minNanoseconds": 14967.92886352539,
            "maxNanoseconds": 15284.322082519531,
            "standardDeviationNanoseconds": 100.75641297912689,
            "operationsPerSecond": 66127.42019301464
          },
          "gc": {
            "bytesAllocatedPerOperation": 55088,
            "gen0Collections": 862,
            "gen1Collections": 95,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery|DocumentCount=10000, QueryPrefix=mark",
          "displayInfo": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery: DefaultJob [QueryPrefix=mark, DocumentCount=10000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LuceneNet_PrefixQuery",
          "parameters": {
            "QueryPrefix": "mark",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 11423.973927815756,
            "medianNanoseconds": 11435.287521362305,
            "minNanoseconds": 11311.227172851562,
            "maxNanoseconds": 11550.543640136719,
            "standardDeviationNanoseconds": 80.87213815599249,
            "operationsPerSecond": 87535.2137810068
          },
          "gc": {
            "bytesAllocatedPerOperation": 54632,
            "gen0Collections": 851,
            "gen1Collections": 1,
            "gen2Collections": 0
          }
        },
        {
          "key": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery|DocumentCount=10000, QueryPrefix=pres",
          "displayInfo": "PrefixQueryBenchmarks.LuceneNet_PrefixQuery: DefaultJob [QueryPrefix=pres, DocumentCount=10000]",
          "typeName": "PrefixQueryBenchmarks",
          "methodName": "LuceneNet_PrefixQuery",
          "parameters": {
            "QueryPrefix": "pres",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 16485.222580973306,
            "medianNanoseconds": 16498.42904663086,
            "minNanoseconds": 16284.344665527344,
            "maxNanoseconds": 16624.705688476562,
            "standardDeviationNanoseconds": 84.88642638882078,
            "operationsPerSecond": 60660.38811961002
          },
          "gc": {
            "bytesAllocatedPerOperation": 54480,
            "gen0Collections": 425,
            "gen1Collections": 2,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "query",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.TermQueryBenchmarks-20260428-142105",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "TermQueryBenchmarks.LeanLucene_TermQuery|DocumentCount=10000, QueryTerm=government",
          "displayInfo": "TermQueryBenchmarks.LeanLucene_TermQuery: DefaultJob [QueryTerm=government, DocumentCount=10000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LeanLucene_TermQuery",
          "parameters": {
            "QueryTerm": "government",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 513.0442989985148,
            "medianNanoseconds": 513.2618799209595,
            "minNanoseconds": 509.79915046691895,
            "maxNanoseconds": 516.0760803222656,
            "standardDeviationNanoseconds": 2.1189354342039284,
            "operationsPerSecond": 1949149.4242349914
          },
          "gc": {
            "bytesAllocatedPerOperation": 304,
            "gen0Collections": 76,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LeanLucene_TermQuery|DocumentCount=10000, QueryTerm=people",
          "displayInfo": "TermQueryBenchmarks.LeanLucene_TermQuery: DefaultJob [QueryTerm=people, DocumentCount=10000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LeanLucene_TermQuery",
          "parameters": {
            "QueryTerm": "people",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 22948.51382446289,
            "medianNanoseconds": 22924.574310302734,
            "minNanoseconds": 22828.93731689453,
            "maxNanoseconds": 23084.775939941406,
            "standardDeviationNanoseconds": 80.44222127474916,
            "operationsPerSecond": 43575.80659249532
          },
          "gc": {
            "bytesAllocatedPerOperation": 472,
            "gen0Collections": 3,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LeanLucene_TermQuery|DocumentCount=10000, QueryTerm=said",
          "displayInfo": "TermQueryBenchmarks.LeanLucene_TermQuery: DefaultJob [QueryTerm=said, DocumentCount=10000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LeanLucene_TermQuery",
          "parameters": {
            "QueryTerm": "said",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 40732.192708333336,
            "medianNanoseconds": 40711.08166503906,
            "minNanoseconds": 40546.09698486328,
            "maxNanoseconds": 40964.75769042969,
            "standardDeviationNanoseconds": 119.21769994620459,
            "operationsPerSecond": 24550.605639146244
          },
          "gc": {
            "bytesAllocatedPerOperation": 464,
            "gen0Collections": 1,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LuceneNet_TermQuery|DocumentCount=10000, QueryTerm=government",
          "displayInfo": "TermQueryBenchmarks.LuceneNet_TermQuery: DefaultJob [QueryTerm=government, DocumentCount=10000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LuceneNet_TermQuery",
          "parameters": {
            "QueryTerm": "government",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 6700.043831888835,
            "medianNanoseconds": 6692.573265075684,
            "minNanoseconds": 6629.933456420898,
            "maxNanoseconds": 6783.187576293945,
            "standardDeviationNanoseconds": 42.01998526366274,
            "operationsPerSecond": 149252.75492087135
          },
          "gc": {
            "bytesAllocatedPerOperation": 12992,
            "gen0Collections": 407,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LuceneNet_TermQuery|DocumentCount=10000, QueryTerm=people",
          "displayInfo": "TermQueryBenchmarks.LuceneNet_TermQuery: DefaultJob [QueryTerm=people, DocumentCount=10000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LuceneNet_TermQuery",
          "parameters": {
            "QueryTerm": "people",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 32601.686462402344,
            "medianNanoseconds": 32620.462829589844,
            "minNanoseconds": 32333.961669921875,
            "maxNanoseconds": 32797.69549560547,
            "standardDeviationNanoseconds": 131.22847753937597,
            "operationsPerSecond": 30673.259837439473
          },
          "gc": {
            "bytesAllocatedPerOperation": 13192,
            "gen0Collections": 51,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "TermQueryBenchmarks.LuceneNet_TermQuery|DocumentCount=10000, QueryTerm=said",
          "displayInfo": "TermQueryBenchmarks.LuceneNet_TermQuery: DefaultJob [QueryTerm=said, DocumentCount=10000]",
          "typeName": "TermQueryBenchmarks",
          "methodName": "LuceneNet_TermQuery",
          "parameters": {
            "QueryTerm": "said",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 51534.99840494792,
            "medianNanoseconds": 51644.209899902344,
            "minNanoseconds": 51000.29675292969,
            "maxNanoseconds": 51839.945068359375,
            "standardDeviationNanoseconds": 256.10902601608854,
            "operationsPerSecond": 19404.288948304093
          },
          "gc": {
            "bytesAllocatedPerOperation": 12888,
            "gen0Collections": 50,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "schemajson",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SchemaAndJsonBenchmarks-20260428-145323",
      "benchmarkCount": 3,
      "benchmarks": [
        {
          "key": "SchemaAndJsonBenchmarks.LeanLucene_Index_NoSchema|DocumentCount=10000",
          "displayInfo": "SchemaAndJsonBenchmarks.LeanLucene_Index_NoSchema: DefaultJob [DocumentCount=10000]",
          "typeName": "SchemaAndJsonBenchmarks",
          "methodName": "LeanLucene_Index_NoSchema",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 220726162.1666667,
            "medianNanoseconds": 221202750.5,
            "minNanoseconds": 218545535.66666666,
            "maxNanoseconds": 222335047.33333334,
            "standardDeviationNanoseconds": 1134728.9463637332,
            "operationsPerSecond": 4.530500554097962
          },
          "gc": {
            "bytesAllocatedPerOperation": 28472872,
            "gen0Collections": 10,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        },
        {
          "key": "SchemaAndJsonBenchmarks.LeanLucene_Index_WithSchema|DocumentCount=10000",
          "displayInfo": "SchemaAndJsonBenchmarks.LeanLucene_Index_WithSchema: DefaultJob [DocumentCount=10000]",
          "typeName": "SchemaAndJsonBenchmarks",
          "methodName": "LeanLucene_Index_WithSchema",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 221826912.24444443,
            "medianNanoseconds": 221584817.66666666,
            "minNanoseconds": 220174286,
            "maxNanoseconds": 223320621,
            "standardDeviationNanoseconds": 909156.2622208615,
            "operationsPerSecond": 4.508019292528581
          },
          "gc": {
            "bytesAllocatedPerOperation": 28873208,
            "gen0Collections": 10,
            "gen1Collections": 5,
            "gen2Collections": 0
          }
        },
        {
          "key": "SchemaAndJsonBenchmarks.LeanLucene_JsonMapping|DocumentCount=10000",
          "displayInfo": "SchemaAndJsonBenchmarks.LeanLucene_JsonMapping: DefaultJob [DocumentCount=10000]",
          "typeName": "SchemaAndJsonBenchmarks",
          "methodName": "LeanLucene_JsonMapping",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 16077722.80357143,
            "medianNanoseconds": 16083764.59375,
            "minNanoseconds": 15890438.375,
            "maxNanoseconds": 16209135.1875,
            "standardDeviationNanoseconds": 85346.26043459923,
            "operationsPerSecond": 62.19786298205519
          },
          "gc": {
            "bytesAllocatedPerOperation": 8704544,
            "gen0Collections": 66,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "suggester",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.SuggesterBenchmarks-20260428-145111",
      "benchmarkCount": 3,
      "benchmarks": [
        {
          "key": "SuggesterBenchmarks.LeanLucene_DidYouMean|DocumentCount=10000",
          "displayInfo": "SuggesterBenchmarks.LeanLucene_DidYouMean: DefaultJob [DocumentCount=10000]",
          "typeName": "SuggesterBenchmarks",
          "methodName": "LeanLucene_DidYouMean",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 325938.40556640626,
            "medianNanoseconds": 325855.298828125,
            "minNanoseconds": 324215.29736328125,
            "maxNanoseconds": 327490.81298828125,
            "standardDeviationNanoseconds": 927.1342454606781,
            "operationsPerSecond": 3068.064342593286
          },
          "gc": {
            "bytesAllocatedPerOperation": 7680,
            "gen0Collections": 3,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "SuggesterBenchmarks.LeanLucene_SpellIndex|DocumentCount=10000",
          "displayInfo": "SuggesterBenchmarks.LeanLucene_SpellIndex: DefaultJob [DocumentCount=10000]",
          "typeName": "SuggesterBenchmarks",
          "methodName": "LeanLucene_SpellIndex",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 325746.3231119792,
            "medianNanoseconds": 325366.40771484375,
            "minNanoseconds": 322636.61767578125,
            "maxNanoseconds": 328477.19921875,
            "standardDeviationNanoseconds": 1439.4032267620694,
            "operationsPerSecond": 3069.873484515858
          },
          "gc": {
            "bytesAllocatedPerOperation": 5920,
            "gen0Collections": 2,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "SuggesterBenchmarks.LuceneNet_SpellChecker|DocumentCount=10000",
          "displayInfo": "SuggesterBenchmarks.LuceneNet_SpellChecker: DefaultJob [DocumentCount=10000]",
          "typeName": "SuggesterBenchmarks",
          "methodName": "LuceneNet_SpellChecker",
          "parameters": {
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 14,
            "meanNanoseconds": 7410029.4375,
            "medianNanoseconds": 7421168.3203125,
            "minNanoseconds": 7312299.84375,
            "maxNanoseconds": 7450017.5703125,
            "standardDeviationNanoseconds": 38155.752619696956,
            "operationsPerSecond": 134.95223041075806
          },
          "gc": {
            "bytesAllocatedPerOperation": 5151600,
            "gen0Collections": 157,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        }
      ]
    },
    {
      "suiteName": "wildcard",
      "summaryTitle": "Rowles.LeanLucene.Benchmarks.WildcardQueryBenchmarks-20260428-144502",
      "benchmarkCount": 6,
      "benchmarks": [
        {
          "key": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery|DocumentCount=10000, WildcardPattern=gov*",
          "displayInfo": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery: DefaultJob [WildcardPattern=gov*, DocumentCount=10000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LeanLucene_WildcardQuery",
          "parameters": {
            "WildcardPattern": "gov*",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 3759.5900489807127,
            "medianNanoseconds": 3758.7474517822266,
            "minNanoseconds": 3734.9611320495605,
            "maxNanoseconds": 3780.6639518737793,
            "standardDeviationNanoseconds": 13.571324865733914,
            "operationsPerSecond": 265986.44718487764
          },
          "gc": {
            "bytesAllocatedPerOperation": 1544,
            "gen0Collections": 96,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery|DocumentCount=10000, WildcardPattern=m*rket",
          "displayInfo": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery: DefaultJob [WildcardPattern=m*rket, DocumentCount=10000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LeanLucene_WildcardQuery",
          "parameters": {
            "WildcardPattern": "m*rket",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 31623.612141927082,
            "medianNanoseconds": 31667.22540283203,
            "minNanoseconds": 31374.03759765625,
            "maxNanoseconds": 31835.254272460938,
            "standardDeviationNanoseconds": 146.15316871109334,
            "operationsPerSecond": 31621.941083516653
          },
          "gc": {
            "bytesAllocatedPerOperation": 23072,
            "gen0Collections": 90,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery|DocumentCount=10000, WildcardPattern=pre*dent",
          "displayInfo": "WildcardQueryBenchmarks.LeanLucene_WildcardQuery: DefaultJob [WildcardPattern=pre*dent, DocumentCount=10000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LeanLucene_WildcardQuery",
          "parameters": {
            "WildcardPattern": "pre*dent",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 2552.6525382995605,
            "medianNanoseconds": 2552.830047607422,
            "minNanoseconds": 2533.1009216308594,
            "maxNanoseconds": 2572.0632667541504,
            "standardDeviationNanoseconds": 11.39942984207492,
            "operationsPerSecond": 391749.3607125026
          },
          "gc": {
            "bytesAllocatedPerOperation": 2224,
            "gen0Collections": 139,
            "gen1Collections": 0,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery|DocumentCount=10000, WildcardPattern=gov*",
          "displayInfo": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery: DefaultJob [WildcardPattern=gov*, DocumentCount=10000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LuceneNet_WildcardQuery",
          "parameters": {
            "WildcardPattern": "gov*",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 28348.204740397134,
            "medianNanoseconds": 28316.083709716797,
            "minNanoseconds": 27912.658569335938,
            "maxNanoseconds": 28661.53369140625,
            "standardDeviationNanoseconds": 208.69808231307127,
            "operationsPerSecond": 35275.602429065526
          },
          "gc": {
            "bytesAllocatedPerOperation": 74568,
            "gen0Collections": 583,
            "gen1Collections": 38,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery|DocumentCount=10000, WildcardPattern=m*rket",
          "displayInfo": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery: DefaultJob [WildcardPattern=m*rket, DocumentCount=10000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LuceneNet_WildcardQuery",
          "parameters": {
            "WildcardPattern": "m*rket",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 214245.16321614583,
            "medianNanoseconds": 214499.43334960938,
            "minNanoseconds": 212467.84497070312,
            "maxNanoseconds": 216560.9140625,
            "standardDeviationNanoseconds": 1236.168517348577,
            "operationsPerSecond": 4667.54994599868
          },
          "gc": {
            "bytesAllocatedPerOperation": 355832,
            "gen0Collections": 348,
            "gen1Collections": 7,
            "gen2Collections": 0
          }
        },
        {
          "key": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery|DocumentCount=10000, WildcardPattern=pre*dent",
          "displayInfo": "WildcardQueryBenchmarks.LuceneNet_WildcardQuery: DefaultJob [WildcardPattern=pre*dent, DocumentCount=10000]",
          "typeName": "WildcardQueryBenchmarks",
          "methodName": "LuceneNet_WildcardQuery",
          "parameters": {
            "WildcardPattern": "pre*dent",
            "DocumentCount": "10000"
          },
          "statistics": {
            "sampleCount": 15,
            "meanNanoseconds": 237977.25865885417,
            "medianNanoseconds": 238257.97045898438,
            "minNanoseconds": 236026.22802734375,
            "maxNanoseconds": 240482.025390625,
            "standardDeviationNanoseconds": 1432.6678375419087,
            "operationsPerSecond": 4202.082189010854
          },
          "gc": {
            "bytesAllocatedPerOperation": 373224,
            "gen0Collections": 365,
            "gen1Collections": 3,
            "gen2Collections": 0
          }
        }
      ]
    }
  ]
}</code></pre>

</details>

