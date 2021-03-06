﻿using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoteTakingApp.Core;
using NoteTakingApp.Core.Behaviours;
using NoteTakingApp.Core.Common;
using NoteTakingApp.Core.Extensions;
using NoteTakingApp.Core.Identity;
using NoteTakingApp.Core.Interfaces;
using NoteTakingApp.Infrastructure.Data;
using NoteTakingApp.Infrastructure.Extensions;
using System;
using System.Linq;

namespace NoteTakingApp.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder().Build();
            ProcessDbCommands(args, host);
            host.Run();
        }
        
        public static IWebHostBuilder CreateWebHostBuilder() =>
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>();

        private static void ProcessDbCommands(string[] args, IWebHost host)
        {
            var services = (IServiceScopeFactory)host.Services.GetService(typeof(IServiceScopeFactory));

            using (var scope = services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (args.Contains("ci"))
                    args = new string[4] { "dropdb", "migratedb", "seeddb", "stop" };

                if (args.Contains("dropdb"))
                    context.Database.EnsureDeleted();

                if (args.Contains("migratedb"))
                    context.Database.Migrate();

                if (args.Contains("seeddb"))
                {
                    context.Database.EnsureCreated();
                    SeedData.Seed(context);            
                }
                
                if (args.Contains("stop"))
                    Environment.Exit(0);
            }
        }        
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
            => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCustomMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddFluentValidation(cfg => { cfg.RegisterValidatorsFromAssemblyContaining<Startup>(); });

            services.AddSingleton<IConcurrentCommandGuard, ConcurrentCommandGuard>();
            services.AddSingleton<ICommandRegistry, CommandRegistry>();

            services
                .Configure<AuthenticationSettings>(options => Configuration.GetSection("Authentication").Bind(options))
                .AddDataStore(Configuration["Data:DefaultConnection:ConnectionString"], Configuration.GetValue<bool>("isTest"))
                .AddCustomSecurity(Configuration)
                .AddCustomSignalR(Configuration["SignalR:DefaultConnection:ConnectionString"], Configuration.GetValue<bool>("isTest"))
                .AddCustomSwagger()
                .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))
                .AddMediatR(typeof(Startup));
        }

        public void Configure(IApplicationBuilder app, IAppDbContext context)
        {
            app.UseResponseBuffering();

            if (Configuration.GetValue<bool>("isTest"))
                app.UseMiddleware<AutoAuthenticationMiddleware>();

            app.UseAuthentication()
                .UseTokenValidation()
                .UseCors(CorsDefaults.Policy)
                .UseMvc();

            if (Configuration.GetValue<bool>("isTest"))
            {
                app.UseSignalR(routes => routes.MapHub<IntegrationEventsHub>("/hub"));
            }
            else
            {
                app.UseAzureSignalR(routes => routes.MapHub<IntegrationEventsHub>("/hub"));
            }

            app.UseSwagger()
                .UseSwaggerUI(options
                =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Note Taking App API");
                    options.RoutePrefix = string.Empty;
                });            
        }
    }
}
