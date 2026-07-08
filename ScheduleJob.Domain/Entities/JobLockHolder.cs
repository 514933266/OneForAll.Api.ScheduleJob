using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleJob.Domain.Entities
{
    /// <summary>
    /// 定时任务当前锁持有者
    /// </summary>
    public class JobLockHolder
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 任务名称（唯一）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TaskName { get; set; }

        /// <summary>
        /// 客户端ID（机器名、IP或实例标识）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ClientId { get; set; }

        /// <summary>
        /// 客户端编号（相同客户端ID可能编号不同）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ClientCode { get; set; }

        /// <summary>
        /// 锁版本号
        /// </summary>
        [Required]
        public int Version { get; set; }

        /// <summary>
        /// 获取锁时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime LockTime { get; set; } = DateTime.UtcNow;
    }
}
