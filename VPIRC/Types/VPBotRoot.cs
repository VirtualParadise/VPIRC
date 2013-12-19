using System;
using Nexus.Graphics.Colors;
using VP;

namespace VPIRC
{
    class VPBotRoot : VPBot
    {
        const string tag = "VPBot Root";

        public VPBotRoot() : base()
        {
            name = VPIRC.Settings.VP["BridgeNick"] ?? "VP-IRC";

            registerRootEvents();
            registerEvents();

            Log.Fine(tag, "Created root bridge bot instance");
        }

        public void Broadcast(string message, params object[] parts)
        {
            Bot.ConsoleBroadcast(ChatEffect.None, Colors.Info, "", message, parts);
        }

        void registerRootEvents()
        {
            this.Connected += onConnected;
        }

        void onConnected()
        {
            Broadcast("*** {0} is bridging this world with channel {1}@{2}...", name, VPIRC.IRC.Channel, VPIRC.Settings.IRC["Hostname"]);
        }
    }
}
