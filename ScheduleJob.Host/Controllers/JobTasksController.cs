using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using System.Threading.Tasks;
using ScheduleJob.Application.Interfaces;
using System.Collections.Generic;
using ScheduleJob.Public.Models;
using ScheduleJob.Host.Filters;
using OneForAll.Core.OAuth;
using ScheduleJob.Domain.Enums;
using System.Net.NetworkInformation;

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
            [FromQuery] string key = default,
            [FromQuery] string groupName = default,
            [FromQuery] string nodeName = default)
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

        /// <summary>
        /// 改变定时任务状态
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="status">状态</param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{id}/Status")]
        public async Task<BaseMessage> ChangeStatusAsync(Guid id, [FromQuery] JobTaskStatusEnum status)
        {
            var msg = new BaseMessage();
            msg.ErrType = await _service.ChangeStatusAsync(id, status, LoginUser);
            var op = "操作";
            switch (status)
            {
                case JobTaskStatusEnum.Unstart: op = "暂停"; break;
                case JobTaskStatusEnum.Running: op = "启动"; break;
            }

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success($"{op}成功");
                case BaseErrType.DataNotFound: return msg.Fail("数据不存在");
                case BaseErrType.NotAllow: return msg.Fail("仅允许启动/暂停操作");
                case BaseErrType.Overflow: return msg.Fail("请先启用定时任务");
                case BaseErrType.DataNotMatch: return msg.Fail($"本地运行任务无法{op}");
                default: return msg.Fail($"{op}失败");
            }
        }

        /// <summary>
        /// 执行一次定时任务
        /// </summary>
        /// <param name="id">任务id</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{id}/Excute")]
        public async Task<BaseMessage> ExcuteAsync(Guid id)
        {
            var msg = new BaseMessage();
            msg.ErrType = await _service.ExcuteAsync(id, LoginUser);
            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("执行成功");
                case BaseErrType.DataNotFound: return msg.Fail("数据不存在");
                case BaseErrType.NotAllow: return msg.Fail("请先启动定时任务");
                case BaseErrType.Overflow: return msg.Fail("请先启用定时任务");
                case BaseErrType.DataNotMatch: return msg.Fail("本地运行任务无法执行操作");
                default: return msg.Fail("执行失败");
            }
        }

        /// <summary>
        /// 设置负责人
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="personIds">人员id</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{id}/Persons")]
        public async Task<BaseMessage> AddPersonsAsync(Guid id, [FromBody] IEnumerable<Guid> personIds)
        {
            var msg = new BaseMessage();
            msg.ErrType = await _service.AddPersonsAsync(id, personIds);
            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("设置成功");
                case BaseErrType.DataNotFound: return msg.Fail("数据不存在");
                case BaseErrType.DataEmpty: return msg.Fail("请选择负责人");
                default: return msg.Fail("设置失败");
            }
        }
    }
}