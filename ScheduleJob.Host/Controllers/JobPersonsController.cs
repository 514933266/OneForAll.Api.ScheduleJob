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

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="form">表单</param>
        /// <returns>结果</returns>
        [HttpPost]
        [CheckPermission(Action = ConstPermission.EnterView)]
        public async Task<BaseMessage> AddAsync([FromBody] JobPersonForm form)
        {
            var msg = new BaseMessage();
            msg.ErrType = await _service.AddAsync(form);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("添加成功");
                default: return msg.Fail("添加失败");
            }
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="form">表单</param>
        /// <returns>结果</returns>
        [HttpPut]
        [CheckPermission(Action = ConstPermission.EnterView)]
        public async Task<BaseMessage> UpdateAsync([FromBody] JobPersonForm form)
        {
            var msg = new BaseMessage();
            msg.ErrType = await _service.UpdateAsync(form);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("修改成功");
                default: return msg.Fail("修改失败");
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="id">人员信息id</param>
        /// <returns>结果</returns>
        [HttpDelete]
        [Route("{id}")]
        [CheckPermission(Action = ConstPermission.EnterView)]
        public async Task<BaseMessage> DeleteAsync(Guid id)
        {
            var msg = new BaseMessage();
            msg.ErrType = await _service.DeleteAsync(id);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("删除成功");
                default: return msg.Fail("删除失败");
            }
        }
    }
}
