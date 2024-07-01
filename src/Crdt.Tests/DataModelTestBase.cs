using Crdt.Changes;
using Crdt.Core;
using Crdt.Db;
using Crdt.Sample;
using Crdt.Sample.Changes;
using Crdt.Sample.Models;
using Crdt.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Crdt.Tests;

public class DataModelTestBase : IAsyncLifetime
{
    protected readonly ServiceProvider _services;
    protected readonly Guid _localClientId = Guid.NewGuid();
    public readonly DataModel DataModel;
    public readonly SampleDbContext DbContext;
    protected readonly MockTimeProvider MockTimeProvider = new();

    public DataModelTestBase()
    {
        _services = new ServiceCollection()
            .AddCrdtDataSample(":memory:")
            .Replace(ServiceDescriptor.Singleton<IHybridDateTimeProvider>(MockTimeProvider))
            .BuildServiceProvider();
        DbContext = _services.GetRequiredService<SampleDbContext>();
        DbContext.Database.OpenConnection();
        DbContext.Database.EnsureCreated();
        DataModel = _services.GetRequiredService<DataModel>();
    }

    public void SetCurrentDate(DateTime dateTime)
    {
        currentDate = dateTime;
    }
    private static int _instanceCount = 0;
    private DateTimeOffset currentDate = new(new DateTime(2000, 1, 1, 0, 0, 0).AddHours(_instanceCount++));
    private DateTimeOffset NextDate() => currentDate = currentDate.AddDays(1);

    public async ValueTask<Commit> WriteNextChange(IChange change, bool add = true)
    {
        return await WriteChange(_localClientId, NextDate(), change, add);
    }

    public async ValueTask<Commit> WriteNextChange(IEnumerable<IChange> changes)
    {
        return await WriteChange(_localClientId, NextDate(), changes);
    }

    public async ValueTask<Commit> WriteChangeAfter(Commit after, IChange change)
    {
        return await WriteChange(_localClientId, after.DateTime.AddHours(1), change);
    }

    public async ValueTask<Commit> WriteChangeBefore(Commit before, IChange change, bool add = true)
    {
        return await WriteChange(_localClientId, before.DateTime.AddHours(-1), change, add);
    }

    protected async ValueTask<Commit> WriteChange(Guid clientId,
        DateTimeOffset dateTime,
        IChange change,
        bool add = true)
    {
        return await WriteChange(clientId, dateTime, [change], add);
    }

    protected async ValueTask<Commit> WriteChange(Guid clientId,
        DateTimeOffset dateTime,
        IEnumerable<IChange> changes,
        bool add = true)
    {
        if (!add)
            return new Commit
            {
                ClientId = clientId,
                HybridDateTime = new HybridDateTime(dateTime, 0),
                ChangeEntities = changes.Select((change, index) => new ChangeEntity<IChange>
                {
                    Change = change, Index = index, CommitId = change.CommitId, EntityId = change.EntityId
                }).ToList()
            };
        MockTimeProvider.SetNextDateTime(dateTime);
        return await DataModel.AddChanges(clientId, changes);
    }

    protected async Task AddCommitsViaSync(IEnumerable<Commit> commits)
    {
        await ((ISyncable)DataModel).AddRangeFromSync(commits);
    }

    public IChange SetWord(Guid entityId, string value)
    {
        return new SetWordTextChange(entityId, value);
    }

    public IChange NewDefinition(Guid wordId,
        string text,
        string partOfSpeech,
        double order = 0,
        Guid? definitionId = default)
    {
        return new NewDefinitionChange(definitionId ?? Guid.NewGuid())
        {
            WordId = wordId,
            Text = text,
            PartOfSpeech = partOfSpeech,
            Order = order
        };
    }

    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
    }

    protected IEnumerable<object> AllData()
    {
        return DbContext.Commits
            .Include(c => c.ChangeEntities)
            .Include(c => c.Snapshots)
            .DefaultOrder()
            .ToArray()
            .OfType<object>()
            .Concat(DbContext.Set<Word>().OrderBy(w => w.Text));
    }
}
