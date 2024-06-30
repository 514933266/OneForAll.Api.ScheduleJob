using AutoMapper;
using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application
{
    /// <summary>
    /// 人员信息
    /// </summary>
    public class JobPersonService : IJobPersonService
    {
        private readonly IMapper _mapper;
        public JobPersonService(IMapper mapper)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns>列表</returns>
        public async Task<IEnumerable<JobPersonDto>> GetListAsync()
        {
            return new List<JobPersonDto>();
        }
    }
}
