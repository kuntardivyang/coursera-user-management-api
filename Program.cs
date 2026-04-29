using Microsoft.OpenApi.Models;
using UserManagementAPI.Middleware;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Management API",
        Version = "v1",
        Description = "CRUD API for managing TechHive Solutions internal users (HR & IT)."
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Token",
        In = ParameterLocation.Header,
        Description = "Paste the API token (see appsettings.Development.json or the Auth:ApiToken config value)."
    };
    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<IUserService, InMemoryUserService>();

var app = builder.Build();

// Middleware pipeline ordering per the activity brief:
//
//   1. Error handling  (outermost — catches anything thrown below)
//   2. Authentication  (gates all protected routes)
//   3. Logging         (innermost — wraps the endpoint call)
//
// In production we'd typically put logging BEFORE auth so unauthenticated
// attempts are also audited; the framework's own auth logger covers that gap
// here. See MIDDLEWARE_NOTES.md for the trade-off discussion.
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TokenAuthenticationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();
