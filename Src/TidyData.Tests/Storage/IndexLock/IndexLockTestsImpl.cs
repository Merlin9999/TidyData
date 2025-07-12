 #nullable disable
 using FluentAssertions;
 using TidyData.Storage;

 namespace TidyData.Tests.Storage.IndexLock
{
    public static class IndexLockTestsImpl
    {
        public static async Task LockReadUpdateUnlockLockReadUnlockImplAsync(IIndexLock indexLock)
        {
            DBStorageIndex initialEmptyIndex;
            DBStorageIndex updatedIndex;
            DBStorageIndex rereadUpdatedIndex;
            
            const string snapshotName = "World Peace";
            var expected = new List<string>() { snapshotName };

            try
            {
                ((dynamic) indexLock).DeleteAsync();

                initialEmptyIndex = await indexLock.ReadAndLockAsync();
                updatedIndex = new DBStorageIndex(cloneFrom: initialEmptyIndex,
                    dbSnapshotNameLog: initialEmptyIndex.DBSnapshotNameLog.Add(snapshotName));
                await indexLock.UpdateAndReleaseLockAsync(updatedIndex);

                rereadUpdatedIndex = await indexLock.ReadAndLockAsync();
            }
            finally
            {
                await indexLock.ReleaseLockAsync();
                ((dynamic)indexLock).DeleteAsync();
            }

            initialEmptyIndex.DBSnapshotNameLog.Should().BeEmpty();
            updatedIndex.DBSnapshotNameLog.Should().BeEquivalentTo(expected);
            rereadUpdatedIndex.DBSnapshotNameLog.Should().BeEquivalentTo(expected);
        }

        public static async Task ReadWriteFailsWhenAlreadyLockedImplAsync(IIndexLock indexLock1, IIndexLock indexLock2)
        {
            const string snapshotName = "Universal Peace";
            var expected = new List<string>() { snapshotName };

            try
            {
                ((dynamic)indexLock1).DeleteAsync();

                DBStorageIndex outerDBStorageIndex = await indexLock1.ReadAndLockAsync();

                Func<Task> asyncAction = async () =>
                {
                    DBStorageIndex innerDBStorageIndex = await indexLock2.ReadAndLockAsync();
                    await indexLock1.UpdateAndReleaseLockAsync(outerDBStorageIndex);
                    await indexLock2.UpdateAndReleaseLockAsync(innerDBStorageIndex);
                };

                await asyncAction.Should().ThrowAsync<StorageConcurrencyException>();
            }
            finally
            {
                await indexLock1.ReleaseLockAsync();
                await indexLock2.ReleaseLockAsync();
                ((dynamic)indexLock1).DeleteAsync();
            }
        }

        public static async Task WriteBeforeReadFailImplAsync(IIndexLock indexLock)
        {
            DBStorageIndex dbStorageIndex = new DBStorageIndex();

            try
            {
                ((dynamic)indexLock).DeleteAsync();

                Func<Task> asyncAction = async () =>
                {
                    await indexLock.UpdateAndReleaseLockAsync(dbStorageIndex);
                };

                await asyncAction.Should().ThrowAsync<InvalidOperationException>().WithMessage($"Must call the {nameof(IIndexLock.ReadAndLockAsync)}() method " +
                    $"before calling {nameof(IIndexLock.UpdateAndReleaseLockAsync)}().");
            }
            finally
            {
                await indexLock.ReleaseLockAsync();
                ((dynamic)indexLock).DeleteAsync();
            }
        }
    }
}