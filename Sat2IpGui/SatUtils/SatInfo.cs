using Microsoft.VisualBasic.FileIO;
using Sat2Ip;
using Sat2ipUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sat2IpGui.SatUtils
{
    public class SatInfo:InfoBase
    {
        private List<Satellite> m_satellites = new();

        public SatInfo()
        {
            string satfilename = Utils.Utils.getStorageFolder() + @"satellites.csv";
            m_satellites = read(satfilename);
        }
        public BindingSource datasourceSatellites()
        {
            BindingSource bs = new();
            bs.DataSource = m_satellites;
            return bs;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public BindingSource datasourceTransponders(LNB lnb)
        {
            string iniFilename = Utils.Utils.getStorageFolder() + "DVBS\\" + String.Format("{00}.ini", lnb.orbit());
            DownloadFrequencies(iniFilename, lnb);
            IniFileNew inifile = new IniFileNew(System.IO.Path.GetFullPath(iniFilename));
            List<Match> matches = inifile.getSections();
            m_transponders = new List<Transponder>();
            if (inifile.getSectionname(matches[0]).Equals("SATTYPE") &&
                inifile.getSectionname(matches[1]).Equals("DVB"))
            {
                Match section = inifile.getSection(1);
                int nroftransponders = int.Parse(inifile.getValue(section, "0"));
                for (int i = 1; i <= nroftransponders; i++)
                {
                    string line = inifile.getValue(section, i.ToString());
                    Transponder tsp = extractInfoFromTransponder(line);
                    tsp.diseqcposition = lnb.diseqcposition;
                    m_transponders.Add(tsp);
                }
            }
            return base.datasourceTransponders();
        }
        private void DownloadFrequencies(string filename, LNB lnb)
        {
            Satellite info = m_satellites.Find(x => x.displayname == lnb.satellitename);
            var client = new HttpClient();
            var response = client.GetAsync(info.downloadlink).Result;

            using (var stream = response.Content.ReadAsStreamAsync().Result)
            {
                var fileInfo = new FileInfo(filename);
                using (var fileStream = fileInfo.OpenWrite())
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
        public BindingSource updateDatasourceTransponders()
        {
            return base.datasourceTransponders();
        }
        private List<Satellite> read(String filename)
        {
            m_satellites.Clear();
            using (TextFieldParser parser = new TextFieldParser(filename))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(";");
                string[] headers = parser.ReadFields();
                while (!parser.EndOfData)
                {
                    //Process row
                    string[] fields = parser.ReadFields();
                    Satellite info = new ();
                    info.orbital = fields[0];
                    info.satellitename = fields[1];
                    byte[] tempBytes;
                    tempBytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(info.orbital + " " + info.satellitename);
                    string asciibytes = System.Text.Encoding.UTF8.GetString(tempBytes);
                    info.displayname = asciibytes.Replace((char)0x3f, ' ');

                    info.norad = fields[2];
                    info.ini = "";
                    info.channels = fields[5];
                    info.ftachannels = fields[6];
                    info.longitude = fields[7];
                    info.downloadlink = fields[13];
                    m_satellites.Add(info);
                }
            }
            return m_satellites;
        }
        private string getSatelliteName(Satellite info)
        {
            byte[] tempBytes;
            tempBytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(info.orbital + " " + info.satellitename);
            string asciibytes = System.Text.Encoding.UTF8.GetString(tempBytes);
            return asciibytes.Replace((char)0x3f, ' ');
        }

        public Satellite findSatelliteName(string name)
        {
            foreach (Satellite info in m_satellites)
            {
                if (getSatelliteName(info).Equals(name))
                    return info;
            }
            return null;
        }
        private string getSatelliteIniFilename(Satellite info)
        {
            string pattern = @"(\d+).(\d+)";
            Match m = Regex.Match(info.orbital, pattern, RegexOptions.IgnoreCase);
            return Utils.Utils.getStorageFolder() + "DVBS\\" + String.Format("{00}{1}.ini", m.Groups[1], m.Groups[2]);
        }

        private Transponder extractInfoFromTransponder(string transponder)
        {
            Transponder tsp = new Transponder();
            char[] delimiterChars = { ' ', ',' };
            string[] parts = transponder.Split(delimiterChars);
            decimal frequencydecimal = decimal.Parse(parts[0], System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
            int frequency = (int)frequencydecimal;
            string polarisation = parts[1];
            int samplerate = int.Parse(parts[2]);
            string errorcorrections = parts[3];
            string dvbtype = parts[4];
            string mtype = parts[5];
            tsp.frequency = (int)frequencydecimal;
            tsp.frequencydecimal = frequencydecimal;
            tsp.samplerate = samplerate;
            tsp.polarisationFromString(polarisation);
            tsp.dvbsystemFromString(dvbtype);
            tsp.fecFromString(errorcorrections);
            tsp.mtypeFromString(mtype);
            return tsp;
        }

    }
    public class Satellite
    {
        public string satellitename { get ; set; }
        public string satelliterow { get; set; }
        public string orbital { get; internal set; }
        public string norad { get; internal set; }
        public string ini { get; internal set; }
        public string channels { get; internal set; }
        public string ftachannels { get; internal set; }
        public string longitude { get; internal set; }
        public string downloadlink { get; internal set; }
        public string displayname { get; internal set; }
    }
}
