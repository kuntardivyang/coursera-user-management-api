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
});

builder.Services.AddSingleton<IUserService, InMemoryUserService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// Order matters: ExceptionHandler must run first so it can catch downstream throws.
app.UseExceptionHandler();
app.UseStatusCodePages();

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
    // Avoid HTTPS redirect in Development so Postman/curl over plain http://localhost:5080 just works.
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
