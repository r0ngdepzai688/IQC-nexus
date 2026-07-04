using Microsoft.EntityFrameworkCore;
using IqcQms.Domain.Entities.Auth;
using IqcQms.Domain.Entities.HR;
using IqcQms.Domain.Entities.Equipment;
using IqcQms.Domain.Entities.Standards;
using IqcQms.Domain.Entities.System;
using IqcQms.Domain.Entities.Chat;

namespace IqcQms.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<InspectionStandard> InspectionStandards { get; set; }
        public DbSet<InspectionItem> InspectionItems { get; set; }
        public DbSet<DynamicForm> DynamicForms { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        
        // Chat module
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Explicit table mappings and relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<InspectionStandard>()
                .HasOne(i => i.Part)
                .WithMany()
                .HasForeignKey(i => i.PartId);

            modelBuilder.Entity<InspectionItem>()
                .HasOne(i => i.InspectionStandard)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.InspectionStandardId);

            // Chat configurations
            modelBuilder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageAttachment>()
                .HasOne(ma => ma.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(ma => ma.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageReaction>()
                .HasOne(mr => mr.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(mr => mr.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
