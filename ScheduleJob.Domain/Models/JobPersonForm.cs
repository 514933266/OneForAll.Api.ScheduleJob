using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Models
{
    /// <summary>
    /// 人员信息
    /// </summary>
    public class JobPersonForm
    {
        /// <summary>
        /// 唯一键
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Name { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [StringLength(100)]
        public string Email { get; set; }

        /// <summary>
        /// 联系方式
        /// </summary>
        [StringLength(20)]
        public string Mobile { get; set; }
    }
}
