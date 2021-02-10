using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DeadCapTracker.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestEase;
using AutoMapper;
using DeadCapTracker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Steeltoe.CloudFoundry.Connector.PostgreSql.EFCore;


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
                    options => options.WithOrigins("https://*.dcsg.com", "https://capn-crunch-gm-bot.herokuapp.com", "https://stanfan.herokuapp.com",
                            "http://localhost:3000", "https://localhost:3000")
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
                Database = databaseUri.LocalPath.TrimStart('/')
            };


            services.AddDbContext<DeadCapTrackerContext>(
                options =>
                {
                    options.UseNpgsql(builder.ToString());
                    //options.EnableRetryOnFailure();
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