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
    /// 定时任务日志监控
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
            var name = typeof(DeleteTaskLogJob).Name;
            int deletedHeartbeats = 0, deletedRuns = 0, deletedErrors = 0;
            var utcToday = DateTime.UtcNow.Date;

            try
            {
                // 删除1天前的非运行/非错误日志（注册、上线等）
                var cutoffHeartbeat = utcToday;
                var heartbeats = await _repository.GetListAsync(w => w.Type != JobTaskLogTypeEnum.Running && w.Type != JobTaskLogTypeEnum.Error && w.CreateTime < cutoffHeartbeat);

                if (heartbeats.Any())
                {
                    deletedHeartbeats = await _repository.BulkDeleteAsync(heartbeats.ToList());
                    await AddLogAsync($"删除非运行/非错误日志完成，共 {deletedHeartbeats} 条");
                }

                // 删除7天前的运行日志
                var cutoffRun = utcToday.AddDays(-7);
                var runs = await _repository.GetListAsync(w => w.Type == JobTaskLogTypeEnum.Running && w.CreateTime < cutoffRun);

                if (runs.Any())
                {
                    deletedRuns = await _repository.BulkDeleteAsync(runs.ToList());
                    await AddLogAsync($"删除7天前运行日志完成，共 {deletedRuns} 条");
                }

                // 删除15天前的错误日志
                var cutoffError = utcToday.AddDays(-15);
                var errors = await _repository.GetListAsync(w => w.Type == JobTaskLogTypeEnum.Error && w.CreateTime < cutoffError);

                if (errors.Any())
                {
                    deletedErrors = await _repository.BulkDeleteAsync(errors.ToList());
                    await AddLogAsync($"删除15天前错误日志完成，共 {deletedErrors} 条");
                }

                await AddLogAsync($"【日志清理完成】共删除：心跳类 {deletedHeartbeats} 条，运行日志 {deletedRuns} 条，错误日志 {deletedErrors} 条。");
            }
            catch (Exception ex)
            {
                await AddLogAsync($"【严重】日志清理任务执行失败：{ex.Message}\n{ex.StackTrace}", true);
            }
        }

        // 记录日志
        private async Task AddLogAsync(string log, bool isException = false)
        {
            await _service.AddLogAsync(_config.ClientCode, typeof(DeleteTaskLogJob).Name, log, isException);
        }
    }
}
