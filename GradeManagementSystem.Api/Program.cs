using GradeManagementSystem.Api.Data;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Services.Mapping;
using GradeManagementSystem.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GradeManagementSystem.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Disable FileSystemWatcher reloadOnChange on Linux containers to avoid inotify instance limits on shared hosts
            foreach (var source in builder.Configuration.Sources.OfType<Microsoft.Extensions.Configuration.FileConfigurationSource>())
            {
                source.ReloadOnChange = false;
            }

            var rawConnStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
            var postgresConnStr = ParseConnectionString(rawConnStr);

            builder.Services.AddDbContext<GradeDbContext>(options =>
                options.UseNpgsql(postgresConnStr));

            builder.Services.AddControllers();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<ISubjectService, SubjectService>();
            builder.Services.AddScoped<IClassService, ClassService>();
            builder.Services.AddScoped<ITeacherAssignmentService, TeacherAssignmentService>();
            builder.Services.AddScoped<ITeacherDashboardService, TeacherDashboardService>();
            builder.Services.AddScoped<IStudentDashboardService, StudentDashboardService>();
            builder.Services.AddScoped<IViceDashboardService, ViceDashboardService>();
            builder.Services.AddScoped<IViceStudentService, ViceStudentService>();
            builder.Services.AddScoped<IViceQuarterGradesService, ViceQuarterGradesService>();
            builder.Services.AddScoped<IViceFinalGradesService, ViceFinalGradesService>();
            builder.Services.AddScoped<IAdminFinalGradesService, AdminFinalGradesService>();
            builder.Services.AddAutoMapper(
                _ => { },
                typeof(AuthMappingProfile).Assembly);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Grade Management System API", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter JWT token"
                });
                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            // Identity Configuration
            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<GradeDbContext>()
            .AddDefaultTokenProviders();

            // JWT Authentication Configuration
            var jwtSigningKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is required.");
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
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
                };
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // TLS is terminated at the edge (Render, Azure Front Door, etc.).
            // Only redirect to HTTPS when running locally so we don't create
            // redirect loops inside the container.
            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            var applyMigrations = app.Environment.IsDevelopment() ||
                string.Equals(Environment.GetEnvironmentVariable("APPLY_MIGRATIONS"), "true", StringComparison.OrdinalIgnoreCase);
            if (applyMigrations)
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GradeDbContext>();
                db.Database.Migrate();
            }

            // AdminSeed always runs (all environments) to guarantee at least one
            // Admin account exists. Credentials come from env vars:
            //   ADMIN_USERNAME  (default: admin)
            //   ADMIN_PASSWORD  (default: Admin@123456!)
            //   ADMIN_EMAIL     (default: admin@grading-system.local)
            AdminSeed.SeedAsync(app.Services).GetAwaiter().GetResult();

            // The remaining seeds are for local development / staging only.
            var runSeed = Environment.GetEnvironmentVariable("RUN_SEED");
            var shouldRunSeed = string.Equals(runSeed, "true", StringComparison.OrdinalIgnoreCase) ||
                (app.Environment.IsDevelopment() && !string.Equals(runSeed, "false", StringComparison.OrdinalIgnoreCase));
            if (shouldRunSeed)
            {
                LocalTestAccountsSeed.SeedAsync(app.Services).GetAwaiter().GetResult();
                StudentDashboardSeed.SeedAsync(app.Services).GetAwaiter().GetResult();
                TeacherDashboardSeed.SeedAsync(app.Services).GetAwaiter().GetResult();
                ViceGradesSeed.SeedAsync(app.Services).GetAwaiter().GetResult();
            }

            app.MapControllers();

            app.Run();
        }

        private static string ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
            {
                var uri = new Uri(connectionString);
                var userInfo = uri.UserInfo.Split(':');
                var user = userInfo[0];
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');

                return $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
            }

            return connectionString;
        }
    }
}
