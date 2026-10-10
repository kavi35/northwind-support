using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

/// <summary>
/// List filters run against SQLite, so the assertions are on rows the database returned,
/// not on a query that was filtered in memory after it was loaded.
/// </summary>
public class TicketQueriesFilterTests
{
    private const string HighInProgress = "TCK-0001";
    private const string HighOpen = "TCK-0042";
    private const string ImportersTicket = "TCK-0019";

    [Fact]
    public async Task Priority_MatchesExactly_AndTheCountIsTakenAfterFiltering()
    {
        await using var fixture = await FilterFixture.CreateAsync();

        var first = await fixture.Queries.GetPagedAsync(High(page: 1, pageSize: 1), CancellationToken.None);
        var second = await fixture.Queries.GetPagedAsync(High(page: 2, pageSize: 1), CancellationToken.None);

        Assert.Equal(2, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Single(first.Items);
        Assert.Equal(2, second.TotalCount);
        Assert.Single(second.Items);
        Assert.Equal(
            new[] { HighInProgress, HighOpen }.OrderBy(reference => reference),
            new[] { first.Items[0].Reference, second.Items[0].Reference }.OrderBy(reference => reference));
        Assert.All(first.Items.Concat(second.Items), item => Assert.Equal(TicketPriority.High, item.Priority));
        Assert.Contains("WHERE", fixture.Sql.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Filters_CombineWithAnd()
    {
        await using var fixture = await FilterFixture.CreateAsync();

        var result = await fixture.Queries.GetPagedAsync(
            new TicketQuery
            {
                Status = TicketStatus.InProgress,
                Priority = TicketPriority.High,
                Page = 1,
                PageSize = 20
            },
            CancellationToken.None);

        var ticket = Assert.Single(result.Items);
        Assert.Equal(HighInProgress, ticket.Reference);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task Search_MatchesCustomerName_AndIgnoresSurroundingSpace()
    {
        await using var fixture = await FilterFixture.CreateAsync();

        var result = await fixture.Queries.GetPagedAsync(
            new TicketQuery { Search = "  Importers  ", Page = 1, PageSize = 20 },
            CancellationToken.None);

        var ticket = Assert.Single(result.Items);
        Assert.Equal(ImportersTicket, ticket.Reference);
        Assert.Equal("Wide World Importers", ticket.Customer.Name);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Search_Blank_IsIgnored()
    {
        await using var fixture = await FilterFixture.CreateAsync();

        var result = await fixture.Queries.GetPagedAsync(
            new TicketQuery { Search = "   ", Page = 1, PageSize = 20 },
            CancellationToken.None);

        Assert.Equal(FilterFixture.TicketCount, result.TotalCount);
    }

    [Fact]
    public async Task SlaStatus_IsAppliedInTheDatabase()
    {
        await using var fixture = await FilterFixture.CreateAsync();

        var withoutDueDate = await fixture.Queries.GetPagedAsync(
            new TicketQuery { SlaStatus = SlaStatus.NotApplicable, Page = 1, PageSize = 20 },
            CancellationToken.None);
        var breached = await fixture.Queries.GetPagedAsync(
            new TicketQuery { SlaStatus = SlaStatus.Breached, Page = 1, PageSize = 20 },
            CancellationToken.None);

        Assert.Equal(FilterFixture.TicketCount, withoutDueDate.TotalCount);
        Assert.Equal(0, breached.TotalCount);
        Assert.Contains("WHERE", fixture.Sql.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnassignedOnly_ReturnsTicketsWithNoAgent()
    {
        await using var fixture = await FilterFixture.CreateAsync();

        var result = await fixture.Queries.GetPagedAsync(
            new TicketQuery { UnassignedOnly = true, Page = 1, PageSize = 20 },
            CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, item => Assert.Null(item.AssignedAgent));
        Assert.DoesNotContain(result.Items, item => item.Reference == HighInProgress);
    }

    private static TicketQuery High(int page, int pageSize) => new()
    {
        Priority = TicketPriority.High,
        Page = page,
        PageSize = pageSize
    };

    /// <summary>
    /// SQLite cannot create the SQL Server covering and filtered indexes, so those indexes are
    /// dropped from the test model. The tables and the query are otherwise the real ones.
    /// </summary>
    private sealed class FilterFixture : IAsyncDisposable
    {
        public const int TicketCount = 5;

        private readonly SqliteConnection _connection;
        private readonly SupportDbContext _db;

        private FilterFixture(SqliteConnection connection, SupportDbContext db, TicketQueries queries, StringBuilder sql)
        {
            _connection = connection;
            _db = db;
            Queries = queries;
            Sql = sql;
        }

        public TicketQueries Queries { get; }

        public StringBuilder Sql { get; }

        public static async Task<FilterFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            try
            {
                var sql = new StringBuilder();
                var options = new DbContextOptionsBuilder<SupportDbContext>()
                    .UseSqlite(connection)
                    .LogTo(message => { sql.AppendLine(message); }, LogLevel.Information)
                    .Options;

                var db = new SqliteSupportDbContext(options);
                await db.Database.EnsureCreatedAsync();
                await SeedAsync(db);

                return new FilterFixture(connection, db, new TicketQueries(db, new FixedClock(), SlaPolicy.Default), sql);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private static async Task SeedAsync(SupportDbContext db)
        {
            var now = FixedClock.DefaultNow;
            var general = new Category("General", requiresSpecialist: false, forcesCriticalPriority: false);
            var billing = new Category("Billing", requiresSpecialist: true, forcesCriticalPriority: false);
            var contoso = new Customer("Contoso", "contoso@example.com", null, CustomerTier.Premium, now);
            var fabrikam = new Customer("Fabrikam", "fabrikam@example.com", null, CustomerTier.Standard, now);
            var importers = new Customer("Wide World Importers", "importers@example.com", null, CustomerTier.Premium, now);
            var alex = new Agent("Alex Turner", "alex@example.com", maxOpenTickets: 10, now);

            db.AddRange(general, billing, contoso, fabrikam, importers, alex);
            await db.SaveChangesAsync();

            var tickets = new[]
            {
                Raise("TCK-0001", "Printer jam", contoso, general, TicketPriority.High, TicketStatus.InProgress, alex),
                Raise("TCK-0002", "Password reset", contoso, general, TicketPriority.Low, TicketStatus.New, agent: null),
                Raise("TCK-0042", "Cannot sign in", fabrikam, billing, TicketPriority.High, TicketStatus.Open, alex),
                Raise("TCK-0004", "Catalogue reprint", fabrikam, billing, TicketPriority.Medium, TicketStatus.InProgress, agent: null),
                Raise(ImportersTicket, "Invoice export failed", importers, billing, TicketPriority.Low, TicketStatus.Open, agent: null)
            };

            db.AddRange(tickets);
            await db.SaveChangesAsync();
        }

        private static Ticket Raise(
            string reference,
            string title,
            Customer customer,
            Category category,
            TicketPriority priority,
            TicketStatus status,
            Agent? agent)
        {
            var ticket = Ticket.Raise(
                reference,
                title,
                "A description long enough to be stored.",
                customer.Id,
                category.Id,
                priority,
                FixedClock.DefaultNow);

            if (agent is not null)
            {
                ticket.AssignTo(agent.Id, FixedClock.DefaultNow);
            }

            if (status != TicketStatus.New)
            {
                ticket.ChangeStatus(status, FixedClock.DefaultNow);
            }

            return ticket;
        }
    }

    private sealed class SqliteSupportDbContext(DbContextOptions<SupportDbContext> options) : SupportDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var index in entityType.GetIndexes().Where(index => !index.IsUnique).ToList())
                {
                    entityType.RemoveIndex(index.Properties);
                }
            }
        }
    }
}
