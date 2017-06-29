using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTPZipAndUpload.Domain;
using Ionic.Zip;
using Ionic.Zlib;

namespace FTPZipAndUpload.Services
{
    public static class ZipService
    {
        /// <summary>
        /// Zips every file in the queue
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="verbose"></param>
        public static bool ZipFolder(string folder, string destination, string[] exclusionList, bool verbose = false)
        {
            using (var zip = new ZipFile())
            {
                if (verbose)
                    Console.Write("Creating ZIP File for {0}... ", folder);

                //Adding 
                AddFolderToZipRecursive(folder, folder, exclusionList, zip);

                zip.CompressionMethod = CompressionMethod.BZip2;
                zip.CompressionLevel = CompressionLevel.BestSpeed;

                if (!destination.EndsWith(".zip")) destination += ".zip";
                zip.Save(destination);

                if (verbose) Console.Write("Zipped.");
                if (verbose) Console.WriteLine();

               return true;
            }
        }

        /// <summary>
        /// Add all files and folders to a zipFile, exlcuding files and directories in exclusion list.
        /// </summary>
        /// <param name="currentPath"></param>
        /// <param name="startingPath"></param>
        /// <param name="excludedCollection"></param>
        /// <param name="zipFile"></param>
        public static void AddFolderToZipRecursive(string currentPath, string startingPath, string[] exclusionList, ZipFile zipFile)
        {
            var currentZippingDirectory = new DirectoryInfo(currentPath);
            if (!exclusionList.Contains(currentPath))
            {
                //
                foreach (var currentFile in currentZippingDirectory.GetFiles())
                {
                    if (!exclusionList.Contains(currentFile.FullName))
                    {
                        string relativePath = currentZippingDirectory.FullName.Replace(startingPath, "");
                        zipFile.AddFile(currentFile.FullName, relativePath);
                    }
                }
                //
                foreach (var currentDirectory in currentZippingDirectory.GetDirectories())
                {
                    AddFolderToZipRecursive(currentDirectory.FullName, startingPath, exclusionList, zipFile);
                }
            }
        }
    }
}
