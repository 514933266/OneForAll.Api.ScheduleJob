using Microsoft.Data.SqlClient;
using OneForAll.Core;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Models;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Threading.Tasks;

namespace ScheduleJob.Application
{
    /// <summary>
    /// 定时任务运行锁（OpenApi）
    /// </summary>
    public class JobLockService : IJobLockService
    {
        /// <summary>
        /// 默认最大并发数（仅锁记录首次创建时生效）
        /// </summary>
        private const int DEFAULT_MAX_CONCURRENT = 1;

        private readonly IJobRunningLockRepository _lockRepository;
        private readonly IJobLockHolderRepository _holderRepository;

        public JobLockService(
            IJobRunningLockRepository lockRepository,
            IJobLockHolderRepository holderRepository)
        {
            _lockRepository = lockRepository;
            _holderRepository = holderRepository;
        }

        /// <summary>
        /// 尝试获取执行资格（供任务节点远程调用，锁记录不存在时自动创建默认配置）
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="clientCode">客户端编号</param>
        /// <returns>检查结果</returns>
        public async Task<JobLockCheckResult> TryAcquireAsync(string appId, string taskName, string clientCode)
        {
            var currentVersion = 0;
            try
            {
                var lockEntity = await _lockRepository.GetWithLockAsync(appId, taskName, asNoTracking: true);

                // 如果不存在锁配置，创建默认配置
                if (lockEntity == null)
                {
                    lockEntity = new JobRunningLock
                    {
                        Id = Guid.NewGuid(),
                        IsEnabled = true,
                        TaskName = taskName,
                        ClientId = appId,
                        Version = 0,
                        MaxConcurrent = DEFAULT_MAX_CONCURRENT,
                        CurrentRunningCount = 0,
                        CreateTime = DateTime.UtcNow,
                        UpdateTime = DateTime.UtcNow
                    };

                    _lockRepository.Add(lockEntity);
                }

                currentVersion = lockEntity.Version + 1;

                // 如果未启用并发控制，直接允许执行（未加锁，无需释放）
                if (!lockEntity.IsEnabled)
                    return new JobLockCheckResult { CanExecute = true, LockAcquired = false, Version = currentVersion };

                // 检查是否达到最大并发数
                if (lockEntity.CurrentRunningCount >= lockEntity.MaxConcurrent)
                    return new JobLockCheckResult { CanExecute = false, LockAcquired = false, Version = currentVersion };

                // 事务内：INSERT Holder（利用唯一索引作为锁）+ UPDATE 计数（乐观锁二次校验）
                var isSuccess = await _lockRepository.TryAcquireWithHolderAsync(
                    lockEntity.Id, appId, clientCode, taskName, lockEntity.Version, currentVersion);

                return new JobLockCheckResult { CanExecute = isSuccess, LockAcquired = isSuccess, Version = currentVersion };
            }
            catch (Exception ex)
            {
                // 唯一索引冲突属于正常并发竞争（如补建锁记录瞬间被其他实例抢占），拒绝执行
                if (IsUniqueIndexConflict(ex))
                    return new JobLockCheckResult { CanExecute = false, LockAcquired = false, Version = currentVersion };

                // 锁基础设施异常向上抛出，由调用方（任务节点）决定放行或跳过
                throw;
            }
        }

        /// <summary>
        /// 释放执行资格
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="version">锁版本号</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> ReleaseAsync(string appId, string taskName, int version)
        {
            var lockEntity = await _lockRepository.GetAsync(appId, taskName, asNoTracking: true);

            if (lockEntity != null && lockEntity.CurrentRunningCount >= 1)
            {
                var isSuccess = await _lockRepository.DecrementRunningCountAsync(lockEntity.Id);
                if (!isSuccess)
                    return BaseErrType.Fail;
            }

            // 删除锁持有者记录（按持有版本精确匹配）
            var holder = await _holderRepository.GetAsync(x => x.ClientId == appId && x.TaskName == taskName && x.Version == version);

            if (holder != null)
            {
                await _holderRepository.DeleteAsync(holder);
            }

            return BaseErrType.Success;
        }

        /// <summary>
        /// 判断异常链中是否包含唯一索引冲突（SQL Server 错误号 2601/2627）
        /// </summary>
        private static bool IsUniqueIndexConflict(Exception ex)
        {
            while (ex != null)
            {
                if (ex is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                    return true;
                ex = ex.InnerException;
            }
            return false;
        }
    }
}
