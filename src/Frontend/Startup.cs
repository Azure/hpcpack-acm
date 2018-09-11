namespace Microsoft.HpcAcm.Frontend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Bazinga.AspNetCore.Authentication.Basic;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Serilog;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public ILogger Logger { get; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddAuthentication("Basic")
                .AddBasicAuthentication<BasicCredentialVerifier>("Basic","Basic", _ => { }, ServiceLifetime.Singleton);
            services.AddMvc().AddJsonOptions(options => {
                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
                builder.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .WithExposedHeaders("Location")
            );

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
