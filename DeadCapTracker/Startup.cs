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
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

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
                    options => options.WithOrigins(
                            "http://localhost:3000", 
                            "https://localhost:3000",
                            "https://localhost:50850",
                            "https://www.stanfan.net")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .WithExposedHeaders("Access-Control-Allow-Origin"));
            });
            var options = new ApplicationInsightsServiceOptions { 
                ConnectionString = @"InstrumentationKey=5e884814-f8b4-4d5f-b41c-30d067b12981;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/"
            };
            services.AddLogging();
            services.AddApplicationInsightsTelemetry(options);
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddSingleton(RestClient.For<IFreeAgencyAuctionAPI>("https://contract-auction-api.azurewebsites.net/"));
            services.AddSingleton(RestClient.For<IGlobalMflApi>("https://api.myfantasyleague.com"));
            services.AddSingleton(RestClient.For<IMflApi>("https://www49.myfantasyleague.com"));
            services.AddSingleton(RestClient.For<IGroupMeApi>("https://api.groupme.com"));
            services.AddSingleton(RestClient.For<IInsultApi>("https://evilinsult.com/"));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ILeagueService, LeagueService>();
            services.AddScoped<IGroupMeRequestService, GroupMeRequestService>();
            services.AddScoped<IRumorService, RumorService>();
            services.AddScoped<IMflTranslationService, MflTranslationService>();
            services.AddScoped<IDataSetHelperService, DataSetHelperService>();
            services.AddScoped<IGroupMePostRepo, GroupMePostRepo>();
            services.AddScoped<IGmFreeAgencyService, GmFreeAgencyService>();
            //TODO : use config for value instead of hardcoding
            services.AddAutoMapper(typeof(Program).Assembly);
            services.AddHttpClient();
            


            services.AddDbContext<DeadCapTrackerContext>(
                options =>
                {
                    options.UseSqlServer(Configuration.GetConnectionString("capn-sql-db"));
                    options.UseLazyLoadingProxies();
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