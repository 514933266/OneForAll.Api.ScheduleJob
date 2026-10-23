using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application.Interfaces
{
    /// <summary>
    /// 定时任务日志
    /// </summary>
    public interface IJobTaskLogService
    {
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
        Task<PageList<JobTaskLogDto>> GetPageAsync(
            int pageIndex,
            int pageSize,
            string appId,
            string taskName,
            JobTaskLogTypeEnum type,
            string key,
            DateTime? beiginTime,
            DateTime? endTime);

        /// <summary>
        /// 按配置分批清理过期日志（调度任务调用）
        /// </summary>
        /// <param name="config">清理配置</param>
        /// <returns>清理结果（含执行日志）</returns>
        Task<JobTaskLogCleanupDto> ClearExpiredAsync(JobTaskLogCleanupConfig config);
    }
}
