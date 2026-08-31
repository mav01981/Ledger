using Ledger.Domain;
using Ledger.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Data;

public class LedgerDbContext : DbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options) { }

    public DbSet<EventRecord> Events => Set<EventRecord>();
    public DbSet<SnapshotRecord> Snapshots => Set<SnapshotRecord>();
    public DbSet<TransferRecord> Transfers => Set<TransferRecord>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<AccountBalanceReadModel> AccountBalances => Set<AccountBalanceReadModel>();
    public DbSet<TransactionHistoryReadModel> TransactionHistory => Set<TransactionHistoryReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventRecord>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => new { e.StreamId, e.StreamPosition });
            entity.Property(e => e.StreamId).HasColumnName("stream_id");
            entity.Property(e => e.StreamPosition).HasColumnName("stream_position");
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.HasIndex(e => e.GlobalPosition).IsUnique();
        });

        modelBuilder.Entity<SnapshotRecord>(entity =>
        {
            entity.ToTable("snapshots");
            entity.HasKey(e => new { e.AggregateId, e.Version });
            entity.Property(e => e.AggregateId).HasColumnName("aggregate_id");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.State).HasColumnName("state").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.SnapshottedAt).HasColumnName("snapshotted_at");
        });

        modelBuilder.Entity<TransferRecord>(entity =>
        {
            entity.ToTable("transfers");
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Lines).HasColumnName("lines").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasColumnName("idempotency_key").HasMaxLength(200);
            entity.Property(e => e.AggregateId).HasColumnName("aggregate_id");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
        });

        modelBuilder.Entity<AccountBalanceReadModel>(entity =>
        {
            entity.ToTable("account_balances");
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AccountType).HasColumnName("account_type").HasMaxLength(50);
            entity.Property(e => e.Balance).HasColumnName("balance").HasColumnType("numeric(18,2)");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.OpenedAt).HasColumnName("opened_at");
            entity.Property(e => e.LastEventVersion).HasColumnName("last_event_version");
        });

        modelBuilder.Entity<TransactionHistoryReadModel>(entity =>
        {
            entity.ToTable("transaction_history");
            entity.HasKey(e => new { e.AccountId, e.TransactionId, e.Direction });
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            entity.Property(e => e.Direction).HasColumnName("direction").HasMaxLength(10);
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.RunningBalance).HasColumnName("running_balance").HasColumnType("numeric(18,2)");
        });
    }
}
