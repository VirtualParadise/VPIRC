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

        public static string[] ChunkByByteLimit(string incoming, int byteLimit)
        {
            var length = incoming.Length;
            var chunks = new List<string>();
            var chunk  = "";

            for (var i = 0; i < length; i++)
            {
                chunk += incoming[i];

                if ( Encoding.UTF8.GetByteCount(chunk) >= byteLimit )
                {
                    chunks.Add(chunk);
                    chunk = "";
                }
            }

            if ( !string.IsNullOrWhiteSpace(chunk) )
                chunks.Add(chunk);

            return chunks.ToArray();
        }
    }
}
