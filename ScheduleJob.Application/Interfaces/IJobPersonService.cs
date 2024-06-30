using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application.Interfaces
{
    /// <summary>
    /// 人员信息
    /// </summary>
    public interface IJobPersonService
    {
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns>列表</returns>
        Task<IEnumerable<JobPersonDto>> GetListAsync();
    }
}
