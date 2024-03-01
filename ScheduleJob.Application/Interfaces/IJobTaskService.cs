using OneForAll.Core;
using OneForAll.Core.OAuth;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application.Interfaces
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public interface IJobTaskService
    {
        /// <summary>
        /// 获取分页列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="key">关键字</param>
        /// <param name="groupName">所属分组</param>
        /// <param name="nodeName">所属节点</param>
        /// <returns>分页列表</returns>
        Task<PageList<JobTaskDto>> GetPageAsync(int pageIndex, int pageSize, string key, string groupName, string nodeName);

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="form">表单</param>
        /// <returns>结果</returns>
        Task<BaseErrType> RegisterAsync(JobTaskRegisterForm form);

        /// <summary>
        /// 更改心跳状态
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        Task<BaseErrType> HeartbeatAsync(string appId, string taskName);

        /// <summary>
        /// 定时服务下线
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        Task<BaseErrType> DownLineAsync(string appId, string taskName);

        /// <summary>
        /// 启用/禁用
        /// </summary>
        /// <param name="id">实体id</param>
        /// <returns>结果</returns>
        Task<BaseErrType> SetIsEnabledAsync(Guid id);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="ids">任务id</param>
        /// <returns>结果</returns>
        Task<BaseErrType> DeleteAsync(IEnumerable<Guid> ids);

        /// <summary>
        /// 更改心跳状态
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="log">日志内容</param>
        /// <returns>结果</returns>
        Task<BaseErrType> AddLogAsync(string appId, string taskName, string log);

        /// <summary>
        /// 改变定时任务状态
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="status">状态</param>
        /// <param name="loginUser">登录用户</param>
        /// <returns></returns>
        Task<BaseErrType> ChangeStatusAsync(Guid id, JobTaskStatusEnum status, LoginUser loginUser);

        /// <summary>
        /// 执行一次定时任务
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="loginUser">登录用户</param>
        /// <returns></returns>
        Task<BaseErrType> ExcuteAsync(Guid id, LoginUser loginUser);

        /// <summary>
        /// 设置负责人
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="personIds">人员id</param>
        /// <returns></returns>
        Task<BaseErrType> AddPersonsAsync(Guid id, IEnumerable<Guid> personIds);
    }
}
