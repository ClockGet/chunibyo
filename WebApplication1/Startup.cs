using Karambolo.Extensions.Logging.File;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using WebApplication1.DAL;
using WebApplication1.Filters;
using WebApplication1.Services;

namespace WebApplication1
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(/*
                cookie验证
                options=> 
            {
                var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }*/);
            services.AddScoped<LoginFilter>();
            services.AddScoped<ApiExceptionFilterAttribute>();
            var mailConfig = new MailConfig();
            Configuration.GetSection("Email").Bind(mailConfig);
            services.AddSingleton(mailConfig);
            services.AddSingleton<EmailService>();
            services.AddSingleton<TokenService>();
            services.AddEntityFrameworkMySql().AddDbContext<MySqlContext>(x => x.UseMySql(Configuration.GetConnectionString("MySqlConnection")));
            //services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            //    .AddCookie(options => 
            //    {
            //        options.Cookie.HttpOnly = true;
            //        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            //        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            //        options.Events = new CookieAuthenticationEvents
            //        {
            //            OnValidatePrincipal = LoginValidator.ValidateAsync
            //        };
            //    });
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = ".Chunibyo.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
            });
            var tempPath = Path.GetTempPath();
            var logPath = Path.Combine(tempPath, "Logs");
            var context = new FileLoggerContext(new PhysicalFileProvider(tempPath),"fallback.log");
            services.AddLogging(b => b.AddFile(context));
            var cb = new ConfigurationBuilder();
            var configData = new Dictionary<string, string>
            {
                [$"{nameof(FileLoggerOptions.BasePath)}"] = "Logs",
                [$"{nameof(FileLoggerOptions.EnsureBasePath)}"] = "true",
                [$"{nameof(FileLoggerOptions.FileEncodingName)}"] = "UTF-8",
                [$"{nameof(FileLoggerOptions.MaxQueueSize)}"] = "100",
                [$"{nameof(FileLoggerOptions.DateFormat)}"] = "yyyyMMdd",
                [$"{ConfigurationFileLoggerSettings.LogLevelSectionName}:{FileLoggerSettingsBase.DefaultCategoryName}"] = "Information"
            };
            cb.AddInMemoryCollection(configData);
            var config = cb.Build();
            services.Configure<FileLoggerOptions>(config);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseSession();

            var routeBuilder = new RouteBuilder(app);

            app.UseMvc(routes => 
            {
                Route.Configure(routes);
            });

            app.UseRouter(routeBuilder.Build());
            //app.UseAuthentication();
        }
    }
}
