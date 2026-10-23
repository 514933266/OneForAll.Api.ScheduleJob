using AutoMapper;
using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application
{
    /// <summary>
    /// 定时任务日志
    /// </summary>
    public class JobTaskLogService : IJobTaskLogService
    {
        private readonly IMapper _mapper;
        private readonly IJobTaskLogQueryService _queryService;
        private readonly IJobTaskLogManager _logManager;
        public JobTaskLogService(
            IMapper mapper,
            IJobTaskLogQueryService queryService,
            IJobTaskLogManager logManager)
        {
            _mapper = mapper;
            _queryService = queryService;
            _logManager = logManager;
        }

        /// <summary>
        /// 获取分页列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="appId">appid</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="type">日志类型</param>
        /// <param name="key">关键字</param>
        /// <param name="beiginTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>分页列表</returns>
        public async Task<PageList<JobTaskLogDto>> GetPageAsync(
            int pageIndex,
            int pageSize,
            string appId,
            string taskName,
            JobTaskLogTypeEnum type,
            string key,
            DateTime? beiginTime,
            DateTime? endTime)
        {
            var data = await _queryService.GetPageAsync(pageIndex, pageSize, appId, taskName, type, key, beiginTime, endTime);
            var items = _mapper.Map<IEnumerable<JobTaskLog>, IEnumerable<JobTaskLogDto>>(data.Items);
            return new PageList<JobTaskLogDto>(data.Total, data.PageSize, data.PageIndex, items);
        }

        /// <summary>
        /// 按配置分批清理过期日志（调度任务调用）
        /// </summary>
        /// <param name="config">清理配置</param>
        /// <returns>清理结果（含执行日志）</returns>
        public async Task<JobTaskLogCleanupDto> ClearExpiredAsync(JobTaskLogCleanupConfig config)
        {
            config = config ?? new JobTaskLogCleanupConfig();

            var dto = new JobTaskLogCleanupDto();
            var utcToday = DateTime.UtcNow.Date;
            var batchSize = config.BatchSize > 0 ? config.BatchSize : 500;

            // 删除心跳/注册/上线/下线日志（分批处理）
            dto.DeletedHeartbeatCount = await ClearBatchesAsync(
                JobTaskLogClearTypeEnum.Normal,
                utcToday.AddDays(-config.HeartbeatDays),
                batchSize,
                $"分批删除{config.HeartbeatDays}天前心跳类日志",
                dto.Logs);

            // 删除运行日志（分批处理）
            dto.DeletedRunningCount = await ClearBatchesAsync(
                JobTaskLogClearTypeEnum.Running,
                utcToday.AddDays(-config.RunningDays),
                batchSize,
                $"分批删除{config.RunningDays}天前运行日志",
                dto.Logs);

            // 删除错误日志（分批处理）
            dto.DeletedErrorCount = await ClearBatchesAsync(
                JobTaskLogClearTypeEnum.Error,
                utcToday.AddDays(-config.ErrorDays),
                batchSize,
                $"分批删除{config.ErrorDays}天前错误日志",
                dto.Logs);

            dto.Logs.Add($"【日志清理完成】配置(心跳:{config.HeartbeatDays}天/运行:{config.RunningDays}天/错误:{config.ErrorDays}天/批次:{batchSize}条)，共删除：心跳类 {dto.DeletedHeartbeatCount} 条，运行日志 {dto.DeletedRunningCount} 条，错误日志 {dto.DeletedErrorCount} 条。");
            return dto;
        }

        /// <summary>
        /// 按类型循环分批删除，直到无数据为止
        /// </summary>
        /// <param name="type">清理类型</param>
        /// <param name="cutoff">截止时间（UTC）</param>
        /// <param name="batchSize">单批上限</param>
        /// <param name="title">日志文案前缀</param>
        /// <param name="logs">执行日志</param>
        /// <returns>删除总量</returns>
        private async Task<int> ClearBatchesAsync(JobTaskLogClearTypeEnum type, DateTime cutoff, int batchSize, string title, List<string> logs)
        {
            var total = 0;
            while (true)
            {
                var count = await _logManager.ClearBatchAsync(type, cutoff, batchSize);
                if (count <= 0)
                    break;

                total += count;
                logs.Add($"{title}，本批 {count} 条，累计 {total} 条");
            }
            return total;
        }
    }
}
