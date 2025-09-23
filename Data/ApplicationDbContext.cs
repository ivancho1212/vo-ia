using Microsoft.EntityFrameworkCore;
using Voia.Api.Models;
using Voia.Api.Models.AiModelConfigs;
using Voia.Api.Models.BotActions;
using Voia.Api.Models.BotIntegrations;
using Voia.Api.Models.BotProfiles;
using Voia.Api.Models.BotTrainingSession;
using Voia.Api.Models.Conversations;
using Voia.Api.Models.GeneratedImages;
using Voia.Api.Models.Plans;
using Voia.Api.Models.Messages;
using Voia.Api.Models.StyleTemplate;
using Voia.Api.Models.Subscriptions;
using Voia.Api.Models.SupportTicket;
using Voia.Api.Models.TrainingDataSessions;
using Voia.Api.Models.UserBotRelations;
using Voia.Api.Models.UserPreferences;
using Voia.Api.Models.Chat;
using Voia.Api.Models.ConversationTag;
using Voia.Api.Models.Users;
using Voia.Api.Models.Bots;

namespace Voia.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Bot> Bots { get; set; }
        public DbSet<BotStyle> BotStyles { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportResponse> SupportResponses { get; set; }
        public DbSet<BotProfile> BotProfiles { get; set; }
        public DbSet<AiModelConfig> AiModelConfigs { get; set; }
        public DbSet<TrainingDataSession> TrainingDataSessions { get; set; }
        public DbSet<GeneratedImage> GeneratedImages { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<UserBotRelation> UserBotRelations { get; set; }
        public DbSet<BotAction> BotActions { get; set; }
        public DbSet<BotIntegration> BotIntegrations { get; set; }
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<BotTrainingConfig> BotTrainingConfigs { get; set; }
        public DbSet<KnowledgeChunk> KnowledgeChunks { get; set; }
        public DbSet<BotTemplate> BotTemplates { get; set; }
        // Removed duplicate definition of BotDataCaptureFields
        public DbSet<BotDataSubmission> BotDataSubmissions { get; set; }
        public DbSet<BotInstallationSetting> BotInstallationSettings { get; set; }
        public DbSet<BotIaProvider> BotIaProviders { get; set; }
        public DbSet<TokenUsageLog> TokenUsageLogs { get; set; }
        public DbSet<BotTemplatePrompt> BotTemplatePrompts { get; set; }
        public DbSet<BotCustomPrompt> BotCustomPrompts { get; set; }
        public DbSet<BotTrainingSession> BotTrainingSessions { get; set; }
        public DbSet<StyleTemplate> StyleTemplates { get; set; }
        public DbSet<TemplateTrainingSession> TemplateTrainingSessions { get; set; }
        public DbSet<VectorEmbedding> VectorEmbeddings { get; set; }
        public DbSet<TrainingCustomText> TrainingCustomTexts { get; set; }
        public DbSet<TrainingUrl> TrainingUrls { get; set; }
        public DbSet<UploadedDocument> UploadedDocuments { get; set; }
        public DbSet<BotDataCaptureField> BotDataCaptureFields { get; set; }
        public DbSet<ChatUploadedFile> ChatUploadedFiles { get; set; }
        public DbSet<ConversationTag> ConversationTags { get; set; }
        public DbSet<UserConsent> UserConsents { get; set; }
        public DbSet<PublicUser> PublicUsers { get; set; }
        public DbSet<BotWelcomeMessage> BotWelcomeMessages { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo de la entidad User a la tabla "users"
            modelBuilder.Entity<User>().ToTable("users");

            // --- MAPEO CAMPOS User ---
            modelBuilder
                .Entity<User>()
                .Property(u => u.DocumentNumber)
                .HasColumnName("document_number");

            modelBuilder
                .Entity<User>()
                .Property(u => u.DocumentTypeId)
                .HasColumnName("document_type_id");

            modelBuilder.Entity<User>().Property(u => u.Phone).HasColumnName("phone");

            modelBuilder.Entity<User>().Property(u => u.Address).HasColumnName("address");

            modelBuilder.Entity<User>().Property(u => u.IsVerified).HasColumnName("is_verified");

            modelBuilder
                .Entity<User>()
                .Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder
                .Entity<User>()
                .Property(u => u.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<User>().Property(u => u.AvatarUrl).HasColumnName("avatar_url");

            modelBuilder
                .Entity<User>()
                .Property(u => u.DocumentPhotoUrl)
                .HasColumnName("document_photo_url");

            modelBuilder
                .Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración de la relación User - Subscription
            modelBuilder
                .Entity<User>()
                .HasMany(u => u.Subscriptions) // Un usuario puede tener muchas suscripciones
                .WithOne(s => s.User) // Una suscripción pertenece a un solo usuario
                .HasForeignKey(s => s.UserId); // Relación con la clave foránea 'UserId'

            // Configuración de la tabla y columnas en snake_case para la tabla 'permissions'
            modelBuilder.Entity<Permission>().ToTable("permissions");

            // Configuración de la tabla y columnas en snake_case para la tabla 'rolepermissions'
            modelBuilder.Entity<RolePermission>().ToTable("rolepermissions");

            modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder
                .Entity<RolePermission>()
                .Property(rp => rp.RoleId)
                .HasColumnName("role_id");

            modelBuilder
                .Entity<RolePermission>()
                .Property(rp => rp.PermissionId)
                .HasColumnName("permission_id");

            modelBuilder
                .Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder
                .Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            modelBuilder.Entity<Bot>().Property(b => b.UserId).HasColumnName("user_id");

            modelBuilder.Entity<Bot>().Property(b => b.Name).HasColumnName("name");

            modelBuilder.Entity<Bot>().Property(b => b.Description).HasColumnName("description");

            modelBuilder.Entity<Bot>().Property(b => b.ApiKey).HasColumnName("api_key");

            modelBuilder
                .Entity<Bot>()
                .Property(b => b.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            modelBuilder
                .Entity<Bot>()
                .Property(b => b.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder
                .Entity<Bot>()
                .Property(b => b.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder
                .Entity<Bot>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bots) // Specify the navigation property on the User side
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            

            modelBuilder.Entity<BotCustomPrompt>().Property(p => p.Role).HasConversion<string>();
            modelBuilder.Entity<BotTemplatePrompt>().Property(p => p.Role).HasConversion<string>();

            modelBuilder.Entity<BotTemplate>()
                .HasMany(t => t.Prompts)
                .WithOne(p => p.BotTemplate) // 👈 esto es clave
                .HasForeignKey(p => p.BotTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BotTemplate>().Property(t => t.IaProviderId)
                .HasColumnName("ia_provider_id")
                .IsRequired();

            modelBuilder.Entity<BotTemplate>().Property(t => t.AiModelConfigId)
                .HasColumnName("ai_model_config_id")
                .IsRequired();

            modelBuilder.Entity<BotTemplate>().Property(t => t.DefaultStyleId)
                .HasColumnName("default_style_id");


            modelBuilder.Entity<TemplateTrainingSession>(entity =>
            {
                entity.ToTable("template_training_sessions");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.BotTemplateId).HasColumnName("bot_template_id");
                entity.Property(e => e.SessionName).HasColumnName("session_name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.BotTemplate)
                    .WithMany()
                    .HasForeignKey(e => e.BotTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UploadedDocument>()
                .HasOne(d => d.BotTemplate)
                .WithMany() // Si en BotTemplate no tienes una colección de documentos
                .HasForeignKey(d => d.BotTemplateId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<BotCustomPrompt>(entity =>
            {
                entity.ToTable("bot_custom_prompts");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.BotTemplateId).HasColumnName("bot_template_id"); // ✅ NUEVO
                entity.Property(e => e.TemplateTrainingSessionId).HasColumnName("template_training_session_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.TemplateTrainingSession)
                    .WithMany(t => t.BotCustomPrompts)
                    .HasForeignKey(e => e.TemplateTrainingSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.BotTemplate) // ✅ NUEVA RELACIÓN
                    .WithMany(t => t.CustomPrompts) // asegúrate de tener esto en el modelo BotTemplate
                    .HasForeignKey(e => e.BotTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ConversationTag>()
                    .HasOne(ct => ct.Conversation)
                    .WithMany()
                    .HasForeignKey(ct => ct.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConversationTag>()
                    .HasOne(ct => ct.HighlightedMessage)
                    .WithMany()
                    .HasForeignKey(ct => ct.HighlightedMessageId)
                    .OnDelete(DeleteBehavior.Restrict);

            // Configuración para BotStyle
            modelBuilder.Entity<BotStyle>(entity =>
            {
                entity.ToTable("bot_styles");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id"); // ← Nuevo mapeo agregado
                entity.Property(e => e.StyleTemplateId).HasColumnName("style_template_id");
                entity.Property(e => e.Theme).HasColumnName("theme");
                entity.Property(e => e.PrimaryColor).HasColumnName("primary_color");
                entity.Property(e => e.SecondaryColor).HasColumnName("secondary_color");
                entity.Property(e => e.FontFamily).HasColumnName("font_family");
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
                entity.Property(e => e.Position).HasColumnName("position");
                entity.Property(e => e.CustomCss).HasColumnName("custom_css");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<ChatUploadedFile>()
                .HasOne(f => f.Conversation)
                .WithMany()
                .HasForeignKey(f => f.ConversationId);

        }
    }
}
