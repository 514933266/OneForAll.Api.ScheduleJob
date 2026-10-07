namespace ScheduleJob.Host.Models
{
    /// <summary>
    /// 定时任务运行锁配置
    /// </summary>
    public class JobLockConfig
    {
        /// <summary>
        /// 锁基础设施异常（锁表未创建、数据库连接失败、超时、死锁等）时是否放行执行（默认false：跳过）
        /// 启用后异常时照常执行任务，但本次执行不参与并发控制
        /// </summary>
        public bool FailOpenOnException { get; set; } = false;
    }
}
