namespace ScheduleJob.Domain.Models
{
    /// <summary>
    /// 任务日志清理结果
    /// </summary>
    public class JobTaskLogCleanupResult
    {
        /// <summary>
        /// 已删除的心跳类日志数量
        /// </summary>
        public int DeletedHeartbeatCount { get; set; }

        /// <summary>
        /// 已删除的运行日志数量
        /// </summary>
        public int DeletedRunningCount { get; set; }

        /// <summary>
        /// 已删除的错误日志数量
        /// </summary>
        public int DeletedErrorCount { get; set; }
    }
}
