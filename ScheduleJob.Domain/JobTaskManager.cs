using AutoMapper;
using Microsoft.AspNetCore.Http;
using OneForAll.Core;
using OneForAll.Core.Extension;
using OneForAll.Core.ORM;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public class JobTaskManager : JobBaseManager, IJobTaskManager
    {
        private readonly IJobTaskRepository _repository;
        public JobTaskManager(
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IJobTaskRepository repository) : base(mapper, httpContextAccessor)
        {
            _repository = repository;
        }

        /// <summary>
        /// 获取分页列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="key">关键字</param>
        /// <param name="groupName">所属分组</param>
        /// <param name="nodeName">所属节点</param>
        /// <returns>分页列表</returns>
        public async Task<PageList<JobTask>> GetPageAsync(int pageIndex, int pageSize, string key, string groupName, string nodeName)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            return await _repository.GetPageAsync(pageIndex, pageSize, key, groupName, nodeName);
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="form">实体</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> RegisterAsync(JobTaskRegisterForm form)
        {
            var errType = BaseErrType.Fail;
            var data = await _repository.GetAsync(w => w.AppId == form.AppId && w.Name == form.Name);
            if (data != null)
            {
                if (!data.IsEnabled)
                    return BaseErrType.NotAllow;

                var id = data.Id;
                form.Id = id;
                _mapper.Map(form, data);
                data.UpdateTime = DateTime.Now;
                data.RunningTime = DateTime.Now;
                data.HeartbeatTime = DateTime.Now;
                data.Status = JobTaskStatusEnum.Running;
                data.IpAddress = _httpContextAccessor.HttpContext == null ? "127.0.0.0" : _httpContextAccessor.HttpContext.Request.Host.Value;
                errType = await ResultAsync(() => _repository.UpdateAsync(data));
            }
            else
            {
                data = _mapper.Map<JobTaskRegisterForm, JobTask>(form);
                data.Status = JobTaskStatusEnum.Running;
                data.IpAddress = _httpContextAccessor.HttpContext == null ? "127.0.0.0" : _httpContextAccessor.HttpContext.Request.Host.Value;
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
            var predicate = PredicateBuilder.Create<JobTask>(w => w.AppId == appId && w.IsEnabled);
            if (!taskName.IsNullOrEmpty())
                predicate = predicate.And(w => w.Name == taskName);

            var data = await _repository.GetListAsync(predicate);
            if (!data.Any())
                return BaseErrType.DataEmpty;

            data.ForEach(e =>
            {
                e.Status = JobTaskStatusEnum.Running;
                e.HeartbeatTime = DateTime.Now;
            });

            return await ResultAsync(_repository.SaveChangesAsync);
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
            var data = await _repository.GetAsync(w => w.AppId == appId && w.Name == taskName && w.IsEnabled);
            if (data == null) return BaseErrType.DataNotFound;

            data.Status = status;
            data.UpdateTime = DateTime.Now;
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
            data.UpdateTime = DateTime.Now;
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
    }
}

