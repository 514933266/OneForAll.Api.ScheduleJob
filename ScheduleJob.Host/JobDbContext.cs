using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ScheduleJob.Domain.Entities;

namespace ScheduleJob.Host
{
    public partial class JobDbContext : DbContext
    {
        public JobDbContext(DbContextOptions<JobDbContext> options)
            : base(options)
        {

        }

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
        }
    }
}
