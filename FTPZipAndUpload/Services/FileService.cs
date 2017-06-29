using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTPZipAndUpload.Domain;

namespace FTPZipAndUpload.Services
{
    public static class FileService
    {
        /// <summary>
        /// Delete items in queueItem if they are uploaded and zipped.
        /// </summary>
        /// <param name="queueItem"></param>
        /// <param name="verbose"></param>
        /// <param name="deleteFile"></param>
        public static void DeleteFilesInQueue(QueueItem queueItem, bool verbose = false)
        {
            foreach (var q in queueItem.WorkList)
            {
                if (q.IsUploaded || q.IsZipped)
                {
                    if (verbose) Console.Write("Deleting File {0}...", q.FullZipPath);
                    //File.Delete(q.OutputPath + q.OutputFileName);
                    File.Delete(q.FullZipPath);

                    if (verbose) Console.Write("Deleted.");
                    if (verbose) Console.WriteLine();
                }
            }

            if (verbose) Console.WriteLine("All deleted.");
        }
    }
}
