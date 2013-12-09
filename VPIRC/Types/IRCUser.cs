namespace VPIRC
{
    public delegate void IRCUserArgs(IRCUser user);

    /// <summary>
    /// Represents an IRC user in an IRC channel with a single identity
    /// </summary>
    public class IRCUser : User
    {
        public IRCUser(string name) : base(name) { }
    }
}
