using Microsoft.EntityFrameworkCore;
using IqcQms.Domain.Entities.Auth;
using IqcQms.Domain.Entities.HR;
using IqcQms.Domain.Entities.Equipment;
using IqcQms.Domain.Entities.Standards;
using IqcQms.Domain.Entities.System;
using IqcQms.Domain.Entities.Chat;
using IqcQms.Domain.Entities.Tasks;
using IqcQms.Domain.Entities.NewModels;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Domain.Entities.Agent;

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

        // Tasks module
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<TaskChecklist> TaskChecklists { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }
        public DbSet<TaskTemplate> TaskTemplates { get; set; }
        public DbSet<TaskDependency> TaskDependencies { get; set; }

        // New Models module
        public DbSet<MasterPlanUpload> MasterPlanUploads { get; set; }
        public DbSet<MasterPlanRecord> MasterPlanRecords { get; set; }
        public DbSet<ProjectWorkspace> ProjectWorkspaces { get; set; }
        public DbSet<MasterPlan> MasterPlans { get; set; }
        public DbSet<ProjectMilestone> ProjectMilestones { get; set; }

        // Data Hub module
        public DbSet<DataSource> DataSources { get; set; }
        public DbSet<ImportBatch> ImportBatches { get; set; }
        public DbSet<RawFile> RawFiles { get; set; }
        public DbSet<StagingMasterPlan> StagingMasterPlans { get; set; }
        public DbSet<ValidationError> ValidationErrors { get; set; }
        public DbSet<BusinessReviewQueue> BusinessReviewQueues { get; set; }
        public DbSet<MappingDictionary> MappingDictionaries { get; set; }
        public DbSet<ImportLog> ImportLogs { get; set; }
        public DbSet<DataHubAuditLog> DataHubAuditLogs { get; set; }
        public DbSet<HeaderMappingProfile> HeaderMappingProfiles { get; set; }

        // Persistent Import Orchestration Module
        public DbSet<PersistentImportJob> PersistentImportJobs { get; set; }
        public DbSet<PersistentImportMappedPayload> PersistentImportMappedPayloads { get; set; }
        public DbSet<CommittedImportRecord> CommittedImportRecords { get; set; }
        public DbSet<PersistentImportAuditEvent> PersistentImportAuditEvents { get; set; }
        public DbSet<PersistentImportCommitReceipt> PersistentImportCommitReceipts { get; set; }
        public DbSet<PersistentImportWorkItem> PersistentImportWorkItems { get; set; }
        public DbSet<PersistentImportOutboxMessage> PersistentImportOutboxMessages { get; set; }

        // Agent Client Module
        public DbSet<AgentDevice> AgentDevices { get; set; }
        public DbSet<AgentCredential> AgentCredentials { get; set; }
        public DbSet<AgentPairingRequest> AgentPairingRequests { get; set; }
        public DbSet<AgentRefreshOperationResult> AgentRefreshOperationResults { get; set; }

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

            // Tasks module
            modelBuilder.Entity<TaskChecklist>()
                .HasOne(tc => tc.TaskItem)
                .WithMany(t => t.Checklists)
                .HasForeignKey(tc => tc.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskComment>()
                .HasOne(tc => tc.TaskItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(tc => tc.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskAttachment>()
                .HasOne(ta => ta.TaskItem)
                .WithMany(t => t.Attachments)
                .HasForeignKey(ta => ta.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.ParentTask)
                .WithMany(t => t.Subtasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskDependency>()
                .HasOne(td => td.Task)
                .WithMany(t => t.DependentOn)
                .HasForeignKey(td => td.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MasterPlan>()
                .HasIndex(value => new { value.BasicKey, value.CatKey })
                .IsUnique();
            modelBuilder.Entity<MasterPlan>()
                .Property(value => value.Version)
                .IsConcurrencyToken();
            modelBuilder.Entity<HeaderMappingProfile>()
                .HasIndex(value => new { value.NormalizedHeaderPath, value.CanonicalField, value.WorkbookFingerprint });

            modelBuilder.Entity<TaskDependency>()
                .HasOne(td => td.PrerequisiteTask)
                .WithMany(t => t.RequiredBy)
                .HasForeignKey(td => td.PrerequisiteTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // Persistent Import Indexes & Constraints
            modelBuilder.Entity<PersistentImportJob>()
                .HasIndex(j => j.OwnerUserId);
            modelBuilder.Entity<PersistentImportJob>()
                .HasIndex(j => j.State);
            modelBuilder.Entity<PersistentImportJob>()
                .HasIndex(j => j.CreatedAt);
            modelBuilder.Entity<PersistentImportJob>()
                .HasIndex(j => j.CommitIdempotencyKey);

            modelBuilder.Entity<PersistentImportMappedPayload>()
                .HasIndex(p => p.JobId);

            modelBuilder.Entity<CommittedImportRecord>()
                .HasIndex(r => r.ImportJobId);

            modelBuilder.Entity<PersistentImportAuditEvent>()
                .HasIndex(a => new { a.JobId, a.OccurredAt });

            modelBuilder.Entity<PersistentImportCommitReceipt>()
                .HasIndex(c => c.JobId);

            modelBuilder.Entity<PersistentImportWorkItem>()
                .HasIndex(w => new { w.State, w.AvailableAtUtc });
            modelBuilder.Entity<PersistentImportWorkItem>()
                .HasIndex(w => w.JobId);

            modelBuilder.Entity<PersistentImportOutboxMessage>()
                .HasIndex(o => new { o.IsDispatched, o.OccurredAtUtc });

            // Agent Client Module Indexes & Constraints
            modelBuilder.Entity<AgentDevice>()
                .HasIndex(d => d.DeviceId)
                .IsUnique();
            modelBuilder.Entity<AgentDevice>()
                .HasIndex(d => d.OwnerUserId);
            modelBuilder.Entity<AgentDevice>()
                .HasIndex(d => d.State);
            modelBuilder.Entity<AgentDevice>()
                .Property(d => d.ConcurrencyVersion)
                .IsConcurrencyToken();

            modelBuilder.Entity<AgentCredential>()
                .HasOne(c => c.Device)
                .WithMany(d => d.Credentials)
                .HasForeignKey(c => c.AgentDeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AgentCredential>()
                .HasIndex(c => c.DeviceId);
            modelBuilder.Entity<AgentCredential>()
                .HasIndex(c => c.CredentialIdentifier)
                .IsUnique();
            modelBuilder.Entity<AgentCredential>()
                .HasIndex(c => c.TokenFamilyId);
            modelBuilder.Entity<AgentCredential>()
                .HasIndex(c => new { c.AgentDeviceId, c.RefreshOperationId });

            modelBuilder.Entity<AgentPairingRequest>()
                .HasIndex(p => p.HashedCode);
            modelBuilder.Entity<AgentPairingRequest>()
                .HasIndex(p => p.OwnerUserId);
            modelBuilder.Entity<AgentPairingRequest>()
                .HasIndex(p => p.State);
            modelBuilder.Entity<AgentPairingRequest>()
                .Property(p => p.ConcurrencyVersion)
                .IsConcurrencyToken();

            modelBuilder.Entity<AgentRefreshOperationResult>()
                .HasIndex(r => new { r.AgentDeviceId, r.RefreshOperationId })
                .IsUnique();
        }
    }
}
