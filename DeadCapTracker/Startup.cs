using System;
using DeadCapTracker.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestEase;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;


namespace DeadCapTracker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(c =>
            {
                c.AddPolicy("AllowSpecificOrigin",
                    options => options.WithOrigins("https://*.dcsg.com", "https://capn-crunch-gm-bot.herokuapp.com", "https://stanfan.herokuapp.com", "http://capn-crunch-gm-bot.herokuapp.com", "http://stanfan.herokuapp.com",
                            "http://localhost:3000", "https://localhost:3000", "https://capn-crunch.herokuapp.com", "http://capn-crunch.herokuapp.com")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .WithExposedHeaders("Access-Control-Allow-Origin"));
            });

            services.AddControllers();
            services.AddSwaggerGen();
            services.AddSingleton(RestClient.For<IGlobalMflApi>("https://api.myfantasyleague.com"));
            services.AddSingleton(RestClient.For<IMflApi>("https://www64.myfantasyleague.com"));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ILeagueService, LeagueService>();
            //I dont know if i need this.
            // services.AddOptions();
            // services.Configure<DatabaseOptions>(Configuration.GetSection("DatabaseOptions"));
            //TODO : use config for value instead of hardcoding

            services.AddAutoMapper(typeof(Startup));
            
            //pull in connection string
             var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
             var databaseUri = new Uri(databaseUrl);
             var userInfo = databaseUri.UserInfo.Split(':');
            
             var builder = new NpgsqlConnectionStringBuilder
             {
                 Host = databaseUri.Host,
                 Port = databaseUri.Port,
                 Username = userInfo[0],
                 Password = userInfo[1],
                 Database = databaseUri.LocalPath.TrimStart('/'),
                 SslMode = SslMode.Require, 
                 TrustServerCertificate = true
            };


            services.AddDbContext<DeadCapTrackerContext>(
                options =>
                {
                    options.UseNpgsql(builder.ToString());
                    // options.UseNpgsql((string) Configuration.GetValue(typeof(string),
                    //     "DatabaseOptions:ConnectionString"));
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors("AllowSpecificOrigin");

            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dead Cap Tracker"); });
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}