using System;
using System.Collections.Generic;
using FTPZipAndUpload.Domain;
using FTPZipAndUpload.Infrastructure;
using WinSCP;

namespace FTPZipAndUpload.Services
{
    public static class FtpService
    {
        /// <summary>
        /// Upload the files in the queueItem to the specified FTP server/folder
        /// </summary>
        /// <param name="queueItem"></param>
        /// <param name="verbose"></param>
        /// <param name="ftpFolder"></param>
        /// <param name="ftpServer"></param>
        /// <param name="ftpUser"></param>
        /// <param name="ftpPassword"></param>
        public static bool UploadQueue(QueueItem queueItem, string ftpFolder, string ftpServer, string ftpUser, string ftpPassword, bool verbose = false)
        {
            bool success = true;

            var sessionOptions = new SessionOptions
            {
                //TODO: Support different protocols
                Protocol = Protocol.Ftp,
                HostName = ftpServer,
                UserName = ftpUser,
                Password = ftpPassword
            };

            try
            {
                using (var session = new Session())
                {
                    session.Open(sessionOptions);
                    var transferOptions = new TransferOptions { TransferMode = TransferMode.Binary };

                    session.CreateDirectory(ftpFolder);

                    foreach (var q in queueItem.WorkList)
                    {
                        if (q.IsUploaded)
                            continue;

                        if (verbose) Console.Write("Uploading ZIP File {0}...", q.FullZipPath);

                        var transferResult = session.PutFiles(q.FullZipPath, ftpFolder, false,
                            transferOptions);
                        transferResult.Check();

                        q.IsUploaded = true;

                        if (verbose) Console.Write("Uploaded.");
                        if (verbose) Console.WriteLine();
                    }

                    if (verbose) Console.WriteLine("Uploading complete.");
                }
            }
            catch (SessionRemoteException re)
            {
                if (re.InnerException != null)
                {
                    if (re.InnerException.Message.ToLowerInvariant().IndexOf("file exist", StringComparison.Ordinal) > 0)
                    {
                        //If files exists, change name adding time and try uploading again.
                        UploadQueue(queueItem, ftpFolder + DateTime.Now.ToString("HHmmss") + "/", ftpServer, ftpUser, ftpPassword, verbose);
                    }
                    else
                    {
                        if (verbose) Console.WriteLine();
                        if (verbose) Console.WriteLine("Error in uploading. Check Logs.");
                        Utilities.WriteToFile(string.Format("Upload Exception: {0}\n", re));

                        success = false;
                    }
                }
                else
                {
                    if (verbose) Console.WriteLine();
                    if (verbose) Console.WriteLine("Error in uploading. Check Logs.");
                    Utilities.WriteToFile(string.Format("Upload Exception: {0}\n", re));

                    success = false;
                }
            }
            catch (Exception e)
            {
                if (verbose) Console.WriteLine();
                if (verbose) Console.WriteLine("Error in uploading. Check Logs.");
                Utilities.WriteToFile(string.Format("Upload Exception: {0}\n", e));

                success = false;
            }

            return success;
        }

        /// <summary>
        /// Deletes directory older than X days
        /// </summary>
        /// <param name="days"></param>
        /// <param name="verbose"></param>
        /// <param name="ftpFolder"></param>
        /// <param name="ftpServer"></param>
        /// <param name="ftpUser"></param>
        /// <param name="ftpPassword"></param>
        public static void CleanOldRemoteFolders(int days, bool verbose, string ftpFolder, string ftpServer, string ftpUser, string ftpPassword)
        {
            var sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Ftp,
                HostName = ftpServer,
                UserName = ftpUser,
                Password = ftpPassword
            };

            try
            {
                using (var session = new Session())
                {
                    session.Open(sessionOptions);

                    if (verbose) Console.WriteLine("Cleaning Old Directories.");

                    RemoteDirectoryInfo directory = session.ListDirectory(ftpFolder);
                    foreach (RemoteFileInfo fileInfo in directory.Files)
                    {
                        if (fileInfo.IsDirectory)
                        {
                            //ListDirectory list also parent directory with name = "..", it needs to be skipped.
                            if (fileInfo.Name != "..")
                            {
                                TimeSpan pastDays = DateTime.Now - fileInfo.LastWriteTime;
                                if (pastDays.Days >= days)
                                {
                                    if (verbose) Console.WriteLine("Deleting Directory: {0} ({1}).", fileInfo.Name, fileInfo.LastWriteTime);

                                    //Remove Files
                                    DeleteDirectoryRecursive(session, verbose, ftpFolder + fileInfo.Name);

                                    if (verbose) Console.WriteLine("Done.");
                                }
                            }
                        }
                    }

                    if (verbose) Console.WriteLine("Cleaning complete.");
                }
            }
            catch (Exception e)
            {
                if (verbose) Console.WriteLine();
                if (verbose) Console.WriteLine("Error in cleaning directory. Check Logs.");
                Utilities.WriteToFile(string.Format("Clean Exception: {0}\n", e));
            }
        }

        /// <summary>
        /// Recursive deleting of files and folder.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="verbose"></param>
        /// <param name="currentDirectory"></param>
        private static void DeleteDirectoryRecursive(Session session, bool verbose, string currentDirectory)
        {
            RemoteDirectoryInfo directory = session.ListDirectory(currentDirectory);
            foreach (RemoteFileInfo fileInfo in directory.Files)
            {
                if (fileInfo.IsDirectory)
                {
                    //Skip .. directory
                    if (fileInfo.Name != "..")
                    {
                        DeleteDirectoryRecursive(session, verbose, currentDirectory + "/" + fileInfo.Name);
                    }
                }
                else
                {
                    if (verbose) Console.Write("Deleting {0}/{1}...", currentDirectory, fileInfo.Name);
                    RemovalOperationResult r = session.RemoveFiles(session.EscapeFileMask(currentDirectory + "/" + fileInfo.Name));

                    if (verbose)
                        Console.WriteLine(r.IsSuccess ? "ok." : "fail.");
                }
            }

            //After deleting all files recursive, delete the directory itself.
            if (verbose) Console.Write("Deleting {0}/...", currentDirectory);
            RemovalOperationResult dr = session.RemoveFiles(session.EscapeFileMask(currentDirectory));

            if (verbose)
                Console.WriteLine(dr.IsSuccess ? "ok." : "fail.");
        }
    }
}