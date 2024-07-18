using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcanachnid.Utilities
{
    public class PersianDateConverter
    {
        private static readonly Dictionary<string, int> PersianMonthNames = new Dictionary<string, int>
    {
        {"فروردین", 1}, {"اردیبهشت", 2}, {"خرداد", 3}, {"فروردين", 1}, {"تير", 4 }, {"شهريور", 6},
        {"تیر", 4}, {"مرداد", 5}, {"شهریور", 6},
        {"مهر", 7}, {"آبان", 8}, {"آذر", 9},
        {"دی", 10}, {"بهمن", 11}, {"اسفند", 12}
    };

        private static readonly Dictionary<char, char> PersianNumeralsToWestern = new Dictionary<char, char>
    {
        {'۰', '0'}, {'۱', '1'}, {'۲', '2'}, {'۳', '3'}, {'۴', '4'},
        {'۵', '5'}, {'۶', '6'}, {'۷', '7'}, {'۸', '8'}, {'۹', '9'}
    };

        public static string ConvertPersianNumeralsToWestern(string input)
        {
            return new string(input.Select(c => PersianNumeralsToWestern.ContainsKey(c) ? PersianNumeralsToWestern[c] : c).ToArray());
        }

        public static DateTime ConvertPersianToDateTimeTDYM(string persianDateString)
        {
            if (string.IsNullOrEmpty(persianDateString))
                return DateTime.Now;

            persianDateString = ConvertPersianNumeralsToWestern(persianDateString.Trim());
            persianDateString.Replace("/", "-");


            string[] mainParts = persianDateString.Split('-');
            if (mainParts.Length != 2)
                throw new ArgumentException("Invalid Persian date format.");

            string timePart = mainParts[0].Trim();
            string datePart = mainParts[1].Trim();

            string[] timeParts = timePart.Split(':');
            if (timeParts.Length != 2)
                throw new ArgumentException("Invalid time format.");

            string[] dateParts = datePart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (dateParts.Length != 3)
                throw new ArgumentException("Invalid date format.");

            int day = int.Parse(dateParts[0]);
            if (!PersianMonthNames.TryGetValue(dateParts[1].Trim(), out int month))
                throw new ArgumentException("Invalid Persian month name.");

            int year = int.Parse(dateParts[2]);

            int hour = int.Parse(timeParts[0]);
            int minute = int.Parse(timeParts[1]);

            PersianCalendar pc = new PersianCalendar();
            DateTime dateTime = pc.ToDateTime(year, month, day, hour, minute, 0, 0);

            return dateTime;
        }

        public static DateTime ConvertPersianToDateTimeYMD(string persianDateString)
        {
            if (string.IsNullOrEmpty(persianDateString)) return DateTime.Now;
            persianDateString = ConvertPersianNumeralsToWestern(persianDateString);

            string[] parts = persianDateString.Trim().Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                throw new ArgumentException("Invalid Persian date format.");

            int day = int.Parse(parts[2]);
            int month = int.Parse(parts[1]);
            int year = int.Parse(parts[0]);

            PersianCalendar pc = new PersianCalendar();
            DateTime dateTime = pc.ToDateTime(year, month, day, 0, 0, 0, 0);

            return dateTime;
        }
        public static DateTime ConvertPersianToDateTime(string persianDateString)
        {
            if (string.IsNullOrEmpty(persianDateString)) return DateTime.Now;
            persianDateString = ConvertPersianNumeralsToWestern(persianDateString);

            string[] parts = persianDateString.Trim().Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                throw new ArgumentException("Invalid Persian date format.");

            int day = int.Parse(parts[0]);
            int month = PersianMonthNames[parts[1]];
            int year = int.Parse(parts[2]);
            string[] timeParts = parts[4].Split(':');
            int hour = int.Parse(timeParts[0]);
            int minute = int.Parse(timeParts[1]);

            PersianCalendar pc = new PersianCalendar();
            DateTime dateTime = pc.ToDateTime(year, month, day, hour, minute, 0, 0);

            return dateTime;
        }
    }
}
