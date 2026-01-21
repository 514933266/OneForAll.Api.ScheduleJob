using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ScheduleJob.Host
{
    public class Program
    {
        public static void Main(string[] args) =>
            CreateWebHostBuilder(args).Build().Run();

        // 作为 IIS站点 运行，启用这段代码
        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                webBuilder.UseIISIntegration();
                webBuilder.UseStartup<Startup>();
            });

        // 作为 Windows Service 运行，启用这段代码
        //public static IHostBuilder CreateWebHostBuilder(string[] args)
        //{
        //    var basePath = AppContext.BaseDirectory;

        //    var configuration = new ConfigurationBuilder()
        //        .SetBasePath(basePath)
        //        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        //        .AddEnvironmentVariables()
        //        .AddCommandLine(args)
        //        .Build();

        //    return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
        //        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        //        .ConfigureWebHostDefaults(webBuilder =>
        //        {
        //            webBuilder.UseContentRoot(basePath);
        //            webBuilder.UseStartup<Startup>();
        //        })
        //        .UseWindowsService()
        //        .ConfigureWebHost(hostBuilder =>
        //        {
        //            hostBuilder.UseUrls(configuration.GetSection("Urls").Get<string[]>());
        //        });
        //}
    }
}