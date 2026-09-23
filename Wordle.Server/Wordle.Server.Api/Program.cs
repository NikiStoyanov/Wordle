using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wordle.Server.Core.Interfaces.Repositories;
using Wordle.Server.Core.Interfaces.Services;
using Wordle.Server.Core.Services;
using Wordle.Server.Infrastructure.Data;
using Wordle.Server.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddPolicy("AllowWordleClient", policy =>
        policy.WithOrigins("https://localhost:44444", "https://wordle-client.azurewebsites.net")
              .AllowAnyHeader()
              .AllowAnyMethod()
    )
);

builder.Services.AddDbContext<WordleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<WordleDbContext>();
        var dictionariesPath = Path.Combine(app.Environment.ContentRootPath, "Dictionaries");

        await DbSeeder.SeedWordsAsync(context, dictionariesPath, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while seeding the database!");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("AllowWordleClient");

app.UseAuthorization();

app.MapControllers();

app.Run();
