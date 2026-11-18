using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pkmds.Web.Services;

[JsonSerializable(typeof(JsonElement))]
internal sealed partial class BugReportJsonContext : JsonSerializerContext;
