using Microsoft.EntityFrameworkCore;
using OneForAll.Core.Extension;
using OneForAll.Core.ORM;
using OneForAll.Core;
using OneForAll.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Domain.Enums;

namespace ScheduleJob.Repository
{
    /// <summary>
    /// 定时任务日志
    /// </summary>
    public class JobTaskLogRepository : Repository<JobTaskLog>, IJobTaskLogRepository
    {
        public JobTaskLogRepository(DbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 查询分页
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="appId">appid</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="type">日志类型</param>
        /// <param name="key">关键字</param>
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
            var predicate = PredicateBuilder.Create<JobTaskLog>(w => true);
            if (!appId.IsNullOrEmpty())
                predicate = predicate.And(w => w.AppId.Contains(appId));
            if (!taskName.IsNullOrEmpty())
                predicate = predicate.And(w => w.TaskName.Contains(taskName));
            if (type != JobTaskLogTypeEnum.None)
                predicate = predicate.And(w => w.Type == type);
            if (!key.IsNullOrEmpty())
                predicate = predicate.And(w => w.Content.Contains(key));
            if (beiginTime != null)
                predicate = predicate.And(w => w.CreateTime >= beiginTime);
            if (endTime != null)
                predicate = predicate.And(w => w.CreateTime <= endTime);

            var total = await DbSet.CountAsync(predicate);

            var data = await DbSet
                .AsNoTracking()
                .Where(predicate)
                .OrderByDescending(o => o.CreateTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PageList<JobTaskLog>(total, pageSize, pageIndex, data);
        }
    }
}
