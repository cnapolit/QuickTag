using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace QuickTag
{
    public class QuickTagSettings : ObservableObject
    {
        public bool AddNewTagToQuickMenu { get; set; } = true;
        public IList<string> Tags { get; set; }
    }

    public class QuickTagSettingsViewModel : ObservableObject, ISettings
    {
        private readonly QuickTag plugin;
        private QuickTagSettings editingClone { get; set; }

        private QuickTagSettings settings;
        public QuickTagSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public QuickTagSettingsViewModel(QuickTag plugin)
        {
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<QuickTagSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new QuickTagSettings();
            }

            if (Settings.Tags == null) 
                Settings.Tags = new List<string>();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}