using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OpenStack.Configuration
{
    public class NullSettings : ISettings
    {
        public event EventHandler SettingsChanged = delegate { };

        public static NullSettings Instance { get; } = new NullSettings();

        public SettingSection GetSection(string sectionName) => null;

        public void AddOrUpdate(string sectionName, SettingItem item) => throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidNullSettingsOperation, nameof(AddOrUpdate)));

        public void Remove(string sectionName, SettingItem item) => throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidNullSettingsOperation, nameof(Remove)));

        public void SaveToDisk() { }

        public IList<string> GetConfigFilePaths() => Enumerable.Empty<string>().ToList();

        public IList<string> GetConfigRoots() => Enumerable.Empty<string>().ToList();
    }
}
