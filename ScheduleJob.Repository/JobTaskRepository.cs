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

namespace ScheduleJob.Repository
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public class JobTaskRepository : Repository<JobTask>, IJobTaskRepository
    {
        public JobTaskRepository(DbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 查询分页
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="key">关键字</param>
        /// <param name="groupName">所属分组</param>
        /// <param name="nodeName">所属节点</param>
        /// <returns>分页列表</returns>
        public async Task<PageList<JobTask>> GetPageAsync(int pageIndex, int pageSize, string key, string groupName, string nodeName)
        {
            var predicate = PredicateBuilder.Create<JobTask>(w => true);
            if (!key.IsNullOrEmpty())
                predicate = predicate.And(w => w.Name.Contains(key));
            if (!groupName.IsNullOrEmpty())
                predicate = predicate.And(w => w.GroupName.Contains(groupName));
            if (!nodeName.IsNullOrEmpty())
                predicate = predicate.And(w => w.NodeName.Contains(nodeName));

            var total = await DbSet.CountAsync(predicate);

            var data = await DbSet
                .AsNoTracking()
                .Where(predicate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PageList<JobTask>(total, pageSize, pageIndex, data);
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="ids">id集合</param>
        /// <returns>列表</returns>
        public async Task<IEnumerable<JobTask>> GetListAsync(IEnumerable<Guid> ids)
        {
            return await DbSet.Where(w => ids.Contains(w.Id)).ToListAsync();
        }
    }
}
