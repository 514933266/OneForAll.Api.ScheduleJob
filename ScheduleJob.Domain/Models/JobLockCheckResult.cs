namespace ScheduleJob.Domain.Models
{
    /// <summary>
    /// 定时任务运行锁检查结果
    /// </summary>
    public class JobLockCheckResult
    {
        /// <summary>
        /// 是否允许执行（false：已达最大并发数，本次跳过）
        /// </summary>
        public bool CanExecute { get; set; }

        /// <summary>
        /// 是否已真正持有锁（true：执行完需调用释放接口）
        /// </summary>
        public bool LockAcquired { get; set; }

        /// <summary>
        /// 当前锁版本号（释放时回传）
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 是否为锁基础设施异常（锁表未创建、连接失败、超时、死锁等）
        /// </summary>
        public bool IsInfrastructureError { get; set; }

        /// <summary>
        /// 锁基础设施异常消息
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
