using LiveSplit.PhasmophobiaAutosplitter;
using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Reflection;

namespace LiveSplit.PhasmophobiaAutosplitter {
    public class Factory : IComponentFactory {
        private static string FullVersion => ExAssembly.GetName().Version.ToString(4);
        private static string RevisionTag => "r" + ExAssembly.GetName().Version.Revision;

        public string ComponentName => "Phasmophobia Autosplitter v" + FullVersion + " (" + RevisionTag + ")";

        public string Description => "Autosplitter for Phasmophobia (v" + FullVersion + ", " + RevisionTag + ")";

        public ComponentCategory Category => ComponentCategory.Control;

        public string UpdateName => "Phasmophobia Autosplitter";

        public string XMLURL => UpdateURL + "Components/Phasmophobia.Updates.xml";

        public string UpdateURL => "https://raw.githubusercontent.com/ItsFrostyYo/PhasmophobiaAutosplitter/main/";

        public Version Version => ExAssembly.GetName().Version;

        public IComponent Create(LiveSplitState state) => new PhasmophobiaComponent(state);

        public static Assembly ExAssembly = Assembly.GetExecutingAssembly();
    }
}

