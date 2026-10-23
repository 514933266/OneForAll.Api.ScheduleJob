using AutoMapper;
using OneForAll.Core;
using OneForAll.Core.Extension;
using OneForAll.Core.ORM;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TimeCrontab;

namespace ScheduleJob.Domain.Managers
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public class JobTaskManager : ScheduleJobDomainManager, IJobTaskManager 
    {
        private readonly IMapper _mapper;
        private readonly IJobTaskRepository _repository;
        private readonly IJobMidTaskPersonRepository _contactRepository;

        private readonly IHttpClientFactory _httpClientFactory;
        public JobTaskManager(
            IMapper mapper,
            ICurrentUser currentUser,
            IJobTaskRepository repository,
            IJobMidTaskPersonRepository contactRepository,
            IHttpClientFactory httpClientFactory) : base(currentUser)
        {
            _mapper = mapper;
            _repository = repository;
            _contactRepository = contactRepository;

            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="form">实体</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> RegisterAsync(JobTaskRegisterForm form)
        {
            var errType = BaseErrType.Fail;
            var data = await _repository.GetByAppAndNameAsync(form.AppId, form.Name);
            if (data != null)
            {
                if (!data.IsEnabled)
                    return BaseErrType.NotAllow;

                var id = data.Id;
                form.Id = id;
                _mapper.Map(form, data);
                data.UpdateTime = DateTime.UtcNow;
                data.RunningTime = DateTime.UtcNow;
                data.HeartbeatTime = DateTime.UtcNow;
                data.Status = JobTaskStatusEnum.Running;
                if (!string.IsNullOrEmpty(IpAddress))
                {
                    data.IpAddress = IpAddress;
                }
                errType = await ResultAsync(() => _repository.UpdateAsync(data));
            }
            else
            {
                data = _mapper.Map<JobTaskRegisterForm, JobTask>(form);
                data.Status = JobTaskStatusEnum.Running;
                if (!string.IsNullOrEmpty(IpAddress))
                {
                    data.IpAddress = IpAddress;
                }
                errType = await ResultAsync(() => _repository.AddAsync(data));
            }
            return errType;
        }

        /// <summary>
        /// 更改心跳状态
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> HeartbeatAsync(string appId, string taskName)
        {
            var data = await _repository.GetListEnabledAsync(appId, taskName);
            if (!data.Any())
                return BaseErrType.DataEmpty;

            data.ForEach(e =>
            {
                e.Status = JobTaskStatusEnum.Running;
                e.HeartbeatTime = DateTime.UtcNow;
            });

            return await ResultAsync(_repository.SaveChangesAsync);
        }

        /// <summary>
        /// 更新最后运行时间
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> UpdateRunningTimeAsync(string appId, string taskName)
        {
            var data = await _repository.GetEnabledByAppAndNameAsync(appId, taskName);
            if (data == null)
                return BaseErrType.DataNotFound;

            data.RunningTime = DateTime.UtcNow;
            return await ResultAsync(() => _repository.SaveChangesAsync());
        }

        /// <summary>
        /// 更改定时任务状态
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="status">任务状态</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> ChangeStatusAsync(string appId, string taskName, JobTaskStatusEnum status)
        {
            var data = await _repository.GetEnabledByAppAndNameAsync(appId, taskName);
            if (data == null)
                return BaseErrType.DataNotFound;

            data.Status = status;
            data.UpdateTime = DateTime.UtcNow;
            return await ResultAsync(() => _repository.SaveChangesAsync());
        }

        /// <summary>
        /// 按id更改定时任务状态
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="status">任务状态</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> ChangeStatusByIdAsync(Guid id, JobTaskStatusEnum status)
        {
            var data = await _repository.FindAsync(id);
            if (data == null)
                return BaseErrType.DataNotFound;

            data.Status = status;
            data.UpdateTime = DateTime.UtcNow;
            return await ResultAsync(() => _repository.SaveChangesAsync());
        }

        /// <summary>
        /// 按id更新修改时间（主动执行一次任务后记录）
        /// </summary>
        /// <param name="id">任务id</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> UpdateUpdateTimeByIdAsync(Guid id)
        {
            var data = await _repository.FindAsync(id);
            if (data == null)
                return BaseErrType.DataNotFound;

            data.UpdateTime = DateTime.UtcNow;
            return await ResultAsync(() => _repository.SaveChangesAsync());
        }

        /// <summary>
        /// 启用/禁用
        /// </summary>
        /// <param name="id">实体id</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> SetIsEnabledAsync(Guid id)
        {
            var data = await _repository.FindAsync(id);
            if (data == null) return BaseErrType.DataNotFound;

            data.IsEnabled = !data.IsEnabled;
            data.UpdateTime = DateTime.UtcNow;
            return await ResultAsync(() => _repository.SaveChangesAsync());
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="ids">权限id</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> DeleteAsync(IEnumerable<Guid> ids)
        {
            if (!ids.Any())
                return BaseErrType.DataEmpty;
            var data = await _repository.GetListAsync(ids);
            if (!data.Any())
                return BaseErrType.DataEmpty;

            return await ResultAsync(() => _repository.DeleteRangeAsync(data));
        }

        /// <summary>
        /// 设置负责人
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="personIds">人员id</param>
        /// <returns></returns>
        public async Task<BaseErrType> AddPersonsAsync(Guid id, IEnumerable<Guid> personIds)
        {
            var data = await _repository.FindAsync(id);
            if (data == null)
                return BaseErrType.DataNotFound;
            if (!personIds.Any())
                return BaseErrType.DataEmpty;
            return BaseErrType.Fail;
        }

        /// <summary>
        /// 尝试激活定时任务
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        public async Task<bool> CheckForSurvivalAsync(string appId, string taskName)
        {
            var data = await _repository.GetEnabledByAppAndNameAsync(appId, taskName);
            if (data == null)
                return false;

            if (!data.NodeName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                if (data.KeepAlive)
                {
                    var client = _httpClientFactory.CreateClient();
                    var url = $"{data.NodeName}/api/Startups";
                    var res = await client.GetAsync(url);
                    return res.StatusCode == System.Net.HttpStatusCode.OK ? true : false;
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// 巡检判定：根据 cron 推算最近一次预期执行时间，判断是否已过宽限期/错过执行
        /// </summary>
        /// <param name="job">任务</param>
        /// <param name="utcNow">当前时间（UTC）</param>
        /// <returns>判定结果</returns>
        public JobTaskInspectResult Inspect(JobTask job, DateTime utcNow)
        {
            var result = new JobTaskInspectResult();
            if (job == null || string.IsNullOrEmpty(job.Cron))
                return result;

            var beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

            // 将当前 UTC 时间转为北京时间，用于 Cron 计算
            var nowBeijing = TimeZoneInfo.ConvertTimeFromUtc(utcNow, beijingTimeZone);
            var crontab = Crontab.Parse(job.Cron, CronStringFormat.WithSeconds);

            // 将"北京时间"转换为 UTC 时间（用于和日志时间比较）
            var lastExpectedRunTime = TimeZoneInfo.ConvertTimeToUtc(crontab.GetPreviousOccurrence(nowBeijing), beijingTimeZone);

            result.LastExpectedRunTime = lastExpectedRunTime;

            // 宽限期 5 分钟：已过宽限期才需要探测/判定异常
            result.IsPastGracePeriod = utcNow > lastExpectedRunTime.AddMinutes(5);

            // 已过宽限期且最后运行时间早于预期执行时间，说明任务进程在但未按时执行
            result.IsMissed = result.IsPastGracePeriod && job.RunningTime < lastExpectedRunTime;

            return result;
        }
    }
}
