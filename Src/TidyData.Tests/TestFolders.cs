 #nullable disable
 namespace TidyData.Tests
 {
     public static class TestFolders
     {
         private static volatile string _solutionFolder;
         private static volatile string _repoFolder;
         private static volatile string _azuriteFolder;

         public static string AzuriteFolder => _azuriteFolder ??= Path.Combine(RepoFolder, "Azurite");

         public static string RepoFolder => _repoFolder ??= GetRepoFolder();

         public static string SolutionFolder => _solutionFolder ??= GetSolutionFolder();

         private static string GetRepoFolder()
         {
             string solutionFolder = SolutionFolder;
             if (solutionFolder == null)
                 return null;

             return Path.GetDirectoryName(solutionFolder);
         }

         private static string GetSolutionFolder()
         {
             string path = Directory.GetCurrentDirectory();

             while (path != null)
             {
                 if (Directory.EnumerateFiles(path, "*.sln").Any())
                     return path;
                 path = Path.GetDirectoryName(path);
             }

             return null;
         }
     }
 }
