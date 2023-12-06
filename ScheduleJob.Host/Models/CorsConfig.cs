using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleJob.Host.Models
{
    /// <summary>
    /// 跨域配置模型
    /// </summary>
    public class CorsConfig
    {
        /// <summary>
        /// 域名
        /// <para>例：http://www.localhost:80</para>
        /// </summary>
        public string[] Origins { get; set; }
    }
}
