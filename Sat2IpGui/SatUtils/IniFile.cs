using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat2IpGui.SatUtils
{
    using System.Text;
    using System.Runtime.InteropServices;
    using System;

       public class IniFile
       {
           public static int capacity = 512;
           public string path { get; private set; }

           [DllImport("kernel32", CharSet = CharSet.Unicode)]
           private static extern int GetPrivateProfileString(string section, string key,
               string defaultValue, StringBuilder value, int size, string filePath);

           [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
           static extern int GetPrivateProfileString(string section, string key, string defaultValue,
               [In, Out] char[] value, int size, string filePath);

           [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
           private static extern int GetPrivateProfileSection(string section, IntPtr keyValue,
               int size, string filePath);

           [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
           [return: MarshalAs(UnmanagedType.Bool)]
           private static extern bool WritePrivateProfileString(string section, string key,
               string value, string filePath);

           /*        [DllImport("kernel32")]
                   private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
                   [DllImport("kernel32")]
                   private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
           */
           public IniFile(string INIPath)
           {
               path = INIPath;
           }
           public void IniWriteValue(string Section, string Key, string Value)
           {
               WritePrivateProfileString(Section, Key, Value, this.path);
           }

           public string ReadValue(string section, string key, string defaultValue = "")
           {
               var value = new StringBuilder(capacity);
               GetPrivateProfileString(section, key, defaultValue, value, value.Capacity, this.path);
               return value.ToString();
           }

           public string[] ReadSections()
           {
               // first line will not recognize if ini file is saved in UTF-8 with BOM 
               while (true)
               {
                   char[] chars = new char[capacity];
                   int size = GetPrivateProfileString(null, null, "", chars, capacity, this.path);

                   if (size == 0)
                   {
                       return null;
                   }

                   if (size < capacity - 2)
                   {
                       string result = new String(chars, 0, size);
                       string[] sections = result.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                       return sections;
                   }

                   capacity = capacity * 2;
               }
           }

           public string[] ReadKeys(string section)
           {
               // first line will not recognize if ini file is saved in UTF-8 with BOM 
               while (true)
               {
                   char[] chars = new char[capacity];
                   int size = GetPrivateProfileString(section, null, "", chars, capacity, this.path);

                   if (size == 0)
                   {
                       return null;
                   }

                   if (size < capacity - 2)
                   {
                       string result = new String(chars, 0, size);
                       string[] keys = result.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                       return keys;
                   }

                   capacity = capacity * 2;
               }
           }

           public string[] ReadKeyValuePairs(string section)
           {
               while (true)
               {
                   IntPtr returnedString = Marshal.AllocCoTaskMem(capacity * sizeof(char));
                   int size = GetPrivateProfileSection(section, returnedString, capacity, this.path);

                   if (size == 0)
                   {
                       Marshal.FreeCoTaskMem(returnedString);
                       return null;
                   }

                   if (size < capacity - 2)
                   {
                       string result = Marshal.PtrToStringAuto(returnedString, size - 1);
                       Marshal.FreeCoTaskMem(returnedString);
                       string[] keyValuePairs = result.Split('\0');
                       return keyValuePairs;
                   }

                   Marshal.FreeCoTaskMem(returnedString);
                   capacity = capacity * 2;
               }
           }
       }
}
