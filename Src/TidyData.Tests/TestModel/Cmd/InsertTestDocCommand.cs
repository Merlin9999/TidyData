 #nullable disable
using System;
using System.Collections.Generic;
using TidyData;

namespace TidySyncDB.UnitTests.TestModel.Cmd
{
    public class InsertTestDocCommand : ICommand<TestDataModel>
    {
        private readonly TestDocument _docToInsert;

        public InsertTestDocCommand(TestDocument docToInsert)
        {
            this._docToInsert = docToInsert;
        }

        public void Execute(TestDataModel model, CollectionWrapperFactory factory)
        {
            factory.Get(model, x => x.Docs)
                .Insert(this._docToInsert);
        }
    }
}