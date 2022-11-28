using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ghgl.CodeEditor
{
    internal static class GhglCompletionProvider
    {
        static string[] _keywords = null;
        static string[] _builtins = null;

        private static string WordAtPosition(string txt, int pos)
        {
	        if (string.IsNullOrWhiteSpace(txt)) return "";
	        if (pos < 0 || pos > txt.Length) return "";

            char[] whitespaceChars = new char[]{' ', '\t'};

	        string txtBeforeCursor = txt.Substring(0,pos);
            int idxWs = txtBeforeCursor.LastIndexOfAny(whitespaceChars);
	        int wordStart = idxWs == -1
                ? 0
                : idxWs + 1;

	        idxWs = txt.Substring(pos).IndexOfAny(whitespaceChars);
	        int wordEnd = idxWs == -1
	        	? txt.Length
	        	: idxWs + txtBeforeCursor.Length;

	        int wordLength = wordEnd - wordStart;
	        return txt.Substring(wordStart, wordLength);
        }

        public static List<char> Triggers = new List<char> { ' ', '_' };

        public static async Task<List<string>> GetCompletion(string code, int position, char ch)
        {
           string word = WordAtPosition(code, position);
           List<string> items = new List<string>();
           if (_keywords == null)
           {
               string kw0 = "attribute layout uniform float int bool vec2 vec3 vec4 " +
                   "mat4 in out sampler2D if else return void flat discard";
               _keywords = kw0.Split(new char[] { ' ' });
               Array.Sort(_keywords);
           }
           if (_builtins == null)
           {
               var bis = BuiltIn.GetUniformBuiltIns();
               bis.AddRange(BuiltIn.GetAttributeBuiltIns());
               _builtins = new string[bis.Count];
               for (int i = 0; i < bis.Count; i++)
                   _builtins[i] = bis[i].Name;
               Array.Sort(_builtins);
           }
           string[] list = _keywords;
           if (word.StartsWith("_"))
               list = _builtins;
           foreach (var kw in list)
           {
               int startIndex = 0;
               bool add = true;
               foreach (var c in word)
               {
                   startIndex = kw.IndexOf(c, startIndex);
                   if (startIndex < 0)
                   {
                       add = false;
                       break;
                   }
               }
               if (add)
                   items.Add(kw);
           }
          return items;
        }
    }
}
