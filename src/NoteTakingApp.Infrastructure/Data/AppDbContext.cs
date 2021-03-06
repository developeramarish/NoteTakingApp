﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NoteTakingApp.Core.Common;
using NoteTakingApp.Core.Interfaces;
using NoteTakingApp.Core.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NoteTakingApp.Infrastructure.Data
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        private readonly IMediator _mediator;
        public AppDbContext(DbContextOptions options, IMediator mediator = default(IMediator))
            :base(options) {
            _mediator = mediator;
        }

        public static readonly LoggerFactory ConsoleLoggerFactory
            = new LoggerFactory(new[] {
                new ConsoleLoggerProvider((category, level)
                    => category == DbLoggerCategory.Database.Command.Name 
                && level == LogLevel.Information, true) });
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Note> Notes { get; set; }        
        public DbSet<Tag> Tags { get; set; }
        public DbSet<User> Users { get; set; }
        
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            var result = default(int);
            
            var domainEventEntities = ChangeTracker.Entries<Entity>()
                .Select(entityEntry => entityEntry.Entity)
                .Where(entity => entity.DomainEvents.Any())
                .ToArray();
            
            foreach (var entity in ChangeTracker.Entries<Entity>()
                .Where(e => (e.State == EntityState.Added || (e.State == EntityState.Modified)))
                .Select(x => x.Entity))
            {
                var isNew = entity.CreatedOn == default(DateTime);
                entity.CreatedOn = isNew ? DateTime.UtcNow : entity.CreatedOn;   
                entity.LastModifiedOn = DateTime.UtcNow;
            }

            foreach (var item in ChangeTracker.Entries<Entity>().Where(e => e.State == EntityState.Deleted))
            {
                item.State = EntityState.Modified;
                item.Entity.IsDeleted = true;
            }

            result = await base.SaveChangesAsync(cancellationToken);

            foreach (var entity in domainEventEntities)
            {
                var events = entity.DomainEvents.ToArray();

                entity.ClearEvents();

                foreach (var domainEvent in events)
                {
                    await _mediator.Publish(domainEvent, cancellationToken);
                }
            }

            return result;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Note>()
                .HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<Note>()
                .Property(e =>e.Version)
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            modelBuilder.Entity<Tag>()
                .HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<User>()
                .HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<NoteTag>()
                .HasKey(t => new { t.TagId, t.NoteId });

            base.OnModelCreating(modelBuilder);
        }       
    }
}