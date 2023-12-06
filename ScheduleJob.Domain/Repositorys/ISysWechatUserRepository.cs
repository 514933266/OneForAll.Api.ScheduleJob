using OneForAll.EFCore;
using ScheduleJob.Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Repositorys
{
    /// <summary>
    /// 微信登录用户
    /// </summary>
    public interface ISysWechatUserRepository : IEFCoreRepository<SysWechatUser>
    {
    }
}
