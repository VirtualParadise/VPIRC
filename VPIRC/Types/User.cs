namespace VPIRC
{
    public delegate void UserArgs(User user);

    public class User
    {
        public string Name;
        public bool   Muted     = false;
        public int    Instances = 1;

        public readonly Side Side;

        public User(string name, Side side)
        {
            this.Name = name;
            this.Side = side;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum Side
    {
        VirtualParadise,
        IRC
    }

    public enum Direction
    {
        Entering,
        Leaving
    }
}
