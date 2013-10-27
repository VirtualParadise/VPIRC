using System;

namespace VPIRC
{
    class VPBotRoot : VPBot
    {
        const string tag = "VPBot Root";

        public VPBotRoot() : base()
        {
            name = VPIRC.Settings.VP["BridgeNick"] ?? "VP-IRC";

            registerDisconnect();
            Log.Fine(tag, "Created root bridge bot instance");
        }

    }
}
