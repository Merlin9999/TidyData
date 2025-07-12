 #nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using TidyData;

namespace TidySyncDB.UnitTests.Helpers
{
    public static class SyncDBTestHelper
    {
        static SyncDBTestHelper()
        {
            // So IDBDocument implementations that are records (which implicitly override Equals()) do not use the Equals() override
            // for equivalency tests. This is required to allow us to ignore the Version property which is updated by the DB Sync System.
            AssertionOptions.AssertEquivalencyUsing(cfg => cfg
                .ComparingByMembers<IDBDocument>()
                // Automatically exclude the IDBDocument.Version property which doesn't compare properly after serialization AND is modified
                // independently by the system to different values on the client and the server, making it problematic.
                .Excluding(x => x.DeclaringType.IsAssignableTo(typeof(IDBDocument)) &&
                    x.Type == typeof(DocumentVersion))
            );

        }

        public static void InitDBDocumentEquivalencyDefaults()
        {
            // Do Nothing but guarantee that the static constructor is called once, which it automatically will.
        }
    }
}
