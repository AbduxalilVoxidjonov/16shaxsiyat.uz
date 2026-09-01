using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>API bilan bir xil JSON sozlamalari (camelCase + string enum) — `Program.cs`dagi `AddJsonOptions`ga mos.</summary>
internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
