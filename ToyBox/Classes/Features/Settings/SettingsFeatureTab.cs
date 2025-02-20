﻿namespace ToyBox.Features.SettingsFeature;
public partial class SettingsFeatureTab : FeatureTab {
    [LocalizedString("ToyBox_Features_SettingsFeature_SettingsFeatureTab_SettingsText", "Settings")]
    public override partial string Name { get; }
    public SettingsFeatureTab() {
        Features.Add(new PerformanceEnhancementFeatures());
        Features.Add(new LanguagePickerFeature());
    }
}
