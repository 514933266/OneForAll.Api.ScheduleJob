using OneForAll.Core;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Interfaces
{
    /// <summary>
    /// 定时任务日志查询服务（只读，返回实体，不含 DTO）
    /// </summary>
    public interface IJobTaskLogQueryService
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
        Task<PageList<JobTaskLog>> GetPageAsync(
            int pageIndex,
            int pageSize,
            string appId,
            string taskName,
            JobTaskLogTypeEnum type,
            string key,
            DateTime? beiginTime,
            DateTime? endTime);
    }
}
