#nullable disable

using TidyData.Query;

// ReSharper disable CheckNamespace
namespace TidyData.Tests._Shared_Synced.TestModel.Qry;

public record DocGetAllQuery : GetAllQuery<TestDataModel, TestDocument>
{
}

public record DocGetByIdQuery : GetByIdQuery<TestDataModel, TestDocument>
{
}