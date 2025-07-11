 #nullable disable
 using NodaTime;
 using TidyUtility.Data.Json;

 namespace TidyData.SnapshotLog
{
    public class FileSnapshotLog<T> : AbstractSnapshotLog<T>
        where T : new()
    {
        private readonly string _pathToSnapshotLogFolder;

        public FileSnapshotLog(SnapshotLogSettings snapshotLogSettings, string pathToSnapshotLogFolder, ISerializer serializer = null, IClock clock = null)
            : base(snapshotLogSettings, serializer, clock)
        {
            this._pathToSnapshotLogFolder = pathToSnapshotLogFolder;
        }

        protected override async Task<string> SaveNewSnapshotAsync(T instanceToSave)
        {
            string snapshotName = this.BuildSnapshotName();
            string fileNameOnly = this.BuildFileName(snapshotName);
            string fileName = Path.Combine(this._pathToSnapshotLogFolder, fileNameOnly);
            string serializedData = this.Serializer.Serialize(instanceToSave);
            using (StreamWriter writer = File.CreateText(fileName))
            {
                await writer.WriteAsync(serializedData);
            }

            return snapshotName;
        }

        public override async Task<T> LoadSnapshotAsync(string snapshotName)
        {
            try
            {
                string fileName = Path.Combine(this._pathToSnapshotLogFolder, this.BuildFileName(snapshotName));
                
                string serializedData;
                using (StreamReader reader = File.OpenText(fileName))
                {
                    serializedData = await reader.ReadToEndAsync();
                }

                return this.Serializer.Deserialize<T>(serializedData);
            }
            catch (FileNotFoundException fnf)
            {
                throw new SnapshotNotFoundException(snapshotName, fnf);
            }
        }

        public override Task DeleteAsync(string snapshotName)
        {
            this.DeleteFile(this.BuildFileName(snapshotName));
            return Task.CompletedTask;
        }

        public override async Task DeleteAllAsync()
        {
            IEnumerable<string> savedFileNames = await this.GetSavedFilesAsync();
            foreach (string fileName in savedFileNames)
                this.DeleteFile(fileName);
        }

        public override async Task DeleteAllEligibleForAutoDeletionAsync()
        {
            IEnumerable<string> snapshotsEligibleForDeletion = await this.GetAllSnapshotNamesEligibleForAutoDeletionAsync();

            foreach (string snapshotName in snapshotsEligibleForDeletion)
                this.DeleteFile(this.BuildFileName(snapshotName));
        }

        public override async Task<IEnumerable<string>> GetSavedSnapshotNamesAsync()
        {
            return (await this.GetSavedFilesAsync())
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
        }

        private void DeleteFile(string fileName)
        {
            string fileNameToDelete = Path.Combine(this._pathToSnapshotLogFolder, fileName);
            File.Delete(fileNameToDelete);
        }

        private Task<IEnumerable<string>> GetSavedFilesAsync()
        {
            IEnumerable<string> filesAtPath = 
                Directory.EnumerateFiles(this._pathToSnapshotLogFolder, this.SnapshotLogName + "*");
            return Task.FromResult(this.GetFilesMatchingSnapshotPattern(filesAtPath));
        }

        private IEnumerable<string> GetFilesMatchingSnapshotPattern(IEnumerable<string> fileNames)
        {
            return fileNames
                .Select(fileName => new
                {
                    FileName = fileName,
                    SnapshotName = Path.GetFileNameWithoutExtension(fileName),
                })
                .Where(x => this.IsSnapshotNameMatch(x.SnapshotName))
                .Select(x => x.FileName);
        }

        private string BuildFileName(string snapshotName)
        {
            return $"{snapshotName}{this.Settings.FileExtension}";
        }
    }
}
