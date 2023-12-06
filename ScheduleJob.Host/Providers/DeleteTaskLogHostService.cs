using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using Quartz.Spi;
using Quartz;
using ScheduleJob.Host.QuartzJobs;
using OneForAll.Core.Extension;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Host.Models;

namespace ScheduleJob.Host.Providers
{
    /// <summary>
    /// 定时任务日志监控
    /// </summary>
    public class DeleteTaskLogHostService : IHostedService
    {
        private readonly AuthConfig _config;
        private readonly IJobFactory _jobFactory;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobTaskService _service;
        public IScheduler Scheduler { get; private set; }

        public DeleteTaskLogHostService(AuthConfig config, IJobFactory jobFactory, ISchedulerFactory schedulerFactory, IJobTaskService service)
        {
            _config = config;
            _jobFactory = jobFactory;
            _schedulerFactory = schedulerFactory;

            _service = service;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;

            var corn = "0 0 23 1/1 * ? ";
            var type = typeof(DeleteTaskLogJob);

            var job = JobBuilder
                .Create(type)
                .WithIdentity(type.FullName)
                .WithDescription(type.Name)
                .Build();

            var trigger = TriggerBuilder
                .Create()
                .WithIdentity(type.FullName.Append(".Trigger"))
                .WithCronSchedule(corn)
                .WithDescription(type.Name.Append(".Trigger"))
                .Build();

            await Scheduler.ScheduleJob(job, trigger, cancellationToken);
            await Scheduler.Start(cancellationToken);
            await _service.RegisterAsync(new JobTaskRegisterForm()
            {
                AppId = _config.ClientCode,
                AppSecret = _config.ClientSecret,
                GroupName = _config.ClientName,
                NodeName = "本地",
                Cron = corn,
                Name = type.Name,
                Remark = "删除任务日志，注册/心跳/上线/下线日志（1天）、运行日志（7天）、异常日志（15天），每天23点执行一次"
            });
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Scheduler?.Shutdown(cancellationToken);
            await _service.DownLineAsync(_config.ClientCode, typeof(DeleteTaskLogJob).Name);
        }
    }
}
