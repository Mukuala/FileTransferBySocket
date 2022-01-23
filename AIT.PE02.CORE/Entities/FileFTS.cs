using System;
using System.Collections.Generic;
using System.Text;

namespace AIT.PE02.Server.Core.Entities
{
    public class FileFTS
    {
        public string Name { get; set; }
        public string Fullpath { get; set; }
        public long Filesize { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
