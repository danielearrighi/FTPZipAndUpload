using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTPZipAndUpload.Infrastructure;

namespace FTPZipAndUpload.Domain
{
    public class ProcessQueue
    {
        private string _configurationFilePath;

        public List<QueueItem> Queue { get; set; }

        public ProcessQueue(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;
            Queue = Serializer.Load<List<QueueItem>>(_configurationFilePath);
        }
    }
}
