using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPZipAndUpload
{
    /// <summary>
    /// Defines the options available in command line
    /// </summary>
    class Options
    {
        [Option('i', "input", Required = false, HelpText = "Folder to compress: -i C:/wwwhost/")]
        public string InputFolder { get; set; }

        [Option('o', "output", Required = false, HelpText = "ZIP Save Folder: -o C:/backup/")]
        public string DestinationFolder { get; set; }

        [Option('n', "name", Required = false, HelpText = "File name: -n TheZIP.zip")]
        public string DestinationFileName { get; set; }

        [Option('a', "append", Required = false, HelpText = "Append time to filename: -a true")]
        public string AppendDateTime { get; set; }

        [Option('f', "ftp-folder", Required = false, HelpText = "FTP folder: -f FTP/wwwhost/")]
        public string FtpFolder { get; set; }

        [Option('h', "help", Required = false, HelpText = "Get help: -h")]
        public bool Help { get; set; }

        [Option('b', "verbose", Required = false, DefaultValue=false, HelpText = "Verbose Output: -b")]
        public bool Verbose { get; set; }

        [Option('d', "delete", Required = false, HelpText = "Delete ZIP file after uploading: -d")]
        public string DeleteFile { get; set; }

        [Option('c', "clean-remote", Required = false, HelpText = "Clean remote folders older than set days after uploading: -c 30")]
        public string CleanRemote { get; set; }

        [Option('p', "password", Required = false, HelpText = "Ouputs the encrypted password: -p 'Password' -> [Encrypted Password Key]")]
        public string Password { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
