using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                // Nullable<IFormFile> if ever used as value-type nullable
                if (Nullable.GetUnderlyingType(t) is { } ut &&
                    (ut == typeof(IFormFile) || ut == typeof(IFormFileCollection)))
                    return true;
                return false;
            }

            var fileParameters = context.MethodInfo.GetParameters().Where(IsFormFileParameter).ToList();

            if (fileParameters.Count == 0)
                return;

            // Clear existing parameters that are IFormFile (Parameters can be null before assignment).
            var existing = operation.Parameters ?? new List<OpenApiParameter>();
            operation.Parameters = existing
                .Where(p => fileParameters.All(fp => fp.Name != p.Name))
                .ToList();

            // Build property map with unique keys (duplicate param names would throw otherwise).
            var properties = new Dictionary<string, OpenApiSchema>();
            var required = new HashSet<string>();
            foreach (var p in fileParameters)
            {
                var key = string.IsNullOrEmpty(p.Name) ? "file" : p.Name;
                var baseKey = key;
                for (var i = 1; properties.ContainsKey(key); i++)
                    key = $"{baseKey}_{i}";

                properties[key] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = "File to upload"
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
                            Type = "object",
                            Properties = properties,
                            Required = required
                        }
                    }
                }
            };
        }
    }
}

