namespace VPIRC
{
    /// <summary>
    /// Represents an abstract user presence in any protocol
    /// </summary>
    public abstract class User
    {
        public string Name;
        public bool   Muted = false;

        public User(string name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
