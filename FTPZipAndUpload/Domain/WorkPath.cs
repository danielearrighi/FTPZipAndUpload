using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPZipAndUpload.Domain
{
    public class WorkPath
    {
        public string FullPath { get; set; }
        public string FullZipPath { get; set; }
        public bool IsZipped { get; set; }
        public bool IsUploaded { get; set; }

        public WorkPath()
        {

        }
    }
}
