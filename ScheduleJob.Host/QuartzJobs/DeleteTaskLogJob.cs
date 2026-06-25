using OneForAll.Core.Extension;
using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Host.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务日志清理
    /// </summary>
    [DisallowConcurrentExecution]
    public class DeleteTaskLogJob : BaseLockJob
    {
        private readonly AuthConfig _config;
        private readonly IScheduleJobService _service;
        private readonly IJobTaskLogRepository _repository;
        public DeleteTaskLogJob(
            AuthConfig config,
            IScheduleJobService service,
            IJobRunningLockRepository lockRepository,
            IJobLockHolderRepository holderRepository,
            IJobTaskLogManager logManager,
            IJobTaskLogRepository repository)
            : base(config, service, lockRepository, holderRepository)
        {
            _config = config;
            _service = service;
            _repository = repository;
        }

        protected override async Task ExecuteInternalAsync(IJobExecutionContext context)
        {
            int deletedHeartbeats = 0, deletedRuns = 0, deletedErrors = 0;
            var utcToday = DateTime.UtcNow.Date;

            // 从 JobDataMap 中读取配置，未配置则使用默认值
            var logConfig = GetLogConfig(context);

            // 删除心跳/注册/上线/下线日志（分批处理）
            var cutoffHeartbeat = utcToday.AddDays(-logConfig.HeartbeatDays);
            int batchSize = logConfig.BatchSize > 0 ? logConfig.BatchSize : 500; // 防止配置为0或负数

            while (true)
            {
                var heartbeats = await _repository.GetListWithTakeAsync(
                    w => w.Type != JobTaskLogTypeEnum.Running &&
                         w.Type != JobTaskLogTypeEnum.Error &&
                         w.CreateTime < cutoffHeartbeat,
                    batchSize);

                if (!heartbeats.Any())
                    break;

                var count = await _repository.BulkDeleteAsync(heartbeats.ToList());
                deletedHeartbeats += count;
                await AddLogAsync($"分批删除{logConfig.HeartbeatDays}天前心跳类日志，本批 {count} 条，累计 {deletedHeartbeats} 条");
            }

            // 删除运行日志（分批处理）
            var cutoffRun = utcToday.AddDays(-logConfig.RunningDays);

            while (true)
            {
                var runs = await _repository.GetListWithTakeAsync(
                    w => w.Type == JobTaskLogTypeEnum.Running && w.CreateTime < cutoffRun,
                    batchSize);

                if (!runs.Any())
                    break;

                var count = await _repository.BulkDeleteAsync(runs.ToList());
                deletedRuns += count;
                await AddLogAsync($"分批删除{logConfig.RunningDays}天前运行日志，本批 {count} 条，累计 {deletedRuns} 条");
            }

            // 删除错误日志（分批处理）
            var cutoffError = utcToday.AddDays(-logConfig.ErrorDays);

            while (true)
            {
                var errors = await _repository.GetListWithTakeAsync(
                    w => w.Type == JobTaskLogTypeEnum.Error && w.CreateTime < cutoffError,
                    batchSize);

                if (!errors.Any())
                    break;

                var count = await _repository.BulkDeleteAsync(errors.ToList());
                deletedErrors += count;
                await AddLogAsync($"分批删除{logConfig.ErrorDays}天前错误日志，本批 {count} 条，累计 {deletedErrors} 条");
            }

            await AddLogAsync($"【日志清理完成】配置(心跳:{logConfig.HeartbeatDays}天/运行:{logConfig.RunningDays}天/错误:{logConfig.ErrorDays}天/批次:{batchSize}条)，共删除：心跳类 {deletedHeartbeats} 条，运行日志 {deletedRuns} 条，错误日志 {deletedErrors} 条。");
        }

        /// <summary>
        /// 从 JobDataMap 读取日志清理配置
        /// </summary>
        private DeleteTaskLogConfig GetLogConfig(IJobExecutionContext context)
        {
            var dataJson = context.JobDetail.JobDataMap.GetString("Data");
            if (!string.IsNullOrWhiteSpace(dataJson))
            {
                var config = dataJson.FromJson<DeleteTaskLogConfig>();
                if (config != null)
                    return config;
            }
            return new DeleteTaskLogConfig();
        }

        // 记录日志
        private async Task AddLogAsync(string log, bool isException = false)
        {
            await _service.AddLogAsync(_config.ClientCode, typeof(DeleteTaskLogJob).Name, log, isException);
        }
    }
}
