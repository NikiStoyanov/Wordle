using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wordle.Server.Core.Interfaces.Repositories;
using Wordle.Server.Core.Interfaces.Services;
using Wordle.Server.Core.Services;
using Wordle.Server.Infrastructure.Data;
using Wordle.Server.Infrastructure.Repositories;
using Wordle.Server.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

builder.Services.AddScoped<IDailyChallengeRepository, DailyChallengeRepository>();
builder.Services.AddScoped<IUserDailyAttemptRepository, UserDailyAttemptRepository>();
builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDailyChallengeService, DailyChallengeService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<ITokenService, TokenService>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
