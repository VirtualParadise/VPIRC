using System;

namespace VPIRC
{
    class VPIRC
    {
        const string tag = "VPIRC";

        public static readonly SettingsManager Settings = new SettingsManager();
        public static readonly VPManager       VP       = new VPManager();
        public static readonly IRCManager      IRC      = new IRCManager();
        public static readonly BridgeManager   Bridge   = new BridgeManager();

        static bool exiting;

        static void Main(string[] args)
        {
            Console.WriteLine("### VPIRC is starting ({0})", DateTime.Now);

            setup(args);

            if (exiting) goto exit;
            else         loop();

        exit:
            takedown();
            Console.WriteLine("### VPIRC is finished ({0})", DateTime.Now);
        }

        static void setup(string[] args)
        {
            Log.QuickSetup();

            Settings.Setup(args);
            VP.Setup();
            IRC.Setup();
            Bridge.Setup();
        }

        static void loop()
        {
            while (!exiting)
            {
                VP.Update();
                IRC.Update();
            }
        }

        static void takedown()
        {
            Bridge.Takedown();
            VP.Takedown();
            IRC.Takedown();
        }
        
        public static void Exit()
        {
            exiting = true;
        }
    }
}
