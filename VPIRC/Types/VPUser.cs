using System.Collections.Generic;

namespace VPIRC
{
    public delegate void VPUserArgs(VPUser user);

    /// <summary>
    /// Represents a single or multiple Virtual Paradise user sessions of the same
    /// identity in a world
    /// </summary>
    public class VPUser : User
    {
        public List<int> Sessions = new List<int>();

        public VPUser(string name) : base(name) { }
    }
   
}
