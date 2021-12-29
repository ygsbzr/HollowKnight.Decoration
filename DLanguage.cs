using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OLan = global::Language.Language;
namespace DecorationMaster
{
    public static class DLanguage
    {
        public static string MyLanguage(string key,string sheetTitle,string orig)
        {
            if(MyLan.ContainsKey(key))
            {
                return MyLan[key];
            }
            return orig;
        }

        public static Dictionary<string, string> MyLan = new Dictionary<string, string>()
        {
            {"Decoration_Test","This is a text" }
        };
    }
}
