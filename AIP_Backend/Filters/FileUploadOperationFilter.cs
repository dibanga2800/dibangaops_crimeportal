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
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) || 
                           p.ParameterType == typeof(IFormFileCollection))
                .ToList();

            if (fileParameters.Any())
            {
                // Clear existing parameters that are IFormFile
                operation.Parameters = operation.Parameters?
                    .Where(p => !fileParameters.Any(fp => fp.Name == p.Name))
                    .ToList();

                // Create request body for file upload
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = fileParameters.ToDictionary(
                                    p => p.Name ?? "file",
                                    p => new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary",
                                        Description = "File to upload"
                                    }
                                ),
                                Required = fileParameters
                                    .Where(p => !p.HasDefaultValue && !p.IsOptional)
                                    .Select(p => p.Name ?? "file")
                                    .ToHashSet()
                            }
                        }
                    }
                };
            }
        }
    }
}

