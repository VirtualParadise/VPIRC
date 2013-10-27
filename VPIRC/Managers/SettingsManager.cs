using Args;
using IniParser;
using System;
using System.ComponentModel;
using System.IO;

namespace VPIRC
{
    class SettingsManager
    {
        const string tag = "Settings";

        public KeyDataCollection VP;
        public KeyDataCollection IRC;

        IniData   ini;
        VPIRCArgs arguments;

        public void Setup(string[] args)
        {
            arguments = Args.Configuration.Configure<VPIRCArgs>().CreateAndBind(args);

            Log.Level = arguments.LogLevel;
            Log.Debug(tag, "Log level set to {0}", Log.Level);

            if ( File.Exists(arguments.Ini) )
                ini = new FileIniDataParser().LoadFile(arguments.Ini);
            else
                ini = new IniData();

            VP  = ini["VirtualParadise"];
            IRC = ini["IRC"];
        }
    }

    class VPIRCArgs
    {
        [Description("Overrides the ini file to be used")]
        [DefaultValue("Settings.ini")]
        [ArgsMemberSwitch("ini", "i")]
        public string Ini { get; set; }   

        [Description("Sets the logging level")]
        [DefaultValue(LogLevels.Production)]
        [ArgsMemberSwitch("loglevel", "l")]
        public LogLevels LogLevel { get; set; }
    }
}
