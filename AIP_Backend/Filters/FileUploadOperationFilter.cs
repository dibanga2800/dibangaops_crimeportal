using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AIPBackend.Filters
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            static bool IsFormFileParameter(System.Reflection.ParameterInfo p)
            {
                var t = p.ParameterType;
                if (t == typeof(IFormFile) || t == typeof(IFormFileCollection))
                    return true;
                if (Nullable.GetUnderlyingType(t) is { } ut &&
                    (ut == typeof(IFormFile) || ut == typeof(IFormFileCollection)))
                    return true;
                return false;
            }

            var fileParameters = context.MethodInfo.GetParameters().Where(IsFormFileParameter).ToList();

            if (fileParameters.Count == 0)
                return;

            operation.Parameters ??= [];
            operation.Parameters = operation.Parameters
                .Where(p => fileParameters.All(fp => fp.Name != p.Name))
                .ToList();

            var properties = new Dictionary<string, IOpenApiSchema>();
            var required = new HashSet<string>();
            foreach (var p in fileParameters)
            {
                var key = string.IsNullOrEmpty(p.Name) ? "file" : p.Name;
                var baseKey = key;
                for (var i = 1; properties.ContainsKey(key); i++)
                    key = $"{baseKey}_{i}";

                properties[key] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "binary",
                    Description = "File to upload",
                };

                if (!p.HasDefaultValue && !p.IsOptional)
                    required.Add(key);
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = properties,
                            Required = required,
                        },
                    },
                },
            };
        }
    }
}
