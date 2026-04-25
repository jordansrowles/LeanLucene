# OpenTelemetry

LeanLucene exposes activities and metrics through the standard BCL APIs. Wire them
into OpenTelemetry to export to any compatible backend (Aspire dashboard, Jaeger,
Prometheus, etc.).

## Source names

| Kind | Name |
|---|---|
| `ActivitySource` | `Rowles.LeanLucene` |
| `Meter` | `Rowles.LeanLucene` |

## Activities

| Activity | Tags |
|---|---|
| `leanlucene.search` | `query.type`, `search.total_hits`, `search.cache_hit` |
| `leanlucene.index.commit` | `index.commit_generation`, `index.segment_count` |
| `leanlucene.index.flush` | `index.segment_id`, `index.doc_count` |
| `leanlucene.index.merge` | `index.segments_merged` |

Activities are only allocated when a listener is attached.

## Metric instruments

`leanlucene.search.duration`, `leanlucene.search.count`, `leanlucene.cache.hits`,
`leanlucene.cache.misses`, `leanlucene.index.flush.duration`,
`leanlucene.index.merge.duration`, `leanlucene.index.merge.segments`,
`leanlucene.index.commit.duration`.

## Wire MeterMetricsCollector

```csharp
using Rowles.LeanLucene.Diagnostics;

var collector = new MeterMetricsCollector();

var writerConfig   = new IndexWriterConfig    { Metrics = collector };
var searcherConfig = new IndexSearcherConfig  { Metrics = collector };
```

## OTLP export to localhost:4317

```csharp
using var tracer = Sdk.CreateTracerProviderBuilder()
    .AddSource("Rowles.LeanLucene")
    .AddOtlpExporter(o => o.Endpoint = new Uri("http://localhost:4317"))
    .Build();

using var meter = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Rowles.LeanLucene")
    .AddOtlpExporter(o => o.Endpoint = new Uri("http://localhost:4317"))
    .Build();
```

A worked example lives in `src/examples/Rowles.LeanLucene.Example.Telemetry`.

## See also

- <xref:Rowles.LeanLucene.Diagnostics.MeterMetricsCollector>
