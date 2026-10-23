namespace ScheduleJob.Domain.Enums
{
    /// <summary>
    /// 任务日志清理类型
    /// </summary>
    public enum JobTaskLogClearTypeEnum
    {
        /// <summary>
        /// 心跳/注册/上线/下线等正常类日志
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 运行日志
        /// </summary>
        Running = 1,

        /// <summary>
        /// 错误日志
        /// </summary>
        Error = 2
    }
}
