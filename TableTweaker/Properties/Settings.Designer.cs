﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TableTweaker.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"using System;
public string FormatDate(string date, string format)
{
  return DateTime.Parse(date).ToString(format);
}
public string IndexOf(string s, string value)
{
  return s.IndexOf(value).ToString();
}
public string Left(string s, int length)
{
  return string.IsNullOrEmpty(s) ? string.Empty : s.Substring(0, (length < s.Length ) ? length : s.Length);
}
public string Right(string s, int length)
{
  return string.IsNullOrEmpty(s) ? string.Empty : ((s.Length > length) ? s.Substring(s.Length - length, length) : s);
}
public string Replace(string s, string oldValue, string newValue)
{
  return s.Replace(oldValue, newValue);
}
public string Substring(string s, int startIndex, int length)
{
  return s.Substring(startIndex, length);
}
public string ToLower(string s)
{
  return s.ToLower();
}
public string ToUpper(string s)
{
  return s.ToUpper();
}
public string Trim(string s, string trimString)
{
  return s.Trim(trimString.ToCharArray());
}
")]
        public string LastSessionCode {
            get {
                return ((string)(this["LastSessionCode"]));
            }
            set {
                this["LastSessionCode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("$EACH+\r\n$rowNum\r\nTo: $ToLower(\"$1.$0@$2.com\")\r\nHello $1 $0,\r\nI\'m sorry to inform " +
            "you of a terrible accident at $2.\r\n---\r\n")]
        public string LastSessionPattern {
            get {
                return ((string)(this["LastSessionPattern"]));
            }
            set {
                this["LastSessionPattern"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Last Name,First Name,Company\r\nCook,Tim,Apple\r\nNadella,Satya,Microsoft\r\nDrury,Rod," +
            "Xero\r\nZuckerberg,Mark,Facebook\r\nPage,Larry,Google")]
        public string LastSessionInput {
            get {
                return ((string)(this["LastSessionInput"]));
            }
            set {
                this["LastSessionInput"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int CbxModeIndex {
            get {
                return ((int)(this["CbxModeIndex"]));
            }
            set {
                this["CbxModeIndex"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int CbxDelimiteIndex {
            get {
                return ((int)(this["CbxDelimiteIndex"]));
            }
            set {
                this["CbxDelimiteIndex"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int CbxQualifierIndex {
            get {
                return ((int)(this["CbxQualifierIndex"]));
            }
            set {
                this["CbxQualifierIndex"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".+")]
        public string CbxFiltersValue {
            get {
                return ((string)(this["CbxFiltersValue"]));
            }
            set {
                this["CbxFiltersValue"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4")]
        public int CbxFontSizeIndex {
            get {
                return ((int)(this["CbxFontSizeIndex"]));
            }
            set {
                this["CbxFontSizeIndex"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int CbxLineWrapIndex {
            get {
                return ((int)(this["CbxLineWrapIndex"]));
            }
            set {
                this["CbxLineWrapIndex"] = value;
            }
        }
    }
}
