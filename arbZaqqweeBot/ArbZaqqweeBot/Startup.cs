using ArbZaqqweeBot.Context;
using ArbZaqqweeBot.Helpers;
using ArbZaqqweeBot.Services;
using ArbZaqqweeBot.Services.Analyzer;
using ArbZaqqweeBot.Services.CryptoRequest;
using ArbZaqqweeBot.Services.CryptoRequest.Binance;
using ArbZaqqweeBot.Services.CryptoRequest.ByBit;
using ArbZaqqweeBot.Services.CryptoRequest.Huobi;
using ArbZaqqweeBot.Services.CryptoRequest.MEXC;
using ArbZaqqweeBot.Services.CryptoRequest.Kucoin;
using ArbZaqqweeBot.Services.CryptoRequest.OKX;
using ArbZaqqweeBot.Services.DuplicateDeleter;
using ArbZaqqweeBot.Services.TelegramBot;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace ArbZaqqweeBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(_ => true)
                        .AllowCredentials());
            });

            string connection = Configuration.GetConnectionString("DevConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connection));


            services.AddSingleton<IBotActions, BotActions>();

            services.AddHostedService<ExecuteAnalyzer>();
            services.AddScoped<IAnalyzerService, AnalyzerService>();

            services.AddHostedService<ExecuteDeleter>();
            services.AddScoped<IDeleter, Deleter>();

            services.AddHostedService<ExecuteBinance>();
            services.AddHostedService<ExecuteKucoin>();
            services.AddHostedService<ExecuteOKX>();
            services.AddHostedService<ExecuteHuobi>();
            services.AddHostedService<ExecuteBybit>();
            services.AddHostedService<ExecuteMexc>();

            services.AddScoped<IKucoinService, KucoinService>();
            services.AddScoped<IBinanceService, BinanceService>();
            services.AddScoped<IOKXService, OKXService>();
            services.AddScoped<IHuobiService, HuobiService>();
            services.AddScoped<IBybitService, BybitService>();
            services.AddScoped<IMexcService, MexcService>();

            services.AddIdentity<IdentityUser, IdentityRole>(opts =>
            {
                opts.Password.RequiredLength = 5;   // минимальная длина
                opts.Password.RequireNonAlphanumeric = false;   // требуются ли не алфавитно-цифровые символы
                opts.Password.RequireLowercase = false; // требуются ли символы в нижнем регистре
                opts.Password.RequireUppercase = false; // требуются ли символы в верхнем регистре
                opts.Password.RequireDigit = false; // требуются ли цифры
            }).AddEntityFrameworkStores<AppDbContext>();

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = AuthOptions.ISSUER,
                    ValidateAudience = true,
                    ValidAudience = AuthOptions.AUDIENCE,
                    ValidateLifetime = false,
                    IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                    ValidateIssuerSigningKey = true,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (string.IsNullOrEmpty(accessToken) == false)
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddControllers(
             options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "arbiZaq.WebApi", Version = "v1" });
                c.AddSecurityDefinition("Bearer",
                  new OpenApiSecurityScheme
                  {
                      Description = "JWT Authorization header using the Bearer scheme.",
                      Type = SecuritySchemeType.Http,
                      Scheme = "bearer"
                  });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement{
                    {
                    new OpenApiSecurityScheme{
                        Reference = new OpenApiReference{
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                        }
                        },new List<string>()
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "arbiZaq.WebApi v1"));
            }
            else
            {
                app.UseDefaultFiles();
                app.UseStaticFiles();
            }

            app.UseCors("CorsPolicy");
            //
            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}