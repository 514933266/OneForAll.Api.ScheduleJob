using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ScheduleJob.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application.Dtos
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public class JobTaskDto
    {
        public Guid Id { get; set; }

        /// <summary>
        /// 应用程序id
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 应用程序密钥
        /// </summary>
        public string AppSecret { get; set; }

        /// <summary>
        /// 所属组名称
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public JobTaskStatusEnum Status { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 运行节点名称
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string Cron { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 最后运行时间
        /// </summary>
        public DateTime RunningTime { get; set; }

        /// <summary>
        /// 心跳时间
        /// </summary>
        public DateTime HeartbeatTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
}
