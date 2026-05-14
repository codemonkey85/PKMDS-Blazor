using System.Text.Json.Serialization;
using Pkmds.Rcl.Models;

namespace Pkmds.Rcl.Services.Json;

[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(List<BatchEditorPreset>), TypeInfoPropertyName = "BatchEditorPresets")]
[JsonSerializable(typeof(Dictionary<string, AdvancedSearchFilter>), TypeInfoPropertyName = "SavedSearchFilters")]
[JsonSerializable(typeof(PokePasteResponse))]
[JsonSerializable(typeof(BenchmarkReport))]
internal sealed partial class PkmdsJsonContext : JsonSerializerContext;
