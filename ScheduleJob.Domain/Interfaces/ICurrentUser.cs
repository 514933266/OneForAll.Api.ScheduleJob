using OneForAll.Core.OAuth;

namespace ScheduleJob.Domain.Interfaces
{
    /// <summary>
    /// 当前请求上下文端口：领域层通过该端口获取登录态与来源IP，不直接依赖 HttpContext
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// 当前登录用户信息
        /// </summary>
        LoginUser LoginUser { get; }

        /// <summary>
        /// 当前请求来源IP（无请求上下文时为空字符串）
        /// </summary>
        string IpAddress { get; }
    }
}
