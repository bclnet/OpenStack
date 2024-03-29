using System;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace OpenStack.Configuration
{
    /// <summary>
    /// A CredentialsItem has a name and it can have between 2 or 3 children:
    ///     - [Required] Username (AddItem)
    ///     - [Required] Either Password or ClearTextPassword (AddItem)
    ///     - [Optional] ValidAuthenticationTypes (AddItem)
    /// </summary>
    public sealed class CredentialsItem : SettingItem
    {
        string _elementName;
        public override string ElementName
        {
            get => XmlConvert.DecodeName(_elementName);
            protected set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "PropertyCannotBeNullOrEmpty", nameof(ElementName)));
                _elementName = XmlUtility.GetEncodedXMLName(value);
            }
        }

        public string Username
        {
            get => _username.Value;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "PropertyCannotBeNullOrEmpty", nameof(Username)));
                _username.Value = value;
            }
        }

        public bool IsPasswordClearText { get; private set; }

        public string Password => _password.Value;

        public void UpdatePassword(string password, bool isPasswordClearText = true)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("ArgumentCannotBeNullOrEmpty", nameof(password));

            if (IsPasswordClearText && !isPasswordClearText) _password.UpdateAttribute(ConfigurationConstants.KeyAttribute, ConfigurationConstants.PasswordToken);
            else if (!IsPasswordClearText && isPasswordClearText) _password.UpdateAttribute(ConfigurationConstants.KeyAttribute, ConfigurationConstants.ClearTextPasswordToken);

            IsPasswordClearText = isPasswordClearText;
            if (!string.Equals(Password, password, StringComparison.Ordinal)) _password.Value = password;
        }

        public string ValidAuthenticationTypes
        {
            get => _validAuthenticationTypes?.Value;
            set
            {
                if (string.IsNullOrEmpty(value)) _validAuthenticationTypes = null;
                else
                {
                    if (_validAuthenticationTypes == null)
                    {
                        _validAuthenticationTypes = new AddItem(ConfigurationConstants.ValidAuthenticationTypesToken, value);
                        if (Origin != null) _validAuthenticationTypes.SetOrigin(Origin);
                    }
                    else _validAuthenticationTypes.Value = value;
                }
            }
        }

        protected override bool CanHaveChildren => true;

        public override bool IsEmpty() => string.IsNullOrEmpty(Username) && string.IsNullOrEmpty(Password);

        internal readonly AddItem _username;

        internal readonly AddItem _password;

        internal AddItem _validAuthenticationTypes { get; set; }

        public CredentialsItem(string name, string username, string password, bool isPasswordClearText, string validAuthenticationTypes) : base()
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("ArgumentCannotBeNullOrEmpty", nameof(name));
            else if (string.IsNullOrEmpty(username)) throw new ArgumentException("ArgumentCannotBeNullOrEmpty", nameof(username));
            else if (string.IsNullOrEmpty(password)) throw new ArgumentException("ArgumentCannotBeNullOrEmpty", nameof(password));

            ElementName = name;
            _username = new AddItem(ConfigurationConstants.UsernameToken, username);
            var passwordKey = isPasswordClearText ? ConfigurationConstants.ClearTextPasswordToken : ConfigurationConstants.PasswordToken;
            _password = new AddItem(passwordKey, password);
            IsPasswordClearText = isPasswordClearText;
            if (!string.IsNullOrEmpty(validAuthenticationTypes)) _validAuthenticationTypes = new AddItem(ConfigurationConstants.ValidAuthenticationTypesToken, validAuthenticationTypes);
        }

        internal CredentialsItem(XElement element, SettingsFile origin) : base(element, origin)
        {
            ElementName = element.Name.LocalName;

            var elementDescendants = element.Elements();
            var countOfDescendants = elementDescendants.Count();
            var parsedItems = elementDescendants.Select(e => SettingFactory.Parse(e, origin) as AddItem).Where(i => i != null);
            foreach (var item in parsedItems)
            {
                if (string.Equals(item.Key, ConfigurationConstants.UsernameToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (_username != null) throw new OpenStackConfigurationException(string.Format(CultureInfo.CurrentCulture, "UserSettingsUnableToParseConfigFile", "ErrorMoreThanOneUsername", origin.ConfigFilePath));
                    _username = item;
                }
                else if (string.Equals(item.Key, ConfigurationConstants.PasswordToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (_password != null) throw new OpenStackConfigurationException(string.Format(CultureInfo.CurrentCulture, "UserSettingsUnableToParseConfigFile", "ErrorMoreThanOnePassword", origin.ConfigFilePath));
                    _password = item;
                    IsPasswordClearText = false;
                }
                else if (string.Equals(item.Key, ConfigurationConstants.ClearTextPasswordToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (_password != null) throw new OpenStackConfigurationException(string.Format(CultureInfo.CurrentCulture, "UserSettingsUnableToParseConfigFile", "Error_MoreThanOnePassword", origin.ConfigFilePath));
                    _password = item;
                    IsPasswordClearText = true;
                }
                else if (string.Equals(item.Key, ConfigurationConstants.ValidAuthenticationTypesToken, StringComparison.OrdinalIgnoreCase))
                {
                    if (_validAuthenticationTypes != null) throw new OpenStackConfigurationException(string.Format(CultureInfo.CurrentCulture, "UserSettingsUnableToParseConfigFile", "ErrorMoreThanOneValidAuthenticationTypes", origin.ConfigFilePath));
                    _validAuthenticationTypes = item;
                }
            }

            if (_username == null || _password == null) throw new OpenStackConfigurationException(string.Format(CultureInfo.CurrentCulture, "UserSettingsUnableToParseConfigFile", "CredentialsItemMustHaveUsernamePassword", origin.ConfigFilePath));
        }

        public override SettingBase Clone()
        {
            var newSetting = new CredentialsItem(ElementName, Username, Password, IsPasswordClearText, ValidAuthenticationTypes);
            if (Origin != null) newSetting.SetOrigin(Origin);
            foreach (var attr in Attributes) newSetting.AddAttribute(attr.Key, attr.Value);
            return newSetting;
        }

        internal override XNode AsXNode()
        {
            if (Node is XElement) return Node;

            var element = new XElement(_elementName,
                _username.AsXNode(),
                _password.AsXNode());
            if (_validAuthenticationTypes != null) element.Add(_validAuthenticationTypes.AsXNode());
            foreach (var attr in Attributes) element.SetAttributeValue(attr.Key, attr.Value);
            return element;
        }

        public override bool Equals(object other)
            => !(other is CredentialsItem item)
                ? false
                : ReferenceEquals(this, item) ? true : string.Equals(ElementName, item.ElementName, StringComparison.Ordinal);

        public override int GetHashCode() => ElementName.GetHashCode();

        /// <remarks>
        /// This method is internal because it updates directly the xElement behind this abstraction.
        /// It should only be called whenever the underlaying config file is intended to be changed.
        /// To persist changes to disk one must save the corresponding setting files
        /// </remarks>
        internal override void Update(SettingItem other)
        {
            base.Update(other);

            var credentials = other as CredentialsItem;

            if (!string.Equals(Username, credentials.Username, StringComparison.Ordinal)) _username.Update(credentials._username);

            if (!string.Equals(Password, credentials.Password, StringComparison.Ordinal) || IsPasswordClearText != credentials.IsPasswordClearText)
            {
                _password.Update(credentials._password);
                IsPasswordClearText = credentials.IsPasswordClearText;
            }

            if (!string.Equals(ValidAuthenticationTypes, credentials.ValidAuthenticationTypes, StringComparison.Ordinal))
            {
                if (_validAuthenticationTypes == null)
                {
                    _validAuthenticationTypes = new AddItem(ConfigurationConstants.ValidAuthenticationTypesToken, credentials.ValidAuthenticationTypes);
                    _validAuthenticationTypes.SetNode(_validAuthenticationTypes.AsXNode());

                    if (Origin != null) _validAuthenticationTypes.SetOrigin(Origin);


                    var element = Node as XElement;
                    if (element != null) XElementUtility.AddIndented(element, _validAuthenticationTypes.Node);
                }
                else if (credentials.ValidAuthenticationTypes == null)
                {
                    XElementUtility.RemoveIndented(_validAuthenticationTypes.Node);
                    _validAuthenticationTypes = null;

                    if (Origin != null) Origin.IsDirty = true;
                }
                else _validAuthenticationTypes.Update(credentials._validAuthenticationTypes);

            }
        }

        internal override void SetOrigin(SettingsFile origin)
        {
            base.SetOrigin(origin);

            _username.SetOrigin(origin);
            _password.SetOrigin(origin);
            _validAuthenticationTypes?.SetOrigin(origin);
        }

        internal override void RemoveFromSettings()
        {
            base.RemoveFromSettings();

            _username.RemoveFromSettings();
            _password.RemoveFromSettings();
            _validAuthenticationTypes?.RemoveFromSettings();
        }
    }
}
