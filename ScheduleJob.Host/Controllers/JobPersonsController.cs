using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneForAll.Core;
using OneForAll.Core.OAuth;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Host.Filters;
using ScheduleJob.Public.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScheduleJob.Host.Controllers
{
    /// <summary>
    /// 人员信息
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoleType.ADMIN)]
    public class JobPersonsController : BaseController
    {
        private readonly IJobPersonService _service;

        public JobPersonsController(IJobPersonService service)
        {
            _service = service;
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns>用户列表</returns>
        [HttpGet]
        public async Task<IEnumerable<JobPersonDto>> GetListAsync()
        {
            return await _service.GetListAsync();
        }
    }
}
