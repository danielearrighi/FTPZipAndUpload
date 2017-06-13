using System;
using System.Collections.Generic;

namespace FTPZipAndUpload.Domain
{
    /// <summary>
    /// Rappresent a single item to upload
    /// </summary>
    public class QueueItem
    {
        //FTP Options
        public string FtpServer { get; set; }
        public string FtpDirectory { get; set; }
        public string FtpUser { get; set; }
        public string FtpPassword { get; set; }

        /// <summary>
        /// Directory to process
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// True = create and upload a ZIP for each subdirectory found in Directory
        /// False = create and upload one ZIP file of the Directory
        /// </summary>
        public bool ProcessSubDirectories { get; set; }

        public string OutputFileName { get; set; }
        public string OutputPath { get; set; }

        public bool Zipped { get; set; }
        public bool Uploaded { get; set; }

        //Options
        public bool AppendDateTime { get; set; }
        public bool DeleteFilesWhenFinished { get; set; }
        public int CleanRemote { get; set; }

        /// <summary>
        /// List of Paths to exclude from zipping
        /// </summary>
        //public List<ExcludedPath> ExcludedFolders { get; set; }

        public List<string> ExcludedFolders { get; set; }

        public List<WorkPath> WorkList { get; set; }

        /// <summary>
        /// Process Queue
        /// </summary>
        public QueueItem()
        {
            WorkList = new List<WorkPath>();
            //ExcludedFolders = new List<ExcludedPath>();
            ExcludedFolders = new List<string>();

            //Defaults
            AppendDateTime = false;
            DeleteFilesWhenFinished = true;
            CleanRemote = 0;
        }

        public bool IsPathExcluded(string currentDirectoryFullName)
        {
            return ExcludedFolders.Exists(x => String.Equals(x, currentDirectoryFullName, StringComparison.InvariantCultureIgnoreCase));
        }

        /*
        public bool IsPathExcluded(string currentDirectoryFullName)
        {
            return ExcludedFolders.Exists(x => x.FullPath.ToLowerInvariant() == currentDirectoryFullName.ToLowerInvariant());
        }
        */
    }

    public static class ProcessQueueExtensions
    {
        /*
        public static string[] ToStringArray(this List<ExcludedPath> items)
        {
            string[] result = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
                result[i] = items[i].FullPath;

            return result;
        }
        */

        public static string[] ToStringArray(this List<string> items)
        {
            string[] result = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
                result[i] = items[i];

            return result;
        }
    }
}
