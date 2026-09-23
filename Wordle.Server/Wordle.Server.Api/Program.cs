using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wordle.Server.Infrastructure.Data;

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
