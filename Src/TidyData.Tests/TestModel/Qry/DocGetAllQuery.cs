 #nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using TidySyncDB.Query;

namespace TidySyncDB.UnitTests.TestModel.Qry
{
    public record DocGetAllQuery : GetAllQuery<TestDataModel, TestDocument>
    {
    }

    public record DocGetByIdQuery : GetByIdQuery<TestDataModel, TestDocument>
    {
    }
}