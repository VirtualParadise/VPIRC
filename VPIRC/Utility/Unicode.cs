using System.Collections.Generic;
using System.Text;

namespace VPIRC
{
    static class Unicode
    {
        public static string FixFromDefault(string incoming)
        {
            var bytes = Encoding.Default.GetBytes(incoming); 
  
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
