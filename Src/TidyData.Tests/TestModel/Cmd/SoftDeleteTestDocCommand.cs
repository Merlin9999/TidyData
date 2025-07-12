 #nullable disable
using System;

namespace TidySyncDB.UnitTests.TestModel.Cmd
{
    public class SoftDeleteTestDocCommand : ICommand<TestDataModel>
    {
        private readonly Guid _idOfDocToDelete;

        public SoftDeleteTestDocCommand(Guid idOfDocToDelete)
        {
            this._idOfDocToDelete = idOfDocToDelete;
        }

        public void Execute(TestDataModel model, CollectionWrapperFactory factory)
        {
            factory.Get(model, x => x.Docs)
                .SoftDelete(this._idOfDocToDelete);
        }
    }
}