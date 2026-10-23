namespace ScheduleJob.Domain.Models
{
    /// <summary>
    /// 锁状态检查结果
    /// </summary>
    public class JobLockCleanupResult
    {
        /// <summary>
        /// 已校验的任务阈值数量
        /// </summary>
        public int TaskThresholdCount { get; set; }

        /// <summary>
        /// 已释放的残留锁数量
        /// </summary>
        public int DeletedHolderCount { get; set; }

        /// <summary>
        /// 已修正的运行计数数量
        /// </summary>
        public int AdjustedLockCount { get; set; }

        /// <summary>
        /// 生效的最低阈值分钟数（配置规范化后）
        /// </summary>
        public int MinMinutes { get; set; }

        /// <summary>
        /// 生效的最高阈值分钟数（配置规范化后）
        /// </summary>
        public int MaxMinutes { get; set; }

        /// <summary>
        /// 生效的周期缓冲分钟数（配置规范化后）
        /// </summary>
        public int BufferMinutes { get; set; }
    }
}
