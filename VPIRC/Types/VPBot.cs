using System;
using System.Threading.Tasks;
using VP;

namespace VPIRC
{
    class VPBot : IDisposable
    {
        const string tag = "VPBot";

        static Random rand = new Random();

        static float randUnit
        {
            get { return (float) ( rand.NextDouble() * 2 ) - 1; }
        }

        static float randRot
        {
            get { return (float) rand.NextDouble() * 360; }
        }

        public event Action Connected;

        public readonly IRCUser  User;
        public readonly Instance Bot = new Instance();

        protected string name;
        /// <summary>
        /// Gets the name of this bot
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        ConnState state = ConnState.Disconnected;
        /// <summary>
        /// Gets the connection state of this bot
        /// </summary>
        public ConnState State
        {
            get { return state; }
        }

        DateTime lastAttempt = TDateTime.UnixEpoch;
        /// <summary>
        /// Gets the timestamp of the last connection attempt of this bot
        /// </summary>
        public DateTime LastAttempt
        {
            get { return lastAttempt; }
        }

        DateTime lastConnect = TDateTime.UnixEpoch;
        /// <summary>
        /// Gets the timestamp of the last successful connection to this bot
        /// </summary>
        public DateTime LastConnect
        {
            get { return lastConnect; }
        }

        public VPBot(IRCUser user)
        {
            this.User = user;
            this.name = VPIRC.VP.Prefix + user.Name;

            registerEvents();
            Log.Fine(tag, "Created bot instance for IRC user '{0}'", user);
        }

        protected VPBot() { }

        protected void registerEvents()
        {
            Bot.UniverseDisconnect += onDisconnect;
            Bot.WorldDisconnect    += onDisconnect;
        }

        public async void Connect()
        {
            state = ConnState.Connecting;

            await Task.Run( () => {
                var username = VPIRC.Settings.VP["Username"];
                var password = VPIRC.Settings.VP["Password"];
                var botname  = Name;
                lastAttempt  = DateTime.Now;

                try
                {
                    Bot.Login(username, password, botname)
                        .Enter(VPIRC.VP.World)
                        .GoTo(randUnit, 0, randUnit, randRot, 0)
                        .Pump();
                }
                catch (VPException e)
                {
                    switch (e.Reason)
                    {
                        default:
                            Log.Warn(tag, "Bot '{0}' cannot connect: {1}", Name, e.Reason);
                            break;
                    }

                    state = ConnState.Disconnected;
                    return;
                }

                Log.Debug(tag, "Connected bot '{0}'", Name);
                lastConnect = DateTime.Now;
                state       = ConnState.Connected;

                if (Connected != null)
                    Connected();
            });
        }

        void onDisconnect(Instance sender, int error)
        {
            Log.Warn(tag, "{0} lost connection, winsock error {1}", Name, error);
            state = ConnState.Disconnected;
        }

        public void Dispose()
        {
            Connected = null;

            Bot.Dispose();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
