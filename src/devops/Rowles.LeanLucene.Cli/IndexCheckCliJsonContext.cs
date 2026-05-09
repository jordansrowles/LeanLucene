using System.Text.Json.Serialization;

namespace Rowles.LeanLucene.Cli;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CliIndexCheckResultDto))]
internal sealed partial class IndexCheckCliJsonContext : JsonSerializerContext;
