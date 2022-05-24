using System;
using System.Data;
using DeadCapTracker.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestEase;
using AutoMapper;
using DeadCapTracker.Services;
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
            services.AddSingleton(RestClient.For<IMflApi>("https://www49.myfantasyleague.com"));
            services.AddSingleton(RestClient.For<IGroupMeApi>("https://api.groupme.com"));
            services.AddSingleton(RestClient.For<IInsultApi>("https://evilinsult.com/"));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ILeagueService, LeagueService>();
            services.AddScoped<IGroupMeRequestService, GroupMeRequestRequestService>();
            services.AddScoped<IRumorService, RumorService>();
            services.AddScoped<IMflTranslationService, MflTranslationService>();
            services.AddScoped<IDataSetHelperService, DataSetHelperService>();
            //I dont know if i need this.
            // services.AddOptions();
            // services.Configure<DatabaseOptions>(Configuration.GetSection("DatabaseOptions"));
            //TODO : use config for value instead of hardcoding

            services.AddAutoMapper(typeof(Startup));
            services.AddHttpClient();
            
            //pull in connection string
            var databaseUrl =
                @"postgres://xhnlfkdajqdqbw:1579aa856e474268243f3b0c049dbf7395766298730f3407c78537851dcd9779@ec2-35-174-118-71.compute-1.amazonaws.com:5432/d1ea1gn980l2dr";//Environment.GetEnvironmentVariable("DATABASE_URL");
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
                    options.UseNpgsql(builder.ConnectionString);
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
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "swagger"); });
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}