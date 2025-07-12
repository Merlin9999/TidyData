 #nullable disable
 using NodaTime;
 using TidyData.Storage;
 using TidyData.Tests._Shared_Synced.TestImpl;
 using TidyUtility.Data.Json;

 namespace TidyData.Tests.Storage.IndexLock
{
    public class FileIndexLockTests
    {
        private const string pathToTestFiles = ".";

        [Fact]
        public async Task LockReadUpdateUnlockLockReadUnlockAsync()
        {
            string fileName = Path.Combine(pathToTestFiles, "LockReadUpdateUnlockLockReadUnlock.json");
            FileIndexLock indexLock = new FileIndexLock(fileName, new JsonDotNetSerializer(), SystemClock.Instance);
            await IndexLockTestsImpl.LockReadUpdateUnlockLockReadUnlockImplAsync(indexLock);
        }

        [Fact]
        public async Task ReadWriteFailsWhenAlreadyLockedAsync()
        {
            string fileName = Path.Combine(pathToTestFiles, "ReadWriteFailsWhenAlreadyLocked.json");
            FileIndexLock indexLock1 = new FileIndexLock(fileName, new JsonDotNetSerializer(), SystemClock.Instance);
            FileIndexLock indexLock2 = new FileIndexLock(fileName, new JsonDotNetSerializer(), SystemClock.Instance);
            await IndexLockTestsImpl.ReadWriteFailsWhenAlreadyLockedImplAsync(indexLock1, indexLock2);
        }
        [Fact]
        public async Task WriteBeforeReadFailAsync()
        {
            string fileName = Path.Combine(pathToTestFiles, "WriteBeforeReadFail.json");
            FileIndexLock indexLock = new FileIndexLock(fileName, new JsonDotNetSerializer(), SystemClock.Instance);
            await IndexLockTestsImpl.WriteBeforeReadFailImplAsync(indexLock);
        }

    }
}
