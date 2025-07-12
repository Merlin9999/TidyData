 #nullable disable
 using TidyData.Query;

 namespace TidyData.Tests.TestModel.Qry
{
    public record DocGetAllQuery : GetAllQuery<TestDataModel, TestDocument>
    {
    }

    public record DocGetByIdQuery : GetByIdQuery<TestDataModel, TestDocument>
    {
    }
}