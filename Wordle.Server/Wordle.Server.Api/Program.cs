using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wordle.Server.Core.Interfaces.Repositories;
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

builder.Services.AddControllers();
builder.Services.AddOpenApi();


var app = builder.Build();

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
