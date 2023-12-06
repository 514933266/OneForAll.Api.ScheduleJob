using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Autofac;
using Autofac.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OneForAll.File;
using OneForAll.EFCore;
using OneForAll.Core.Extension;
using ScheduleJob.Host.Models;
using ScheduleJob.Host.Filters;
using ScheduleJob.Public.Models;
using ScheduleJob.HttpService.Models;
using OneForAll.Core.Upload;
using ScheduleJob.HttpService;
using Quartz;
using ScheduleJob.Host.Providers;
using Quartz.Spi;
using ScheduleJob.Domain;
using Quartz.Impl;
using NPOI.SS.Formula.Functions;
using OneForAll.Core.Utility;
using ScheduleJob.Host.QuartzJobs;

namespace ScheduleJob.Host
{
    public class Startup
    {
        private const string CORS = "Cors";
        private const string AUTH = "Auth";
        private const string QUARTZ = "Quartz";

        private const string HTTP_SERVICE = "ScheduleJob.HttpService";
        private const string HTTP_SERVICE_KEY = "HttpService";

        private const string BASE_HOST = "ScheduleJob.Host";
        private const string BASE_DOMAIN = "ScheduleJob.Domain";
        private const string BASE_APPLICATION = "ScheduleJob.Application";
        private const string BASE_REPOSITORY = "ScheduleJob.Repository";

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            #region Cors

            var corsConfig = new CorsConfig();
            Configuration.GetSection(CORS).Bind(corsConfig);
            services.AddCors(option => option.AddPolicy(CORS, policy => policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                ));

            #endregion

            #region Mvc

            services.AddControllers(options =>
            {
                options.EnableEndpointRouting = false;
                options.Filters.Add<AuthorizationFilter>();
                options.Filters.Add<ApiModelStateFilter>();
                options.Filters.Add<ExceptionFilter>();

            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });
            services.AddSingleton<IUploader, Uploader>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            #endregion

            #region Http数据服务

            var serviceConfig = new HttpServiceConfig();
            Configuration.GetSection(HTTP_SERVICE_KEY).Bind(serviceConfig);
            var props = OneForAll.Core.Utility.ReflectionHelper.GetPropertys(serviceConfig);
            props.ForEach(e =>
            {
                services.AddHttpClient(e.Name, c =>
                {
                    c.BaseAddress = new Uri(e.GetValue(serviceConfig).ToString());
                    c.DefaultRequestHeaders.Add("ClientId", ClientClaimType.Id);
                });
            });
            services.AddSingleton<HttpServiceConfig>();
            #endregion

            #region IdentityServer4

            var authConfig = new AuthConfig();
            Configuration.GetSection(AUTH).Bind(authConfig);
            services.AddAuthentication(authConfig.Type)
            .AddIdentityServerAuthentication(options =>
            {
                options.Authority = authConfig.Url;
                options.RequireHttpsMetadata = false;
            });
            services.AddSingleton(authConfig);

            #endregion

            #region AutoMapper

            services.AddAutoMapper(config =>
            {
                config.AllowNullDestinationValues = false;
            }, Assembly.Load(BASE_HOST));

            #endregion

            #region EFCore

            services.AddDbContext<OneForAll_JobContext>(options =>
                options.UseSqlServer(Configuration["ConnectionStrings:Default"]));
            services.AddScoped<ITenantProvider, TenantProvider>();

            #endregion

            #region Quartz

            var quartzConfig = new QuartzScheduleJobConfig();
            Configuration.GetSection(QUARTZ).Bind(quartzConfig);
            services.AddSingleton(quartzConfig);
            services.AddSingleton<IJobFactory, ScheduleJobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            
            // 注册自定义执行的任务
            if(quartzConfig.Mode == "Center")
            {
                // 定时任务状态监控
                services.AddHostedService<MonitorTaskStatusHostService>();
                services.AddSingleton<MonitorTaskStatusJob>();
                // 定时任务日志监控
                services.AddHostedService<DeleteTaskLogHostService>();
                services.AddSingleton<DeleteTaskLogJob>();
            }
            else if (quartzConfig.Mode == "Job")
            {
                services.AddHostedService<QuartzJobHostService>();
                var jobNamespace = BASE_HOST.Append(".QuartzJobs");
                quartzConfig.ScheduleJobs.ForEach(e =>
                {
                    var typeName = jobNamespace + "." + e.TypeName;
                    var jobType = Assembly.Load(BASE_HOST).GetType(typeName);
                    if (jobType != null)
                    {
                        e.JobType = jobType;
                        services.AddSingleton(e.JobType);
                    }
                });
            }
            #endregion
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Http数据服务
            builder.RegisterAssemblyTypes(Assembly.Load(HTTP_SERVICE))
               .Where(t => t.Name.EndsWith("Service"))
               .AsImplementedInterfaces();

            // 基础
            builder.RegisterGeneric(typeof(Repository<>))
                .As(typeof(IEFCoreRepository<>));

            builder.RegisterAssemblyTypes(Assembly.Load(BASE_APPLICATION))
                .Where(t => t.Name.EndsWith("Service"))
                .AsImplementedInterfaces();

            builder.RegisterAssemblyTypes(Assembly.Load(BASE_DOMAIN))
                .Where(t => t.Name.EndsWith("Manager"))
                .AsImplementedInterfaces();

            builder.RegisterType(typeof(OneForAll_JobContext)).Named<DbContext>("OneForAll_JobContext");
            builder.RegisterAssemblyTypes(Assembly.Load(BASE_REPOSITORY))
               .Where(t => t.Name.EndsWith("Repository"))
               .WithParameter(ResolvedParameter.ForNamed<DbContext>("OneForAll_JobContext"))
               .AsImplementedInterfaces();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            DirectoryHelper.Create(Path.Combine(Directory.GetCurrentDirectory(), @"upload"));
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), @"upload")),
                RequestPath = new PathString("/resources"),
                OnPrepareResponse = (c) =>
                {
                    c.Context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
            });
            app.UseDefaultFiles();

            app.UseRouting();

            app.UseCors(CORS);

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseMiddleware<ApiLogMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
