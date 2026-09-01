using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StudentRoadMap.Api.Swagger;

/// <summary>
/// `AddSwaggerGen`dagi `options.SupportNonNullableReferenceTypes()` faqat har bir xususiyatning
/// `nullable` bayrog'ini to'g'ri hisoblaydi — lekin OpenAPI `required` ro'yxatini TO'LDIRMAYDI
/// (Swashbuckle.AspNetCore 10.2.3'da tekshirilgan xatti-harakat: sxemalar `required: []` bilan
/// chiqadi, xususiyat nullable=false bo'lsa ham). Natijada `npm run generate:api` barcha
/// maydonlarni ixtiyoriy (`?`) deb generatsiya qiladi — frontend agentining xabari (2026-09-02).
///
/// Bu filtr C#ning HAQIQIY non-nullable xususiyatlarini (`NullabilityInfoContext` orqali — ham
/// reference, ham value tiplar, ya'ni `string`/`Guid`/`int` singari `Nullable&lt;T&gt;` bo'lmagan
/// va NRT bo'yicha `?` belgilanmagan hammasi) `required`ga qo'shadi. `T?`/`Nullable&lt;T&gt;`
/// xususiyatlar tegilmaydi — ular allaqachon `nullable: true` bilan to'g'ri belgilangan.
/// </summary>
public sealed class RequiredNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema || concreteSchema.Properties is null || concreteSchema.Properties.Count == 0)
        {
            return;
        }

        concreteSchema.Required ??= new HashSet<string>();

        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue; // Indekslovchi (`this[int]`) — oddiy JSON xususiyati emas.
            }

            var jsonName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!concreteSchema.Properties.ContainsKey(jsonName))
            {
                continue;
            }

            var nullabilityInfo = NullabilityContext.Create(property);
            var isNullable = nullabilityInfo.WriteState == NullabilityState.Nullable
                || nullabilityInfo.ReadState == NullabilityState.Nullable;

            if (!isNullable)
            {
                concreteSchema.Required.Add(jsonName);
            }
        }
    }
}
