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

namespace ScheduleJob.Host.Controllers
{
    /// <summary>
    /// 定时任务
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoleType.ADMIN)]
    public class JobTasksController : BaseController
    {
        private readonly IJobTaskService _service;

        public JobTasksController(IJobTaskService service)
        {
            _service = service;
        }

        /// <summary>
        /// 获取分页
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="key">关键字</param>
        /// <param name="groupName">所属分组</param>
        /// <param name="nodeName">所属节点</param>
        /// <returns>权限列表</returns>
        [HttpGet]
        [Route("{pageIndex}/{pageSize}")]
        [CheckPermission(Action = ConstPermission.EnterView)]
        public async Task<PageList<JobTaskDto>> GetPageAsync(
            int pageIndex,
            int pageSize,
            [FromQuery] string key,
            [FromQuery] string groupName,
            [FromQuery] string nodeName)
        {
            return await _service.GetPageAsync(pageIndex, pageSize, key, groupName, nodeName);
        }

        /// <summary>
        /// 启用/禁用
        /// </summary>
        /// <param name="id">实体id</param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{id}/IsEnabled")]
        [CheckPermission(Action = ConstPermission.EnterView)]
        public async Task<BaseMessage> SetIsEnabledAsync(Guid id)
        {
            var msg = new BaseMessage();

            msg.ErrType = await _service.SetIsEnabledAsync(id);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("设置成功");
                default: return msg.Fail("设置失败");
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="ids">实体id</param>
        /// <returns></returns>
        [HttpPatch]
        [Route("Batch/IsDeleted")]
        [CheckPermission(Action = ConstPermission.EnterView)]
        public async Task<BaseMessage> DeleteAsync([FromBody] IEnumerable<Guid> ids)
        {
            var msg = new BaseMessage();
            msg.ErrType = await _service.DeleteAsync(ids);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("删除成功");
                case BaseErrType.DataNotFound: return msg.Success("数据不存在");
                default: return msg.Fail("删除失败");
            }
        }
    }
}