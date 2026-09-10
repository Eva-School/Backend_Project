using GradeManagementSystem.Api.Data;
using GradeManagementSystem.Core.Entities.Identity;
using GradeManagementSystem.Core.Interfaces;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Services.Mapping;
using GradeManagementSystem.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
            var postgresConnStr = PostgresConnectionParser.Parse(rawConnStr);

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

            builder.Services.AddMemoryCache();

            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAdminAccountService, AdminAccountService>();
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

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                        var db = context.HttpContext.RequestServices.GetRequiredService<GradeDbContext>();

                        var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!int.TryParse(userIdClaim, out var userId))
                        {
                            context.Fail("Invalid or missing user identifier in token.");
                            return;
                        }

                        var cacheKey = $"auth_user_{userId}";
                        if (!cache.TryGetValue(cacheKey, out (bool IsActive, string SecurityStamp, string RoleName) userState))
                        {
                            var userFromDb = await db.Users
                                .AsNoTracking()
                                .Include(u => u.Role)
                                .Where(u => u.UserId == userId)
                                .Select(u => new
                                {
                                    u.IsActive,
                                    SecurityStamp = u.SecurityStamp ?? string.Empty,
                                    RoleName = u.Role != null ? u.Role.RoleName : "Student"
                                })
                                .FirstOrDefaultAsync();

                            if (userFromDb == null)
                            {
                                context.Fail("User account no longer exists.");
                                return;
                            }

                            userState = (userFromDb.IsActive, userFromDb.SecurityStamp, userFromDb.RoleName);
                            cache.Set(cacheKey, userState, TimeSpan.FromSeconds(30));
                        }

                        // 1. Account must be active
                        if (!userState.IsActive)
                        {
                            context.Fail("User account is disabled or inactive.");
                            return;
                        }

                        // 2. Validate Security Stamp (rejects tokens after password reset or session revocation)
                        var tokenStamp = context.Principal?.FindFirst("security_stamp")?.Value;
                        if (!string.IsNullOrEmpty(userState.SecurityStamp) &&
                            !string.IsNullOrEmpty(tokenStamp) &&
                            !string.Equals(tokenStamp, userState.SecurityStamp, StringComparison.Ordinal))
                        {
                            context.Fail("Security stamp mismatch. Active session has been revoked.");
                            return;
                        }

                        // 3. Validate Role consistency (rejects tokens with stale role claims after role change)
                        var tokenRole = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                        if (!string.IsNullOrEmpty(tokenRole) &&
                            !string.Equals(tokenRole, userState.RoleName, StringComparison.OrdinalIgnoreCase))
                        {
                            context.Fail("User role has been modified. Please sign in again.");
                            return;
                        }
                    }
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
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                // Pre-creation Data API lockout: Ensure tables in public schema are NEVER exposed to PostgREST anon/authenticated roles
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        DO $$
                        BEGIN
                            IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                                REVOKE USAGE ON SCHEMA public FROM anon, authenticated;
                                REVOKE ALL ON ALL TABLES IN SCHEMA public FROM anon, authenticated, PUBLIC;
                                REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM anon, authenticated, PUBLIC;
                                REVOKE ALL ON ALL ROUTINES IN SCHEMA public FROM anon, authenticated, PUBLIC;
                                ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON TABLES FROM anon, authenticated, PUBLIC;
                                ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON SEQUENCES FROM anon, authenticated, PUBLIC;
                                ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON ROUTINES FROM anon, authenticated, PUBLIC;
                            END IF;
                        END $$;
                    ");
                    logger.LogInformation("Supabase Data API lockout successfully verified/applied.");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not apply Supabase Data API lockout SQL (non-fatal if permissions restricted).");
                }

                db.Database.Migrate();
            }

            // AdminSeed always runs (all environments) to guarantee at least one
            // Admin account exists. Credentials come from env vars:
            //   ADMIN_USERNAME  (default: admin)
            //   ADMIN_PASSWORD  (default: Admin@123456!)
            //   ADMIN_EMAIL     (default: admin@grading-system.local)
            AdminSeed.SeedAsync(app.Services).GetAwaiter().GetResult();

            // The test accounts and domain seeders are strictly guarded for local development / staging.
            // They will NEVER execute in Production.
            var isProduction = app.Environment.IsProduction() ||
                string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase);

            var runSeed = Environment.GetEnvironmentVariable("RUN_SEED");
            var shouldRunSeed = !isProduction && (string.Equals(runSeed, "true", StringComparison.OrdinalIgnoreCase) ||
                (app.Environment.IsDevelopment() && !string.Equals(runSeed, "false", StringComparison.OrdinalIgnoreCase)));
            if (shouldRunSeed)
            {
                LocalTestAccountsSeed.SeedAsync(app.Services).GetAwaiter().GetResult();
                ViceGradesSeed.SeedAsync(app.Services).GetAwaiter().GetResult();
            }

            app.MapControllers();

            app.Run();
        }
    }
}
