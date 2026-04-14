using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BankMaskingAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            string jwtKey = builder.Configuration["JwtSettings:Key"] ?? "BANK_MASKING_DEMO_KEY_CHANGE_ME_2026";
            string jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "BankMaskingAPI";
            string jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "BankMaskingClients";

            builder.Services.AddSingleton<SecurityModules>();
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddCors();
            builder.Services.AddControllers();
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });
            builder.Services.AddAuthorization();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Ensure no sensitive field stays in cleartext at rest.
            try
            {
                var security = app.Services.GetRequiredService<SecurityModules>();
                security.EnsureSensitiveDataEncryptedAtRest();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] Startup encryption sync skipped: " + ex.Message);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseCors(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
