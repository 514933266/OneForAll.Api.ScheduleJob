namespace ScheduleJob.Application.Dtos
{
    /// <summary>
    /// 任务日志清理结果
    /// </summary>
    public class JobTaskLogCleanupDto
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

        /// <summary>
        /// 执行日志（供调度任务逐条落库）
        /// </summary>
        public System.Collections.Generic.List<string> Logs { get; set; } = new System.Collections.Generic.List<string>();
    }
}
