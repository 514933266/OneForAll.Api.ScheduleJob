using Microsoft.EntityFrameworkCore;
using ScheduleJob.Domain.Entities;

namespace ScheduleJob.Host
{
    public partial class JobDbContext : DbContext
    {
        public JobDbContext(DbContextOptions<JobDbContext> options)
            : base(options)
        {

        }

        public virtual DbSet<JobTask> JobTasks { get; set; }
        public virtual DbSet<JobTaskLog> JobTaskLogs { get; set; }
        public virtual DbSet<JobNotificationConfig> JobNotificationConfigs { get; set; }
        public virtual DbSet<JobMidTaskPerson> JobMidTaskPersons { get; set; }
        public virtual DbSet<JobRunningLock> JobRunningLocks { get; set; }
        public virtual DbSet<JobLockHolder> JobLockHolders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JobTask>(entity =>
            {
                entity.ToTable("job_task");
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<JobTaskLog>(entity =>
            {
                entity.ToTable("job_task_log");
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<JobNotificationConfig>(entity =>
            {
                entity.ToTable("job_notification_config");
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<JobMidTaskPerson>(entity =>
            {
                entity.ToTable("job_mid_task_person");
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<JobRunningLock>(entity =>
            {
                entity.ToTable("job_running_lock");
                entity.HasIndex(e => new { e.ClientId, e.TaskName }).IsUnique();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<JobLockHolder>(entity =>
            {
                entity.ToTable("job_lock_holder");
                entity.HasIndex(e => new { e.ClientId, e.TaskName, e.Version }).IsUnique();
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });
        }
    }
}
