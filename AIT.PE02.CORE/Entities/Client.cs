using System;
using System.Collections.Generic;
using System.Text;

namespace AIT.PE02.Server.Core.Entities
{
    public class Client
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string CurrentMap { get; set; }
    }
}
