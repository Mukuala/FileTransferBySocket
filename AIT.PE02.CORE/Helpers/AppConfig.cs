using System.Data;
using System.IO;

namespace AIT.PE02.Server.Core.Helpers
{
    public class AppConfig
    {
        public static void GetConfig(out string IP, out int Port)
        {
            string xmlBestand = Directory.GetCurrentDirectory() + "/serverconfig.xml";
            if (!File.Exists(xmlBestand))
            {
                MakeConfigFile();
            }
            DataSet ds = new DataSet();
            ds.ReadXml(xmlBestand, XmlReadMode.ReadSchema);
            IP = ds.Tables[0].Rows[0][0].ToString();
            Port = int.Parse(ds.Tables[0].Rows[0][1].ToString());
        }
        public static void WriteConfig(string IP, int Port)
        {
            string xmlBestand = Directory.GetCurrentDirectory() + "/serverconfig.xml";
            if (!File.Exists(xmlBestand))
            {
                MakeConfigFile();
            }
            DataSet ds = new DataSet();
            ds.ReadXml(xmlBestand, XmlReadMode.ReadSchema);
            ds.Tables[0].Rows[0][0] = IP;
            ds.Tables[0].Rows[0][1] = Port;
            ds.WriteXml(xmlBestand, XmlWriteMode.WriteSchema);
        }
        private static void MakeConfigFile()
        {
            DataSet ds = new DataSet();
            DataTable dt = ds.Tables.Add();
            DataColumn dc;
            dc = new DataColumn();
            dc.ColumnName = "IP";
            dc.DataType = typeof(string);
            dt.Columns.Add(dc);
            dc = new DataColumn();
            dc.ColumnName = "Port";
            dc.DataType = typeof(int);
            dt.Columns.Add(dc);
            DataRow dr = dt.NewRow();
            dr[0] = "127.0.0.1";
            dr[1] = 49200;
            dt.Rows.Add(dr);
            string xmlBestand = Directory.GetCurrentDirectory() + "/serverconfig.xml";
            ds.WriteXml(xmlBestand, XmlWriteMode.WriteSchema);
        }
    }
}
