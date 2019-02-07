using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCModGetter.Classes
{
    /// <summary>
    /// Data Model representing typical data about an MC Mod.
    /// </summary>
    public class Mod
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public bool IsClientSide { get; set; }
        public bool IsOnLocalMachine { get; set; }
    }
}
