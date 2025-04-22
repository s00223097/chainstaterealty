using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using API.Data;
using API.Services;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            // Configure Database Connection
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

            // Configure Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Register BlockchainService
            builder.Services.AddSingleton<BlockchainService>();

            // Configure JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

            var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            // Add Google Authentication only if credentials are provided
            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            if (!string.IsNullOrEmpty(googleClientId))
            {
                authBuilder.AddGoogle(options =>
                {
                    var googleAuthSettings = builder.Configuration.GetSection("Authentication:Google");
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleAuthSettings["ClientSecret"] ?? "";
                    options.CallbackPath = "/api/auth/google-callback";
                });
            }

            // Add Microsoft Authentication only if credentials are provided
            var msClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
            if (!string.IsNullOrEmpty(msClientId))
            {
                authBuilder.AddMicrosoftAccount(options =>
                {
                    var msAuthSettings = builder.Configuration.GetSection("Authentication:Microsoft");
                    options.ClientId = msClientId;
                    options.ClientSecret = msAuthSettings["ClientSecret"] ?? "";
                    options.CallbackPath = "/api/auth/microsoft-callback";
                });
            }

            // Authorisation
            builder.Services.AddAuthorization();

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Controllers
            builder.Services.AddControllers();
            // from https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
