using OneForAll.Core;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Interfaces
{
    /// <summary>
    /// 定时任务日志
    /// </summary>
    public interface IJobTaskLogManager
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="form">表单</param>
        /// <returns>结果</returns>
        Task<BaseErrType> AddAsync(JobTaskLogForm form);

        /// <summary>
        /// 删除7天前的日志
        /// </summary>
        /// <returns>结果</returns>
        Task<BaseErrType> DeleteListSevenDayAsync();

        /// <summary>
        /// 按类型分批删除截止时间之前的日志
        /// </summary>
        /// <param name="type">日志清理类型</param>
        /// <param name="cutoff">截止时间（UTC）</param>
        /// <param name="batchSize">单批删除上限</param>
        /// <returns>本批删除数量</returns>
        Task<int> ClearBatchAsync(JobTaskLogClearTypeEnum type, DateTime cutoff, int batchSize);
    }
}
