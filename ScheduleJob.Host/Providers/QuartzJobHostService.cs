using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using OneForAll.Core.Extension;
using Org.BouncyCastle.Asn1.Cms;
using Quartz;
using Quartz.Spi;
using ScheduleJob.Host.Models;
using ScheduleJob.HttpService.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScheduleJob.Host.Providers
{
    /// <summary>
    /// 定时任务启动服务
    /// </summary>
    public class QuartzJobHostService : IHostedService
    {
        private readonly QuartzScheduleJobConfig _config;
        private readonly IJobFactory _jobFactory;
        private readonly ISchedulerFactory _schedulerFactory;

        private readonly IScheduleJobHttpService _jobHttpService;

        public IScheduler Scheduler { get; private set; }
        public QuartzJobHostService(
            QuartzScheduleJobConfig config,
            IJobFactory jobFactory,
            ISchedulerFactory schedulerFactory,
            IScheduleJobHttpService jobHttpService)
        {
            _config = config;
            _jobFactory = jobFactory;
            _schedulerFactory = schedulerFactory;
            _jobHttpService = jobHttpService;
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;
            foreach (var jobSchedule in _config.ScheduleJobs)
            {
                if (jobSchedule.JobType == null)
                    return;
                // 调度中心不需要请求判断任务是否允许注册
                //var msg = await _jobHttpService.RegisterAsync(new HttpService.Models.JobRegisterRequest()
                //{
                //    AppId = _config.AppId,
                //    AppSecret = _config.AppSecret,
                //    GroupName = _config.GroupName,
                //    NodeName = _config.NodeName,
                //    Cron = jobSchedule.Corn,
                //    Name = jobSchedule.TypeName,
                //    Remark = jobSchedule.Remark
                //});

                // 调度中心允许注册才启动服务
                //if (msg.Status)
                //{
                var job = CreateJob(jobSchedule);
                var trigger = CreateTrigger(jobSchedule);
                await Scheduler.ScheduleJob(job, trigger, cancellationToken);
                //}
            }
            await Scheduler.Start(cancellationToken);
        }

        /// <summary>
        /// 暂停服务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Scheduler?.Shutdown(cancellationToken);
            foreach (var jobSchedule in _config.ScheduleJobs)
            {
                await _jobHttpService.DownLineAsync(_config.AppId, jobSchedule.TypeName);
            }
        }

        // 创建任务
        private IJobDetail CreateJob(QuartzScheduleJob schedule)
        {
            return JobBuilder
                .Create(schedule.JobType)
                .WithIdentity(schedule.JobType.FullName)
                .WithDescription(schedule.JobType.Name)
                .Build();
        }

        // 创建触发器
        private ITrigger CreateTrigger(QuartzScheduleJob schedule)
        {
            return TriggerBuilder
                .Create()
                .WithIdentity(schedule.JobType.FullName.Append(".Trigger"))
                .WithCronSchedule(schedule.Corn)
                .WithDescription(schedule.JobType.Name.Append(".Trigger"))
                .Build();
        }
    }
}
