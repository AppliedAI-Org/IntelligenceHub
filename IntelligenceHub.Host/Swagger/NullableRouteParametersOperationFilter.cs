using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IntelligenceHub.Host.Swagger
{
    /// <summary>
    /// A Swagger operation filter that ensures optional route parameters 
    /// are correctly marked as optional in the OpenAPI specification.
    /// </summary>
    public class NullableRouteParametersOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to modify Swagger operation parameters.
        /// </summary>
        /// <param name="operation">The OpenAPI operation being processed.</param>
        /// <param name="context">The context of the operation, including API description and metadata.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                return;

            foreach (var parameter in operation.Parameters)
            {
                // Retrieve the corresponding API parameter description
                var description = context.ApiDescription.ParameterDescriptions.FirstOrDefault(p => p.Name == parameter.Name);

                // Mark the parameter as optional in Swagger only if it is explicitly optional in the route definition
                if (description?.RouteInfo?.IsOptional == true) parameter.Required = false;
            }
        }
    }
}
