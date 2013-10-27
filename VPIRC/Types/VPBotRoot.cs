using System;

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

        void registerRootEvents()
        {
            this.Connected += onConnected;
        }

        void onConnected()
        {
            Bot.ConsoleBroadcast("", "*** {0} is bridging this world with channel {1}@{2}...", name, VPIRC.IRC.Channel, VPIRC.Settings.IRC["Hostname"]);
        }

    }
}
