using OneForAll.Core;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Interfaces
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public interface IJobTaskManager
    {
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
        /// 更新最后运行时间
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        Task<BaseErrType> UpdateRunningTimeAsync(string appId, string taskName);

        /// <summary>
        /// 更改定时任务状态
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="status">任务状态</param>
        /// <returns>结果</returns>
        Task<BaseErrType> ChangeStatusAsync(string appId, string taskName, JobTaskStatusEnum status);

        /// <summary>
        /// 按id更改定时任务状态
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="status">任务状态</param>
        /// <returns>结果</returns>
        Task<BaseErrType> ChangeStatusByIdAsync(Guid id, JobTaskStatusEnum status);

        /// <summary>
        /// 按id更新修改时间
        /// </summary>
        /// <param name="id">任务id</param>
        /// <returns>结果</returns>
        Task<BaseErrType> UpdateUpdateTimeByIdAsync(Guid id);

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
        /// 设置负责人
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="personIds">人员id</param>
        /// <returns></returns>
        Task<BaseErrType> AddPersonsAsync(Guid id, IEnumerable<Guid> personIds);

        /// <summary>
        /// 尝试激活定时任务
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        Task<bool> CheckForSurvivalAsync(string appId, string taskName);

        /// <summary>
        /// 巡检判定：根据 cron 推算最近一次预期执行时间，判断是否已过宽限期/错过执行
        /// </summary>
        /// <param name="job">任务</param>
        /// <param name="utcNow">当前时间（UTC）</param>
        /// <returns>判定结果</returns>
        JobTaskInspectResult Inspect(JobTask job, DateTime utcNow);
    }
}
