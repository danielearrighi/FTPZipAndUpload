using CommandLine;
using CommandLine.Text;

namespace FTPZipAndUpload.Domain
{
    /// <summary>
    /// Defines the options available in command line
    /// </summary>
    class Options
    {
        [Option('z', "zipto", Required = false, HelpText = "ZIP Temp Folder: -o C:/backup/")]
        public string ZipDestinationFolder { get; set; }

        [Option('c', "config", Required = false, HelpText = "Configuration XML file")]
        public string ConfigFile { get; set; }

        [Option('h', "help", Required = false, HelpText = "Get help: -h")]
        public bool Help { get; set; }

        [Option('b', "verbose", Required = false, DefaultValue = true, HelpText = "Verbose Output: -b")]
        public bool Verbose { get; set; }

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
