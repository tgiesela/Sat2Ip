using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sat2IpGui.SatUtils
{
    public class IniFileNew
    {
    // System.Linq 
    // System.Text.RegularExpressions 
    string iniText = @"[Common]
UnitsSystem=US
UnitsSystem=US
ClientName=
[UnCommon]
CreatedBy=WRH
CreatedOn=Thu Feb 08 07:43:49 2007
LastUsedBy=WRH
Note=This is my sample data";


    string pattern = @"^(?:\[)                  # Section Start
                      (?<Section>[^\]]*)        # Actual Section text into Section Group
                      (?:\])                    # Section End then EOL/EOB
                      (?:[\r\n]{0,2}|\Z)        # Match but don't capture the CRLF or EOB

                      (?<KVPs>                # Begin working on the Key Value Pairs
                        (?!\[)                # Stop if a [ is found
                        (?<Key>[^=]*?)        # Any text before the =, matched few as possible
                           (?:=)              # Anchor the = now but don't capture it
                        (?<Value>[^=\[]*)     # Get everything that is not an =
                        (?=^[^=]*?=|\Z|\[)    # Look Ahead, Capture but don't consume
                      )+                      # Multiple values
                     ";
        private MatchCollection mcKVPs;
        public IniFileNew(string file)
        {
            iniText = File.ReadAllText(file);
            mcKVPs = Regex.Matches(iniText,
                                   pattern,
                                   RegexOptions.Compiled |
                                   RegexOptions.IgnorePatternWhitespace |
                                   RegexOptions.Multiline
                                  );
        }
        public Match getSection(int i)
        {
            if (i < mcKVPs.Count)
                return mcKVPs[i];
            throw new Exception("Index out of range");
        }
        public List<Match> getSections()
        {
            List<Match> sections = new List<Match>();
            foreach (Match match in mcKVPs)
                sections.Add(match);
            return sections;
        }
        public string getSectionname(Match m)
        {
            return m.Groups["Section"].Value;
        }
        public List<string> getKeyValuePairs(Match m)
        {
            List<string> captures = new List<string>();
            foreach (Capture cap in m.Groups["KVPs"].Captures)
                captures.Add(cap.ToString());
            return captures;
        }
        public string getValue(Match m, string key)
        {
            int i = 0;
            foreach (Capture cap in m.Groups["Key"].Captures)
            {
                if (cap.Value.Trim().Replace("\t","").Equals(key))
                   return m.Groups["Value"].Captures[i].Value.Trim().Replace("\t","");
                i++;
            }
            throw new Exception("Key " + key + " not found");
        }
        public string getKeyValuePair(Match m, int i)
        {
            if (i < m.Groups["KVPs"].Captures.Count)
                return m.Groups["KVPs"].Captures[i].ToString();
            throw new Exception("Index out of range");
        }
        public IniFileNew() {
            MatchCollection mcKVPs = Regex.Matches(iniText,
                                                    pattern,
                                                    RegexOptions.Compiled |
                                                    RegexOptions.IgnorePatternWhitespace |
                                                    RegexOptions.Multiline
                                                   );
            var ini = from Match m in mcKVPs
                      where mcKVPs != null
                      where mcKVPs.Count > 0
                      select new
                      {
                          section = m.Groups["Section"].ToString(),
                          kvps = from Capture cpt in m.Groups["KVPs"].Captures
                                 select cpt.ToString(),

                          keys = from Capture cpt in m.Groups["Key"].Captures
                                 select cpt.ToString(),

                          values = from Capture cpt in m.Groups["Value"].Captures
                                   select cpt.ToString(),
                      };

            foreach (var kvps in ini)
            {
                Console.WriteLine(kvps.section);

                foreach (string sKVP in kvps.kvps)
                    Console.WriteLine("\t{0}", sKVP.Trim());

            }
        }
    }
}
