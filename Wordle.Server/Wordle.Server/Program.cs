using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddPolicy("AllowWordleClient", policy =>
        policy.WithOrigins("https://localhost:44444", "https://wordle-client.azurewebsites.net")
              .AllowAnyHeader()
              .AllowAnyMethod()
    )
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
