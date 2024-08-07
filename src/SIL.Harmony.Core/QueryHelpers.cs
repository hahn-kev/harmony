﻿using Microsoft.EntityFrameworkCore;

namespace SIL.Harmony.Core;

public static class QueryHelpers
{
    public static async Task<SyncState> GetSyncState(this IQueryable<CommitBase> commits)
    {
        var dict = await commits.GroupBy(c => c.ClientId)
            .Select(g => new { ClientId = g.Key, DateTime = g.Max(c => c.HybridDateTime.DateTime) })
            .AsAsyncEnumerable()//this is so the ticks are calculated server side instead of the db
            .ToDictionaryAsync(c => c.ClientId, c => c.DateTime.ToUnixTimeMilliseconds());
        return new SyncState(dict);
    }

    public static async Task<ChangesResult<TCommit>> GetChanges<TCommit, TChange>(this IQueryable<TCommit> commits, SyncState remoteState) where TCommit : CommitBase<TChange>
    {
        var newHistory = new List<TCommit>();
        var localSyncState = await commits.GetSyncState();
        foreach (var (clientId, localTimestamp) in localSyncState.ClientHeads)
        {
            //client is new to the other history
            if (!remoteState.ClientHeads.TryGetValue(clientId, out var otherTimestamp))
            {
                //todo slow, it would be better if we could query on client id and get latest changes per client
                newHistory.AddRange(await commits.Include(c => c.ChangeEntities).DefaultOrder()
                    .Where(c => c.ClientId == clientId)
                    .ToArrayAsync());
            }
            //client has newer history than the other history
            else if (localTimestamp > otherTimestamp)
            {
                var otherDt = DateTimeOffset.FromUnixTimeMilliseconds(otherTimestamp);
                //todo even slower because we need to filter out changes that are already in the other history
                newHistory.AddRange((await commits.Include(c => c.ChangeEntities).DefaultOrder()
                    .Where(c => c.ClientId == clientId && c.HybridDateTime.DateTime > otherDt)
                    .ToArrayAsync())
                    //fixes an issue where the query would include commits that are already in the other history
                    .Where(c => c.DateTime.ToUnixTimeMilliseconds() > otherTimestamp));
            }
        }

        return new(newHistory.ToArray(), localSyncState);
    }

    public static IQueryable<T> DefaultOrder<T>(this IQueryable<T> queryable) where T: CommitBase
    {
        return queryable
            .OrderBy(c => c.HybridDateTime.DateTime)
            .ThenBy(c => c.HybridDateTime.Counter)
            .ThenBy(c => c.Id);
    }

    public static IEnumerable<T> DefaultOrder<T>(this IEnumerable<T> queryable) where T: CommitBase
    {
        return queryable
            .OrderBy(c => c.HybridDateTime.DateTime)
            .ThenBy(c => c.HybridDateTime.Counter)
            .ThenBy(c => c.Id);
    }

    public static IQueryable<T> DefaultOrderDescending<T>(this IQueryable<T> queryable) where T: CommitBase
    {
        return queryable
            .OrderByDescending(c => c.HybridDateTime.DateTime)
            .ThenByDescending(c => c.HybridDateTime.Counter)
            .ThenByDescending(c => c.Id);
    }

    public static IQueryable<T> WhereAfter<T>(this IQueryable<T> queryable, T after) where T : CommitBase
    {
        return queryable.Where(c => after.HybridDateTime.DateTime < c.HybridDateTime.DateTime
        || (after.HybridDateTime.DateTime == c.HybridDateTime.DateTime && after.HybridDateTime.Counter < c.HybridDateTime.Counter)
        || (after.HybridDateTime.DateTime == c.HybridDateTime.DateTime && after.HybridDateTime.Counter == c.HybridDateTime.Counter && after.Id < c.Id));
    }

    public static IQueryable<T> WhereBefore<T>(this IQueryable<T> queryable, T after) where T : CommitBase
    {
        return queryable.Where(c => c.HybridDateTime.DateTime < after.HybridDateTime.DateTime
        || (c.HybridDateTime.DateTime == after.HybridDateTime.DateTime && c.HybridDateTime.Counter < after.HybridDateTime.Counter)
        || (c.HybridDateTime.DateTime == after.HybridDateTime.DateTime && c.HybridDateTime.Counter == after.HybridDateTime.Counter && c.Id < after.Id));
    }
}
