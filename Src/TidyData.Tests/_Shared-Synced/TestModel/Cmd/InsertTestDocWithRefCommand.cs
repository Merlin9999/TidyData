#nullable disable

// ReSharper disable CheckNamespace
namespace TidyData.Tests._Shared_Synced.TestModel.Cmd;

public class InsertTestDocWithRefCommand : ICommand<TestDataModel>
{
    private readonly TestDocumentWithForeignKey _docToInsert;

    public InsertTestDocWithRefCommand(TestDocumentWithForeignKey docToInsert)
    {
        this._docToInsert = docToInsert;
    }

    public void Execute(TestDataModel model, CollectionWrapperFactory factory)
    {
        factory.Get(model, x => x.DocsWithForeignKey)
            .Insert(this._docToInsert);
    }
}