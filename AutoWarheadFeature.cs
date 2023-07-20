using Compendium.Features;
using PluginAPI.Core.Attributes;

namespace AutoWarhead {
    public class AutoWarheadFeature : ConfigFeatureBase {
        public override string Name => "Auto Warhead";
        public override bool IsPatch => true;

        public override void Load() {
            base.Load();
            AutoWarheadLogic.TimerInit();
            AutoWarheadLogic.IsEnabled = AutoWarheadLogic.DefaultEnabled;
        }
    }
}