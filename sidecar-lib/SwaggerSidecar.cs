using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace sidecar_lib;
public static class SwaggerSidecar
{
    public static void ConfigureSwagger(this IServiceCollection services, string apiName)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{apiName}", Version = "v1" });
            // Configure Swagger to use JWT authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[]{}
                }
            });

            var filePath = Path.Combine(System.AppContext.BaseDirectory, $"{apiName}.xml");
            c.IncludeXmlComments(filePath);
        });
    }
}