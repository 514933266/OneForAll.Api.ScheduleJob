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
        private readonly IJobTaskService _service;
        private readonly IJobTaskLogRepository _repository;
        public DeleteTaskLogJob(
            AuthConfig config,
            IJobTaskService service,
            IJobTaskLogRepository repository)
        {
            _config = config;
            _service = service;
            _repository = repository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // 删除前一天的注册/上线/心跳/下线日志
            var name = typeof(DeleteTaskLogJob).Name;
            var heartbeats = await _repository.GetListAsync(w => w.Type != JobTaskLogTypeEnum.Running && w.Type != JobTaskLogTypeEnum.Error && w.CreateTime <= DateTime.Now.Date.AddDays(-1));
            if (heartbeats.Any())
            {
                var effected = await _repository.DeleteRangeAsync(heartbeats);
                await _service.AddLogAsync(_config.ClientCode, name, $"删除前一天的注册/上线/心跳/下线日志执行完成，共有{effected}条");
            }
            // 删除7天前的运行日志
            var sevenDays = await _repository.GetListAsync(w => w.Type == JobTaskLogTypeEnum.Running && w.CreateTime <= DateTime.Now.Date.AddDays(-7));
            if (sevenDays.Any())
            {
                var effected = await _repository.DeleteRangeAsync(sevenDays);
                await _service.AddLogAsync(_config.ClientCode, name, $"删除7天前的运行日志执行完成，共有{effected}条");
            }
            // 删除15天前的错误日志
            var errors = await _repository.GetListAsync(w => w.Type == JobTaskLogTypeEnum.Error && w.CreateTime <= DateTime.Now.Date.AddDays(-15));
            if (errors.Any())
            {
                var effected = await _repository.DeleteRangeAsync(errors);
                await _service.AddLogAsync(_config.ClientCode, name, $"删除15天前的错误日志执行完成，共有{effected}条");
            }
        }
    }
}
