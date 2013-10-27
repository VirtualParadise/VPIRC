using System;
using System.Threading.Tasks;
using VP;
using Nexus;

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

        public VPBot(string name)
        {
            this.name = VPIRC.VP.Prefix + name;

            registerDisconnect();
            Log.Fine(tag, "Created bot instance for user '{0}'", Name);
        }

        protected VPBot() { }

        protected void registerDisconnect()
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
            });
        }

        void onDisconnect(Instance sender, int error)
        {
            Log.Warn(tag, "Lost connection, winsock error {1}", Name, error);
            state = ConnState.Disconnected;
        }

        public void Dispose()
        {
            Bot.Dispose();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
