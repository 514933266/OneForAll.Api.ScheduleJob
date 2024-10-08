﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ScheduleJob.Domain.AggregateRoots;

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

            modelBuilder.Entity<JobTaskPersonContact>(entity =>
            {
                entity.ToTable("Job_TaskPersonContact");

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });
        }
    }
}
