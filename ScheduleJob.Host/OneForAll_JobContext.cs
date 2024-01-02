using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ScheduleJob.Domain.AggregateRoots;

namespace ScheduleJob.Host
{
    public partial class OneForAll_JobContext : DbContext
    {
        public OneForAll_JobContext(DbContextOptions<OneForAll_JobContext> options)
            : base(options)
        {

        }

        public virtual DbSet<JobTask> JobTask { get; set; }
        public virtual DbSet<JobTaskLog> JobTaskLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JobTask>(entity =>
            {
                entity.ToTable("Job_Task");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<JobTaskLog>(entity =>
            {
                entity.ToTable("Job_TaskLog");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<JobNotificationConfig>(entity =>
            {
                entity.ToTable("Job_NotificationConfig");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });
        }
    }
}
