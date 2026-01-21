using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Public.Models
{
    /// <summary>
    /// RabbitMQ链接
    /// </summary>
    public class RabbitMqConnectionConfig
    {
        /// <summary>
        /// 主机
        /// </summary>
        public string HostName { get; set; } = "localhost";

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// 账号
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// 虚拟主机
        /// </summary>
        public string VirtualHost { get; set; } = "/";
    }
}
