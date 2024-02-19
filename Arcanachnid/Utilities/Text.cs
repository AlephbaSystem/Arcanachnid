using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Arcanachnid.Utilities
{
    public static class Text
    {
        public static DateTime ConvertPersian(string persianDateString)
        {
            if (string.IsNullOrEmpty(persianDateString)) return DateTime.Now;
            PersianCalendar pc = new PersianCalendar();
            string[] parts = persianDateString.Split('/');
            if (parts.Length < 3)
                throw new ArgumentException("Invalid Persian date format.");
            int year = int.Parse(parts[0]);
            int month = int.Parse(parts[1]);
            int day = int.Parse(parts[2]);
            DateTime dateTime = pc.ToDateTime(year, month, day, 0, 0, 0, 0);
            return dateTime;
        }
        public static string Normalize(string text)
        {
            string[] lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            HashSet<string> uniqueLines = new HashSet<string>();
            var result = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine)) continue;

                if (uniqueLines.Add(trimmedLine))
                {
                    result.AppendLine(trimmedLine);
                }
            }
            text = result.ToString().Trim();
            text = WebUtility.HtmlDecode(text);
            text = WebUtility.UrlDecode(text);
            // \u0600-\u06FF: Persian and Arabic scripts
            // \u200c (\u200c is a zero-width non-joiner), \u200d (a zero-width joiner): Used in Persian writing
            // \u0660-\u0669: Arabic-Indic digits (used in Persian)
            // A-Za-z: English letters
            // 0-9: English numerals
            var regex = new Regex(@"[^\u0600-\u06FF\u200c\u200d\u0660-\u0669A-Za-z0-9\s.,،؛:!?()""'-]+");
            return regex.Replace(text, string.Empty).Trim();
        }
    }
}
