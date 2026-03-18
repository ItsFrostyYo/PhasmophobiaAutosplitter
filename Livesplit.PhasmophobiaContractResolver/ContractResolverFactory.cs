using System;
using System.Reflection;
using LiveSplit.Model;
using LiveSplit.UI.Components;

namespace LiveSplit.PhasmophobiaContractResolver
{
    public class ContractResolverFactory : IComponentFactory
    {
        public string ComponentName => "Contract Resolver";
        public string Description => "Shows Cursed Possession and Bone Location for the current contract.";
        public ComponentCategory Category => ComponentCategory.Information;

        public IComponent Create(LiveSplitState state) => new ContractResolverComponent(state);

        public string UpdateName => ComponentName;
        public string UpdateURL => "https://raw.githubusercontent.com/ItsFrostyYo/PhasmophobiaAutosplitter/main/";
        public string XMLURL => UpdateURL + "Components/Phasmophobia.Updates.xml";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    }
}
