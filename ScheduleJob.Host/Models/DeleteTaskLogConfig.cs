namespace ScheduleJob.Host.Models
{
    /// <summary>
    /// 日志清理配置
    /// </summary>
    public class DeleteTaskLogConfig
    {
        /// <summary>
        /// 心跳/注册/上线/下线日志保留天数
        /// </summary>
        public int HeartbeatDays { get; set; } = 1;

        /// <summary>
        /// 运行日志保留天数
        /// </summary>
        public int RunningDays { get; set; } = 7;

        /// <summary>
        /// 错误日志保留天数
        /// </summary>
        public int ErrorDays { get; set; } = 15;

        /// <summary>
        /// 批量删除时每批处理的记录数（默认500条）
        /// </summary>
        public int BatchSize { get; set; } = 500;
    }
}
