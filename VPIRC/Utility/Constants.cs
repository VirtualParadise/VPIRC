using Nexus.Graphics.Colors;
using System.Text.RegularExpressions;

namespace VPIRC
{
    static class Colors
    {
        public static readonly ColorRgb Info    = new ColorRgb(0, 128, 128);
        public static readonly ColorRgb Private = new ColorRgb(0, 128, 255);
    }

    static class Regexes
    {
        public static readonly Regex IRCNicknameChars = new Regex(@"[^-0-9a-zA-Z_\\\[\]\{\}`|]");
    }
}
