using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DeDuper
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter the folder path to start: ");
            var path = Console.ReadLine();

            if(!Directory.Exists(path)){
                Console.WriteLine($"{path} is not a valid directory location.");
                Environment.Exit(0);
            }

            var deDuper = new FileDeDuper(path);
            deDuper.IncludeExtensions.AddRange(new[]{
                "gif",
                "jpg",
                "jpeg",
                "img",
                "png",
                "avi",
                "mp4",
                "mov"
            }.ToList());

            var success = deDuper.ProcessFiles(recursive: true);
            var msg = success ? "everything went well" : "something went wrong";
            Console.WriteLine($"The {nameof(deDuper.ProcessFiles)}() method returned {success}, so {msg}.");
            Console.WriteLine();

#if DEBUG
            Console.WriteLine($"The following {deDuper.ProcessedFiles.Count()} file(s) were determined to be distinct.");
            foreach (var entry in deDuper.ProcessedFiles)
            {
                Console.WriteLine($"{entry.Key} - {entry.Value}");
            }
#endif

            Console.WriteLine($"Press <ENTER> to quit.");
            Console.ReadLine();

            Environment.Exit(success ? 0 : 1);
        }
    }

    public class FileDeDuper : IDisposable
    {
        public string RootPath { get; set; }
        public List<string> IncludeExtensions { get; set; }
        public Dictionary<string, string> ProcessedFiles { get; set; }

        private SHA1 _hashAlgorithm;

        public FileDeDuper(string path)
        {
            IncludeExtensions = new List<string>();
            RootPath = path;
            ProcessedFiles = new Dictionary<string, string>();
            _hashAlgorithm = SHA1.Create();
        }

        public bool ProcessFiles(bool recursive = false)
        {
            try
            {
                var dupeFolder = Directory.CreateDirectory(
                    Path.Combine(RootPath, $"DeDupedFiles.{DateTime.Now.ToString("dd-MM-yyyy")}"));

                var rootDir = new DirectoryInfo(RootPath);
                var filesToCheck = rootDir
                    .EnumerateFiles("*", recursive
                        ? SearchOption.AllDirectories
                        : SearchOption.TopDirectoryOnly)
                    .Where(x => IncludeExtensions.Any(e => ("." + e.ToLower()).Equals(
                        x.Extension.ToLower(),
                        StringComparison.CurrentCultureIgnoreCase)))
                    .ToList();

                ProcessedFiles = new Dictionary<string, string>();
                Console.WriteLine("Hashing files...");
                var filesMoved = 0;
                foreach (var currentFile in filesToCheck)
                {
                    var hash = GetHash(currentFile);
                    if (ProcessedFiles.ContainsKey(hash.Key))
                    {
                        filesMoved++;
                        // This file was already processed, so dump this one into the dupes folder
                        var targetPath = Path.Combine(
                            dupeFolder.FullName,
                            $"{Path.GetFileNameWithoutExtension(currentFile.Name)}_{filesMoved}{currentFile.Extension}");
                        Console.WriteLine($"Moving to path: {targetPath}");
                        currentFile.MoveTo(targetPath);
                    }
                    else
                    {
                        // This is a new hash, so it needs added to our list
                        ProcessedFiles.Add(hash.Key, hash.Value);
                    }
                    Console.WriteLine($"Computed hash: {hash.Key} for file: {hash.Value}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception was encountered while processing files: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                return false;
            }
        }

        private KeyValuePair<string, string> GetHash(FileInfo fileInfo)
        {
            var path = fileInfo.FullName;
            var bytes = File.ReadAllBytes(path);
            var hash = _hashAlgorithm.ComputeHash(bytes);
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();
            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < hash.Length; i++)
                sBuilder.Append(hash[i].ToString("x2"));

            return new KeyValuePair<string, string>(sBuilder.ToString(), path);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _hashAlgorithm.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                _hashAlgorithm = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileDeDuper() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion

    }
}
