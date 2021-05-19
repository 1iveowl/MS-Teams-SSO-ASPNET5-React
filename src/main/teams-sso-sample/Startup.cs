using System;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using teams_sso_sample.Client;
using teams_sso_sample.Options;
using teams_sso_sample.Policies;

namespace teams_sso_sample
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
            // Using Options: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-5.0
            services.Configure<ApiAppOptions>(Configuration.GetSection(ApiAppOptions.ApiAppRegistration));
            services.Configure<MSGraphOptions>(Configuration.GetSection(MSGraphOptions.MSGraphSettings));
            services.Configure<ThrottlingOptions>(Configuration.GetSection(ThrottlingOptions.ThrottelingSettings));

            var registry = services
                .AddPollyPolicyRegistry(Configuration.GetSection(ThrottlingOptions.ThrottelingSettings).Get<ThrottlingOptions>());

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration, ApiAppOptions.ApiAppRegistration)
                .EnableTokenAcquisitionToCallDownstreamApi()
                  .AddDownstreamWebApi("MSGraphAPI", Configuration.GetSection(MSGraphOptions.MSGraphSettings))
                  .AddInMemoryTokenCaches();

            services.AddHttpClient<IGraphApiClientFactory, GraphApiClientFactory>()
                .ConfigureHttpClient((serviceprovider, httpClient) =>
                {
                    using var scope = serviceprovider.CreateScope();
                    var baseUrl = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<MSGraphOptions>>().Value.BaseUrl;
                    httpClient.BaseAddress = new Uri(baseUrl);

                });
                //.AddPolicyHandlerFromRegistry(PolicyRepository.Selector);

            services.AddScoped<IGraphRequestHandler, GraphRequestHandler>();

            // For serving SPA TypeScript client.
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "Frontend/Build";
            });

            services.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy
                options.FallbackPolicy = options.DefaultPolicy;
            });

            services.AddRazorPages()
                .AddMvcOptions(options => { })
                .AddMicrosoftIdentityUI();
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
                app.UseHttpsRedirection();
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Use Api controller pipeline if the path includes /api else use the React App pipeline.
            app.Map("/api", app => HandleApi(app, env));

            app.UseSpaStaticFiles();
            
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "Frontend";

                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("https://1iveowl-teams-react.eu.ngrok.io");
                    //spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });

        }
        private void HandleApi(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAuthentication();
            
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
