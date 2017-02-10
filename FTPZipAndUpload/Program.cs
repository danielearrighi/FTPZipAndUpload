using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using CommandLine;
using Ionic.Zip;
using Ionic.Zlib;
using WinSCP;

namespace FTPZipAndUpload
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Process(args);
        }

        /// <summary>
        /// A flag that determines if the program can clean old directories. 
        /// If something went bad, it will not clean for security purposes.
        /// </summary>
        private static bool _canClean = true;

        /// <summary>
        /// Main process
        /// </summary>
        private static void Process(string[] args)
        {
            Console.WriteLine("FOLDER ZIP AND UPLOAD v1.1");
            Console.WriteLine("by Daniele Arrighi @ Idioblast. daniele@idioblast.it");
            Console.WriteLine();
            Console.WriteLine("Processing...");

            //LOG
            Utilities.WriteToFile(string.Format("{0}: Executing Routine...", DateTime.Now));

            /******************** 
             * DEFAULTS VALUES
            ********************/
            string zipOriginFolder = ConfigurationManager.AppSettings["ZipOriginFolder"];
            string zipDestinationFolder = ConfigurationManager.AppSettings["ZipDestinationFolder"];

            string zipDestinationFileNameBase = ConfigurationManager.AppSettings["ZipDestinationFileNameBase"];

            string ftpFolder = ConfigurationManager.AppSettings["FTPFolder"];
            string ftpServer = ConfigurationManager.AppSettings["FTPServer"];
            string ftpUser = ConfigurationManager.AppSettings["FTPUser"];
            string ftpPassword = ConfigurationManager.AppSettings["FTPPassword"]; //TODO: Ask for Password this is empty and write it directly to App.Config 

            bool appendDateTime = Convert.ToBoolean(ConfigurationManager.AppSettings["AppendDateTime"]);

            bool deleteFile = Convert.ToBoolean(ConfigurationManager.AppSettings["DeleteFile"]);
            int cleanRemoteFoldersDays = Utilities.ToInt(ConfigurationManager.AppSettings["CleanRemote"], 0); //0 = no clean.

            bool verbose = false;

            /******************** 
            * OVERWRITE DEFAULT VALUES WITH COMMAND ARGUMENTS IF PRESENT
            ********************/
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                verbose = options.Verbose;

                if (!string.IsNullOrEmpty(options.InputFolder))
                    zipOriginFolder = options.InputFolder;

                if (!string.IsNullOrEmpty(options.DestinationFolder))
                    zipDestinationFolder = options.DestinationFolder;

                if (!string.IsNullOrEmpty(options.DestinationFileName))
                    zipDestinationFileNameBase = options.DestinationFileName;

                if (!string.IsNullOrEmpty(options.FtpFolder))
                    ftpFolder = options.FtpFolder;

                if (!string.IsNullOrEmpty(options.AppendDateTime))
                    if (Utilities.IsBoolean(options.AppendDateTime))
                        appendDateTime = Convert.ToBoolean(options.AppendDateTime);

                if (!string.IsNullOrEmpty(options.DeleteFile))
                    if (Utilities.IsBoolean(options.DeleteFile))
                        deleteFile = Convert.ToBoolean(options.DeleteFile);

                if (!string.IsNullOrEmpty(options.Password))
                {
                    Console.WriteLine("Encrypted Password is: {0}", Security.EncryptText(options.Password));
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
            }

#if DEBUG
            verbose = true;
#endif

            //Setup Directory Names 
            string nowFileName = DateTime.Now.ToString("yyyyMMdd");

            //Extra checks:
            if (!ftpFolder.EndsWith("/")) ftpFolder += "/";
            //
            if (!zipOriginFolder.EndsWith("\\")) zipOriginFolder += "\\";
            if (!zipDestinationFolder.EndsWith("\\")) zipDestinationFolder += "\\";
            //
            if (String.IsNullOrEmpty(ftpPassword))
            {
                Console.WriteLine("Error. Missing password (Encrypted) for FTP connection");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }
            else
            {
                try
                {
                    ftpPassword = Security.DecryptText(ftpPassword);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error. Wrong password format");
                    Console.WriteLine("Press any key to exit...");

                    _canClean = false;
                }
            }

            //The process queue
            List<ProcessQueue> queue = new List<ProcessQueue>();

            //Create a ZIP file for each subDirectory found
            if (Directory.Exists(zipOriginFolder))
            {
                var theDirectory = new DirectoryInfo(zipOriginFolder);
                foreach (var currentDirectory in theDirectory.GetDirectories())
                {
                    var q = new ProcessQueue
                    {
                        Directory = currentDirectory.FullName,
                        OutputPath = zipDestinationFolder,
                        OutputFileName = currentDirectory.Name + ".zip",
                        Zipped = false,
                        Uploaded = false
                    };

                    queue.Add(q);
                }

                //ZIP ALL FOLDER
                ZipQueue(queue, verbose);

                //FTP UPLOAD ALL FOLDER
                UploadQueue(queue, verbose, ftpFolder + nowFileName + "/", ftpServer, ftpUser, ftpPassword);

                //DELETE FILES
                DeleteFilesInQueue(queue, verbose, deleteFile);

                //DELETE OLDER FOLDERS IF REQUIRED
                //TODO: Use same session in UploadQueue and in CleanOldestRemoteFolders
                if (cleanRemoteFoldersDays > 0 && _canClean)
                    CleanOldestRemoteFolders(cleanRemoteFoldersDays, verbose, ftpFolder, ftpServer, ftpUser, ftpPassword);

                if (verbose)
                {
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadLine();
                }

                //LOG
                Utilities.WriteToFile(string.Format("{0}: Zip & Upload Completed.\n", DateTime.Now));
            }
            else
            {
                if (verbose)
                {
                    Console.WriteLine("Input folder does not exist, please specify a different input folder...");
                    Console.ReadLine();
                }

                //LOG
                Utilities.WriteToFile(string.Format("{0}: Input folder {1} is missing...\n", DateTime.Now, zipOriginFolder));
                Utilities.WriteToFile(string.Format("{0}: Operation Aborted.\n", DateTime.Now));
            }

            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Zips every file in the queue
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="verbose"></param>
        private static void ZipQueue(List<ProcessQueue> queue, bool verbose)
        {
            foreach (var q in queue)
            {
                if (q.Zipped)
                    continue;

                using (var zip = new ZipFile())
                {
                    if (verbose)
                        Console.Write("Creating ZIP File for {0}... ", q.Directory);
                    //zip.StatusMessageTextWriter = System.Console.Out;

                    zip.AddDirectory(q.Directory); //Recursive
                    zip.CompressionMethod = CompressionMethod.BZip2;
                    zip.CompressionLevel = CompressionLevel.BestSpeed;
                    zip.Save(q.OutputPath + q.OutputFileName);

                    if (verbose) Console.Write("Zipped.");
                    if (verbose) Console.WriteLine();

                    q.Zipped = true;
                }
            }

            if (verbose) Console.WriteLine("Zipping complete.");
        }

        /// <summary>
        /// Delete items in queue if they are uploaded and zipped.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="verbose"></param>
        /// <param name="deleteFile"></param>
        private static void DeleteFilesInQueue(List<ProcessQueue> queue, bool verbose, bool deleteFile)
        {
            foreach (var q in queue)
                if (q.Uploaded && q.Zipped)
                    if (deleteFile)
                    {
                        if (verbose) Console.Write("Deleting File {0}...", q.OutputFileName);
                        File.Delete(q.OutputPath + q.OutputFileName);

                        if (verbose) Console.Write("Deleted.");
                        if (verbose) Console.WriteLine();
                    }

            if (verbose) Console.WriteLine("All deleted.");
        }

        /// <summary>
        /// Upload the files in the queue to the specified FTP server/folder
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="verbose"></param>
        /// <param name="ftpFolder"></param>
        /// <param name="ftpServer"></param>
        /// <param name="ftpUser"></param>
        /// <param name="ftpPassword"></param>
        private static void UploadQueue(List<ProcessQueue> queue, bool verbose, string ftpFolder, string ftpServer, string ftpUser, string ftpPassword)
        {
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

                    foreach (var q in queue)
                    {
                        if (q.Uploaded)
                            continue;

                        if (verbose) Console.Write("Uploading ZIP File {0}...", q.OutputFileName);

                        var transferResult = session.PutFiles(q.OutputPath + q.OutputFileName, ftpFolder, false,
                            transferOptions);
                        transferResult.Check();

                        q.Uploaded = true;

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
                        UploadQueue(queue, verbose, ftpFolder + DateTime.Now.ToString("HHmmss") + "/", ftpServer, ftpUser, ftpPassword);
                    }
                    else
                    {
                        if (verbose) Console.WriteLine();
                        if (verbose) Console.WriteLine("Error in uploading. Check Logs.");
                        Utilities.WriteToFile(string.Format("Upload Exception: {0}\n", re));

                        _canClean = false;
                    }
                }
                else
                {
                    if (verbose) Console.WriteLine();
                    if (verbose) Console.WriteLine("Error in uploading. Check Logs.");
                    Utilities.WriteToFile(string.Format("Upload Exception: {0}\n", re));

                    _canClean = false;
                }
            }
            catch (Exception e)
            {
                if (verbose) Console.WriteLine();
                if (verbose) Console.WriteLine("Error in uploading. Check Logs.");
                Utilities.WriteToFile(string.Format("Upload Exception: {0}\n", e));

                _canClean = false;
            }
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
        private static void CleanOldestRemoteFolders(int days, bool verbose, string ftpFolder, string ftpServer, string ftpUser, string ftpPassword)
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