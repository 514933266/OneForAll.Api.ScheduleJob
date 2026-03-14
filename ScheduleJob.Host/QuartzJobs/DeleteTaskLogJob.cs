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
    public class DeleteTaskLogJob : IJob
    {
        private readonly AuthConfig _config;
        private readonly IScheduleJobService _service;
        private readonly IJobTaskLogRepository _repository;
        public DeleteTaskLogJob(
            AuthConfig config,
            IScheduleJobService service,
            IJobTaskLogManager logManager,
            IJobTaskLogRepository repository)
        {
            _config = config;
            _service = service;
            _repository = repository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            int deletedHeartbeats = 0, deletedRuns = 0, deletedErrors = 0;
            var utcToday = DateTime.UtcNow.Date;

            // 从 JobDataMap 中读取配置，未配置则使用默认值
            var logConfig = GetLogConfig(context);

            try
            {
                // 删除心跳/注册/上线/下线日志
                var cutoffHeartbeat = utcToday.AddDays(-logConfig.HeartbeatDays);
                var heartbeats = await _repository.GetListAsync(w => w.Type != JobTaskLogTypeEnum.Running && w.Type != JobTaskLogTypeEnum.Error && w.CreateTime < cutoffHeartbeat);

                if (heartbeats.Any())
                {
                    deletedHeartbeats = await _repository.BulkDeleteAsync(heartbeats.ToList());
                    await AddLogAsync($"删除{logConfig.HeartbeatDays}天前心跳类日志完成，共 {deletedHeartbeats} 条");
                }

                // 删除运行日志
                var cutoffRun = utcToday.AddDays(-logConfig.RunningDays);
                var runs = await _repository.GetListAsync(w => w.Type == JobTaskLogTypeEnum.Running && w.CreateTime < cutoffRun);

                if (runs.Any())
                {
                    deletedRuns = await _repository.BulkDeleteAsync(runs.ToList());
                    await AddLogAsync($"删除{logConfig.RunningDays}天前运行日志完成，共 {deletedRuns} 条");
                }

                // 删除错误日志
                var cutoffError = utcToday.AddDays(-logConfig.ErrorDays);
                var errors = await _repository.GetListAsync(w => w.Type == JobTaskLogTypeEnum.Error && w.CreateTime < cutoffError);

                if (errors.Any())
                {
                    deletedErrors = await _repository.BulkDeleteAsync(errors.ToList());
                    await AddLogAsync($"删除{logConfig.ErrorDays}天前错误日志完成，共 {deletedErrors} 条");
                }

                await AddLogAsync($"【日志清理完成】配置(心跳:{logConfig.HeartbeatDays}天/运行:{logConfig.RunningDays}天/错误:{logConfig.ErrorDays}天)，共删除：心跳类 {deletedHeartbeats} 条，运行日志 {deletedRuns} 条，错误日志 {deletedErrors} 条。");
            }
            catch (Exception ex)
            {
                await AddLogAsync($"【严重】日志清理任务执行失败：{ex.Message}\n{ex.StackTrace}", true);
            }
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
