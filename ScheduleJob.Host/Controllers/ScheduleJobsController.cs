using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OneForAll.Core;
using System.Threading.Tasks;
using ScheduleJob.Domain.Models;
using ScheduleJob.Application.Interfaces;

namespace ScheduleJob.Host.Controllers
{
    /// <summary>
    /// 定时任务
    /// </summary>
    [AllowAnonymous]
    [Route("api/[controller]")]

    public class ScheduleJobsController : BaseController
    {
        private readonly IScheduleJobService _service;
        private readonly IJobLockService _jobLockService;

        public ScheduleJobsController(
            IScheduleJobService service,
            IJobLockService jobLockService)
        {
            _service = service;
            _jobLockService = jobLockService;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="entity">表单</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<BaseMessage> RegisterAsync([FromBody] JobTaskRegisterForm entity)
        {
            var msg = new BaseMessage();
            msg.ErrType = await _service.RegisterAsync(entity);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("服务注册成功");
                case BaseErrType.NotAllow: return msg.Fail("不允许注册");
                case BaseErrType.PasswordInvalid: return msg.Fail("AppId或AppSecret错误");
                default: return msg.Fail("服务注册失败");
            }
        }

        /// <summary>
        /// 定时服务心跳
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns></returns>
        [HttpPatch]
        [Route("{appId}/{taskName}/Heartbeats")]
        public async Task<BaseMessage> HeartbeatAsync(string appId, string taskName)
        {
            var msg = new BaseMessage();

            msg.ErrType = await _service.HeartbeatAsync(appId, taskName);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("心跳更新成功");
                case BaseErrType.DataNotFound: return msg.Fail("服务不存在");
                case BaseErrType.NotAllow: return msg.Fail("不允许心跳更新");
                default: return msg.Fail("心跳更新失败");
            }
        }

        /// <summary>
        /// 定时服务下线
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{appId}/{taskName}")]
        public async Task<BaseMessage> DownLineAsync(string appId, string taskName)
        {
            var msg = new BaseMessage();

            msg.ErrType = await _service.DownLineAsync(appId, taskName);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("状态变更成功");
                case BaseErrType.DataNotFound: return msg.Fail("服务不存在");
                case BaseErrType.NotAllow: return msg.Fail("不允许下线状态更新");
                default: return msg.Fail("状态变更失败");
            }
        }

        /// <summary>
        /// 定时服务记录日志
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="log">日志</param>
        /// <param name="isException">是否异常日志</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{appId}/{taskName}/Logs")]
        public async Task<BaseMessage> AddLogAsync(string appId, string taskName, [FromBody] string log, [FromQuery] bool isException = false)
        {
            var msg = new BaseMessage();

            msg.ErrType = await _service.AddLogAsync(appId, taskName, log, isException);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("日志添加成功");
                case BaseErrType.DataNotFound: return msg.Fail("服务不存在");
                case BaseErrType.DataEmpty: return msg.Fail("服务不存在");
                default: return msg.Fail("日志添加失败");
            }
        }

        /// <summary>
        /// 尝试获取任务执行资格（并发控制检查，供任务节点远程调用）
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="clientCode">客户端编号</param>
        /// <returns>检查结果（CanExecute：是否允许执行；LockAcquired：是否需释放；Version：锁版本号）</returns>
        [HttpPost]
        [Route("{appId}/{taskName}/LockChecks")]
        public async Task<BaseMessage<JobLockCheckResult>> TryAcquireLockAsync(string appId, string taskName, [FromQuery] string clientCode = "")
        {
            var result = await _jobLockService.TryAcquireAsync(appId, taskName, clientCode);

            return BaseMessage<JobLockCheckResult>.Success(
                result.CanExecute ? "获取执行资格成功" : "已达最大并发数，本次执行被跳过",
                result);
        }

        /// <summary>
        /// 释放任务执行资格
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="version">锁版本号</param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{appId}/{taskName}/LockChecks/{version}")]
        public async Task<BaseMessage> ReleaseLockAsync(string appId, string taskName, int version)
        {
            var msg = new BaseMessage();

            msg.ErrType = await _jobLockService.ReleaseAsync(appId, taskName, version);

            switch (msg.ErrType)
            {
                case BaseErrType.Success: return msg.Success("释放成功");
                default: return msg.Fail("释放失败");
            }
        }
    }
}
