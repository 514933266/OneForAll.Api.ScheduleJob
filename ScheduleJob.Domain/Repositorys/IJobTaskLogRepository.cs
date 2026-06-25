using OneForAll.Core;
using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Repositorys
{
    /// <summary>
    /// 定时任务日志
    /// </summary>
    public interface IJobTaskLogRepository : IEFCoreRepository<JobTaskLog>
    {
        /// <summary>
        /// 查询分页
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


        /// <summary>
        /// 查询第一条数据
        /// </summary>
        /// <returns>列表</returns>
        Task<JobTaskLog> GetTop1ByTaskAsync(string appId, string taskName);

        /// <summary>
        /// 分批查询日志（用于批量删除）
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <param name="take">每次获取数量</param>
        /// <returns>列表</returns>
        Task<IEnumerable<JobTaskLog>> GetListWithTakeAsync(System.Linq.Expressions.Expression<Func<JobTaskLog, bool>> predicate, int take);
    }
}
