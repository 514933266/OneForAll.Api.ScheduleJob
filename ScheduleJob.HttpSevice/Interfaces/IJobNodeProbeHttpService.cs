using System.Threading.Tasks;

namespace ScheduleJob.HttpService.Interfaces
{
    /// <summary>
    /// 任务运行节点存活探测
    /// </summary>
    public interface IJobNodeProbeHttpService
    {
        /// <summary>
        /// 探测节点是否存活（GET {nodeUrl}/api/Startups 返回 200 视为存活）
        /// </summary>
        /// <param name="nodeUrl">节点地址</param>
        /// <returns>是否存活</returns>
        Task<bool> IsAliveAsync(string nodeUrl);
    }
}
