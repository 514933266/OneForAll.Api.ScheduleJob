using OneForAll.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.HttpService.Interfaces
{
    /// <summary>
    /// 启动类请求
    /// </summary>
    public interface IStartupHttpService
    {
        /// <summary>
        /// 重新启动（IIS回收时重新发起请求）
        /// </summary>
        /// <returns></returns>
        Task<BaseMessage> RequestAsync();
    }
}
