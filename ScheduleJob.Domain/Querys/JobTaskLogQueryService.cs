using OneForAll.Core;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Querys
{
    /// <summary>
    /// 定时任务日志查询服务（只读，返回实体，不含 DTO）
    /// </summary>
    public class JobTaskLogQueryService : IJobTaskLogQueryService
    {
        private readonly IJobTaskLogRepository _repository;

        public JobTaskLogQueryService(IJobTaskLogRepository repository)
        {
            _repository = repository;
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
        public async Task<PageList<JobTaskLog>> GetPageAsync(
            int pageIndex,
            int pageSize,
            string appId,
            string taskName,
            JobTaskLogTypeEnum type,
            string key,
            DateTime? beiginTime,
            DateTime? endTime)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            return await _repository.GetPageAsync(pageIndex, pageSize, appId, taskName, type, key, beiginTime, endTime);
        }
    }
}
