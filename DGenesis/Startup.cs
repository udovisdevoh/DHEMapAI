using DGenesis.Services;
using DGenesis.Services.Deformations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DGenesis
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddSingleton<GameAssetService>();
            services.AddSingleton<AssetThemeService>();
            services.AddSingleton<AssetFunctionService>();
            services.AddScoped<DGenesisRandomGeneratorService>();
            services.AddScoped<DGraphGeneratorService>();
            services.AddScoped<DGraphLayoutService>();
            services.AddScoped<DGraphUntanglerService>();
            services.AddScoped<DGraphRoleAssignmentService>();
            services.AddScoped<DGraphPathfindingService>();
            services.AddScoped<DGraphChaosService>();
            services.AddScoped<DGraphFinalizeService>();
            services.AddScoped<DShapeGeneratorService>();
            services.AddScoped<DShapeDeformationService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}