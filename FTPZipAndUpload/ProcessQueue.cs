using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPZipAndUpload
{
    /// <summary>
    /// Rappresent a single item to upload
    /// </summary>
    class ProcessQueue
    {
        public string Directory { get; set; }
        public string OutputFileName { get; set; }
        public string OutputPath { get; set; }
        public bool Zipped { get; set; }
        public bool Uploaded { get; set; }

        /// <summary>
        /// Process Queue
        /// </summary>
        public ProcessQueue()
        {
        }
    }
}
