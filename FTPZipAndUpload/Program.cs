using System;
using System.Configuration;
using System.IO;
using CommandLine;
using FTPZipAndUpload.Domain;
using FTPZipAndUpload.Infrastructure;
using FTPZipAndUpload.Services;

namespace FTPZipAndUpload
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Process(args);
        }

        /// <summary>
        /// Main process
        /// </summary>
        private static void Process(string[] args)
        {
            Console.WriteLine("FOLDER ZIP AND UPLOAD v2.0");
            Console.WriteLine("by Daniele Arrighi @ Idioblast. daniele@idioblast.it");
            Console.WriteLine();
            Console.WriteLine("Processing...");

            //LOG
            Utilities.WriteToFile(string.Format("{0}: Executing...", DateTime.Now));

            /******************** 
             * DEFAULTS VALUES
            ********************/
            //ZIPs Temporary Path
            string zipDestinationFolder = ConfigurationManager.AppSettings["ZipDestinationFolder"];
            if (!zipDestinationFolder.EndsWith("\\")) zipDestinationFolder += "\\";

            //Configuration File to Load
            string configurationFile = AppDomain.CurrentDomain.BaseDirectory + "config.xml";

            //Verbosity
            bool verbose = false;

            /******************** 
            * OVERWRITE DEFAULT VALUES WITH COMMAND ARGUMENTS IF PRESENT
            ********************/
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                //Verbosity
                verbose = options.Verbose;

                //ZIPs Temporary Path
                if (!string.IsNullOrEmpty(options.ZipDestinationFolder))
                    zipDestinationFolder = options.ZipDestinationFolder;

                //Configuration File to Load
                if (!string.IsNullOrEmpty(options.ConfigFile))
                    configurationFile = options.ConfigFile;

                //Outputs the encrypted password to save in configuration file.
                if (!string.IsNullOrEmpty(options.Password))
                {
                    Console.WriteLine("Encrypted Password is: {0}", Security.EncryptText(options.Password));
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
            }

            /******************** 
            * CONFIGURATION
            ********************/

            //Checks if configuration files exist
            if (!File.Exists(configurationFile))
            {
                Utilities.WriteToFile(string.Format("Missing Configuration File At {0}", configurationFile));

                Console.WriteLine("Missing Configuration File At {0}", configurationFile);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            //Checks if the Temporary Directory Exist, or create it
            if (!Directory.Exists(zipDestinationFolder))
                Directory.CreateDirectory(zipDestinationFolder);

            //Gets the folders to process from XML file
            ProcessQueue folderQueue = new ProcessQueue(configurationFile);

            //Setup Directory Names 
            string nowFileName = DateTime.Now.ToString("yyyyMMdd");

            /******************** 
            * PROCESS THE QUEUE
            ********************/
            foreach (QueueItem queue in folderQueue.Queue)
            {
                //Extra checks
                if (!queue.FtpDirectory.EndsWith("/")) queue.FtpDirectory += "/";
                if (!queue.Directory.EndsWith("\\")) queue.Directory += "\\";

                //Password check
                if (String.IsNullOrEmpty(queue.FtpPassword))
                {
                    Utilities.WriteToFile("Error. Missing password (Encrypted) for FTP connection");
                    Console.WriteLine("Error. Missing password (Encrypted) for FTP connection");
                    continue;
                }
                else
                {
                    try
                    {
                        queue.FtpPassword = Security.DecryptText(queue.FtpPassword);
                    }
                    catch
                    {
                        Utilities.WriteToFile("Error. Wrong password format");
                        Console.WriteLine("Error. Wrong password format");
                        continue;
                    }
                }

                //ELABORATES ALL THE FOLDERS TO ZIP AND UPLOAD
                if (queue.ProcessSubDirectories)
                {
                    DirectoryInfo startingDirectory = new DirectoryInfo(queue.Directory);
                    foreach (var currentDirectory in startingDirectory.GetDirectories())
                    {
                        if (!queue.IsPathExcluded(currentDirectory.FullName))
                        {
                            WorkPath w = new WorkPath();

                            w.FullPath = currentDirectory.FullName;
                            w.FullZipPath = Path.Combine(zipDestinationFolder, currentDirectory.Name + ".zip");

                            if (queue.AppendDateTime)
                                w.FullZipPath = Path.Combine(zipDestinationFolder,
                                    String.Format("{0:yyyMMddHHmmss}-{1}.zip", DateTime.Now, currentDirectory.Name));

                            w.IsZipped = ZipService.ZipFolder(currentDirectory.FullName, w.FullZipPath,
                                queue.ExcludedFolders.ToStringArray(), verbose);
                            w.IsUploaded = false;

                            queue.WorkList.Add(w);
                        }
                    }
                }
                else
                {
                    //IF SHOULD NOT PROCSS SUBDIRS, ZIP THE DIRECTORY DIRECTLY
                    DirectoryInfo currentDirectory = new DirectoryInfo(queue.Directory);
                    if (!queue.IsPathExcluded(currentDirectory.FullName))
                    {
                        WorkPath w = new WorkPath();

                        w.FullPath = currentDirectory.FullName;
                        w.FullZipPath = Path.Combine(zipDestinationFolder, currentDirectory.Name + ".zip");

                        if (queue.AppendDateTime)
                            w.FullZipPath = Path.Combine(zipDestinationFolder,
                                String.Format("{0:yyyMMddHHmmss}-{1}.zip", DateTime.Now, currentDirectory.Name));

                        w.IsZipped = ZipService.ZipFolder(currentDirectory.FullName, w.FullZipPath,
                            queue.ExcludedFolders.ToStringArray(), verbose);
                        w.IsUploaded = false;

                        queue.WorkList.Add(w);
                    }
                }

                //FTP UPLOAD ALL ELABORATED FOLDER
                bool uploadSuccess = FtpService.UploadQueue(queue, queue.FtpDirectory + nowFileName + "/", queue.FtpServer, queue.FtpUser, queue.FtpPassword, verbose);

                //DELETE OLDER FOLDERS IF REQUIRED
                //TODO: Use same session in UploadQueue and in CleanOldRemoteFolders
                if (queue.CleanRemote > 0 && uploadSuccess)
                    FtpService.CleanOldRemoteFolders(queue.CleanRemote, verbose, queue.FtpDirectory, queue.FtpServer, queue.FtpUser, queue.FtpPassword);

                //DELETE ALL CREATED ZIP FILES
                if (queue.DeleteFilesWhenFinished)
                    FileService.DeleteFilesInQueue(queue, verbose);
            }

            //LOG
            Utilities.WriteToFile(String.Format("Zip & Upload routine completed at: {0}\n", DateTime.Now));

            //SEND MAIL 
            string subject = String.Format("Zip & Upload routine completed at: {0}", DateTime.Now); //Todo: Send more information in the mail body.
            MailService.Send("Zip & Upload Executed", subject, verbose);

            //DONE
            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            } 
        } 
    }
}