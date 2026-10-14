using System;
using System.Collections.Generic;
using System.Globalization;

namespace HarborCity
{
    [Serializable] public sealed class TranslationEntry {public string key,text;}
    [Serializable] public sealed class TranslationData {public string language;public TranslationEntry[] entries;}
    public sealed class CityLocalization
    {
        readonly Dictionary<string,string> text=new Dictionary<string,string>(StringComparer.Ordinal);
        public CityLocalization(TranslationData data)
        {
            if(data?.entries==null)throw new ArgumentException("Missing translations");
            foreach(var entry in data.entries){if(entry==null||string.IsNullOrWhiteSpace(entry.key)||entry.text==null||text.ContainsKey(entry.key))throw new ArgumentException("Invalid/duplicate translation key");text.Add(entry.key,entry.text);}
        }
        public string Get(string key,params object[] arguments)
        {if(!text.TryGetValue(key,out var value))return "["+key+"]";return arguments.Length==0?value:string.Format(CultureInfo.CurrentCulture,value,arguments);}
        public string Error(Exception error)=>error is CityRuleException rule?Get(rule.Key,rule.Arguments):error.Message;
    }
}
