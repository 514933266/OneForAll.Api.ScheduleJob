using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using System.Threading.Tasks;
using ScheduleJob.Host.Models;
using ScheduleJob.Domain.Models;
using ScheduleJob.Application.Interfaces;
using System.Collections.Generic;
using ScheduleJob.Public.Models;
using ScheduleJob.Host.Filters;
using Autofac.Core;
using ScheduleJob.Domain.Enums;
using OneForAll.Core.OAuth;

namespace ScheduleJob.Host.Controllers
{
    /// <summary>
    /// 定时任务日志
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoleType.ADMIN)]
    public class JobTaskLogsController : BaseController
    {
        private readonly IJobTaskLogService _service;

        public JobTaskLogsController(IJobTaskLogService service)
        {
            _service = service;
        }

        /// <summary>
        /// 获取分页
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="appId">appid</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="type">日志类型</param>
        /// <param name="key">关键字</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>权限列表</returns>
        [HttpGet]
        [Route("{pageIndex}/{pageSize}")]
        [CheckPermission(Action = ConstPermission.EnterView)]
        public async Task<PageList<JobTaskLogDto>> GetPageAsync(
            int pageIndex,
            int pageSize,
            [FromQuery] string appId = default,
            [FromQuery] string taskName = default,
            [FromQuery] JobTaskLogTypeEnum type = JobTaskLogTypeEnum.None,
            [FromQuery] string key = default,
            [FromQuery] DateTime? startTime = default,
            [FromQuery] DateTime? endTime = default)
        {
            return await _service.GetPageAsync(pageIndex, pageSize, appId, taskName, type, key, startTime, endTime);
        }
    }
}
