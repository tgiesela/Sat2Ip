using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace Sat2IpGui.SatUtils
{
    class SatelliteReader
    {
        List<SatelliteInfo> listinfo = new List<SatelliteInfo>();
        public List<SatelliteInfo> read(String filename)
        {
            listinfo.Clear();
            using (TextFieldParser parser = new TextFieldParser(filename))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(";");
                string[] headers = parser.ReadFields();
                while (!parser.EndOfData)
                {
                    //Process row
                    string[] fields = parser.ReadFields();
                    SatelliteInfo info = new SatelliteInfo();
                    info.Orbital = fields[0];
                    info.Satellitename = fields[1];
                    info.Norad = fields[2];
                    info.Ini = "";
                    info.Channels = fields[5];
                    info.Ftachannels = fields[6];
                    info.Longitude = fields[7];
                    info.DownloadLink = fields[13];
                    listinfo.Add(info);
                }
            }
            return listinfo;
        }

        internal string getTransponderIniFilename(SatelliteInfo info)
        {
            string pattern = @"(\d+).(\d+)";
            Match m = Regex.Match(info.Orbital, pattern, RegexOptions.IgnoreCase);
            return Utils.Utils.getStorageFolder() + String.Format("{00}{1}.ini", m.Groups[1], m.Groups[2]);
        }

        internal string getSatelliteName(SatelliteInfo info)
        {
            byte[] tempBytes;
            tempBytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(info.Orbital + " " + info.Satellitename);
            string asciibytes = System.Text.Encoding.UTF8.GetString(tempBytes);
            return asciibytes.Replace((char)0x3f, ' ');
        }
        internal SatelliteInfo findSatelliteName(string name)
        {
            foreach (SatelliteInfo info in listinfo)
            {
                if (getSatelliteName(info).Equals(name))
                    return info;
            }
            return null;
        }
        internal SatelliteInfo findSatelliteOrbit(int orbit)
        {
            foreach (SatelliteInfo info in listinfo)
            {
                string[] parts = getSatelliteName(info).Split(' ');
                if (decimal.Parse(parts[0]) == orbit)
                    return info;
            }
            return null;
        }
    }
    class SatelliteInfo
    {
        string orbital;
        string satellitename;
        string norad;
        string ini;
        string channels;
        string ftachannels;
        string longitude;
        string downloadLink;

        public string Orbital { get => orbital; set => orbital = value; }
        public string Satellitename { get => satellitename; set => satellitename = value; }
        public string Norad { get => norad; set => norad = value; }
        public string Ini { get => ini; set => ini = value; }
        public string Ftachannels { get => ftachannels; set => ftachannels = value; }
        public string Longitude { get => longitude; set => longitude = value; }
        public string DownloadLink { get => downloadLink; set => downloadLink = value; }
        public string Channels { get => channels; set => channels = value; }
    }
}
