# OpenTelemetry

LeanLucene exposes activities and metrics through the standard BCL APIs. Your app
can export them to any OpenTelemetry backend, including Aspire, Jaeger and
Prometheus.

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
`leanlucene.index.commit.duration`, `leanlucene.hnsw.search.duration`,
`leanlucene.hnsw.search.nodes_visited`, `leanlucene.hnsw.build.duration`,
`leanlucene.hnsw.build.nodes`.

## Wire MeterMetricsCollector

```csharp
using Rowles.LeanLucene.Diagnostics;

var collector = new MeterMetricsCollector();

var writerConfig   = new IndexWriterConfig    { Metrics = collector };
var searcherConfig = new IndexSearcherConfig  { Metrics = collector };
```

In hosted apps, pass the DI-managed `IMeterFactory` into `MeterMetricsCollector`.

## OTLP export

Let environment variables configure the endpoint. This keeps the same code
working under Aspire AppHost, Aspire Runner and other OTLP collectors.

```csharp
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MySearchApp"))
    .WithTracing(t => t
        .AddSource("Rowles.LeanLucene")
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddMeter("Rowles.LeanLucene")
        .AddOtlpExporter());
```

For a local collector:

```powershell
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
$env:OTEL_EXPORTER_OTLP_PROTOCOL = "grpc"
```

## Structured logs

LeanLucene does not log directly. Application logs can still be exported to the
same OTLP endpoint:

```csharp
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes           = true;
    logging.ParseStateValues        = true;
    logging.AddOtlpExporter();
});
```

Use message templates, not string interpolation, so log values become structured
attributes:

```csharp
logger.LogInformation("Search {QueryType} returned {HitCount} hits", queryType, hits);
```

## Aspire Runner

Aspire Runner defaults to HTTPS for the dashboard and OTLP. If your app uses
`http://localhost:4317`, start the dashboard without HTTPS:

```powershell
aspire-dashboard -s false
```

Or set `OTEL_EXPORTER_OTLP_ENDPOINT` to `https://localhost:4317` and trust the
certificate used by the dashboard.

A worked example lives in `src/examples/Rowles.LeanLucene.Example.Telemetry`.

## See also

- [Aspire dashboard](05-aspire-dashboard.md)
- <xref:Rowles.LeanLucene.Diagnostics.MeterMetricsCollector>
