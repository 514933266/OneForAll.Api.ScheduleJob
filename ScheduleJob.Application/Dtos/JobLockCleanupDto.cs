namespace ScheduleJob.Application.Dtos
{
    /// <summary>
    /// 锁状态检查结果
    /// </summary>
    public class JobLockCleanupDto
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
        /// 执行日志（供调度任务逐条落库）
        /// </summary>
        public System.Collections.Generic.List<string> Logs { get; set; } = new System.Collections.Generic.List<string>();
    }
}
