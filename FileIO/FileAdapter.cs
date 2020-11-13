using System.IO;
using System.Xml.Serialization;

namespace MACOs.JY.DataLogger.FileIO
{
    public class FileAdapter
    {
        public static void WriteConfigureFile(Project p)
        {
            XmlSerializer x = new XmlSerializer(p.GetType());
            string path = @".\" + p.Name + @".xml";
            TextWriter fs = new StreamWriter(path, false);
            x.Serialize(fs, p);
            fs.Close();
        }

        public static Project ReadConfigureFile(string path)
        {
            XmlSerializer x = new XmlSerializer(typeof(Project));
            StreamReader fs = new StreamReader(path);
            Project prj = (Project)x.Deserialize(fs);
            fs.Close();
            return prj;
        }

        public static void WriteToMAT()
        {
        }

        public static void ReadFromMAT()
        {
        }
    }
}