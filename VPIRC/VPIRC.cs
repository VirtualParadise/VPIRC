using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VPIRC
{
    class VPIRC
    {
        const string tag = "VPIRC";

        public static readonly SettingsManager Settings = new SettingsManager();
        public static readonly VPManager       VP       = new VPManager();
        public static readonly IRCManager      IRC      = new IRCManager();

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
        }

        static void loop()
        {
            while (!exiting)
                VP.Update();
        }

        static void takedown()
        {
            VP.Takedown();
            IRC.Takedown();
        }
        
        public static void Exit()
        {
            exiting = true;
        }
    }
}
