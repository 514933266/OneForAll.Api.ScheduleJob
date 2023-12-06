using Microsoft.EntityFrameworkCore;
using OneForAll.EFCore;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Repository
{
    /// <summary>
    /// 微信登录用户
    /// </summary>
    public class SysWechatUserRepository : Repository<SysWechatUser>, ISysWechatUserRepository
    {
        public SysWechatUserRepository(DbContext context)
            : base(context)
        {

        }
    }
}
