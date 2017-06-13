using System;
using System.Globalization;
using System.IO;

namespace FTPZipAndUpload.Infrastructure
{
    public static class Utilities
    {
        /// <summary>
        /// Logger utility
        /// </summary>
        /// <param name="Input"></param>
        public static void WriteToFile(string Input)
        {
            string filePath = AppDomain.CurrentDomain.BaseDirectory + "log.txt";
            using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(Input);
            }
        }

        /// <summary>
        /// Converts to Integer with default value if input is not an integer
        /// </summary>
        /// <param name="s"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int ToInt(string s, int defaultValue = 0)
        {
            if (s != null)
            {
                if (IsInteger(s))
                {
                    return Convert.ToInt32(s, new CultureInfo("it-IT"));
                }
                else
                {
                    return defaultValue;
                }
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Check if string is an Integer
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsInteger(string s)
        {
            if (s != string.Empty)
            {
                int output;
                return Int32.TryParse(s, NumberStyles.Integer, new CultureInfo("it-IT"), out output);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check if string is a bool (true/false)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsBoolean(string s)
        {
            try
            {
                bool result = Convert.ToBoolean(s);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
