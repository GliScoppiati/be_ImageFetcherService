using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using ImageFetcherService.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🔐 JWT settings
var jwtSection = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.ASCII.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false;
        opt.SaveToken = true;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization();

// ✅ Servizi
builder.Services.AddHttpClient();
builder.Services.AddScoped<PexelsClient>();
builder.Services.AddScoped<PixabayClient>();
builder.Services.AddScoped<UnsplashClient>();

// ✅ Controller + Swagger + JWT Bearer support
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Image Fetcher API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ✅ CORS Policy (permissiva per sviluppo, da restringere in produzione)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Recupera il logger di Program
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// ✅ Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll"); // ⬅️ Prima dell'autenticazione
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 🟢 Log avvio del service
var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "non configurato";
logger.LogInformation(
    "[ImageFetcherService] 📷 Service avviato su: {Urls}",
    urls
);

app.Run();
