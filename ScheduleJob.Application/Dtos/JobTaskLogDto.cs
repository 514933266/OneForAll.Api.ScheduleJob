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
    /// 定时任务日志
    /// </summary>
    public class JobTaskLogDto
    {
        /// <summary>
        /// 主键
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 应用程序id
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        ///任务名称
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// 日志内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public JobTaskLogTypeEnum Type { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
