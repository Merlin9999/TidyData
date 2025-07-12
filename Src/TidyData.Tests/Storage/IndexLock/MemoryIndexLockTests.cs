 #nullable disable
using System.Threading.Tasks;
using NodaTime;
using TidySyncDB.Storage;
using TidyUtility.Serializer;
using Xunit;

namespace TidySyncDB.UnitTests.Storage.IndexLock
{
    public class MemoryIndexLockTests
    {
        private const string pathToTestFiles = ".";

        [Fact]
        public async Task LockReadUpdateUnlockLockReadUnlockAsync()
        {
            string name = "LockReadUpdateUnlockLockReadUnlock";
            IIndexLock indexLock = new MemoryIndexLock(name, new JsonDotNetSerializer(), SystemClock.Instance);
            await IndexLockTestsImpl.LockReadUpdateUnlockLockReadUnlockImplAsync(indexLock);
        }

        [Fact]
        public async Task ReadWriteFailsWhenAlreadyLockedAsync()
        {
            string name = "ReadWriteFailsWhenAlreadyLocked";
            IIndexLock indexLock1 = new MemoryIndexLock(name, new JsonDotNetSerializer(), SystemClock.Instance);
            IIndexLock indexLock2 = new MemoryIndexLock(name, new JsonDotNetSerializer(), SystemClock.Instance);
            await IndexLockTestsImpl.ReadWriteFailsWhenAlreadyLockedImplAsync(indexLock1, indexLock2);
        }

        [Fact]
        public async Task WriteBeforeReadFailAsync()
        {
            string name = "WriteBeforeReadFail";
            IIndexLock indexLock = new MemoryIndexLock(name, new JsonDotNetSerializer(), SystemClock.Instance);
            await IndexLockTestsImpl.WriteBeforeReadFailImplAsync(indexLock);
        }
    }
}
