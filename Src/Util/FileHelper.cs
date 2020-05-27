
using System;
using System.Text;
using System.IO;
using System.Security.Permissions; // SecurityPermission
using System.Diagnostics; // Process

namespace Util
{
    public static class FileHelper
    {
        public static readonly string crLf = Environment.NewLine;
        const string possibleWriteErrCause = 
            "The file may be write-protected or locked by another software";

        public static string GetParentDirectoryPath(string path)
        {
            // Return parent directory path
            // Ex.: C:\Tmp\Tmp2 -> C:\Tmp
            // Ex. with filename : C:\Tmp\MyFile.txt -> C:\Tmp
            // Ex. with a filtered filename : C:\Tmp\*.txt -> C:\Tmp
            return Path.GetDirectoryName(path);
        }

        public static string[] ReadFile(string filePath, out string msgErr, 
            bool defaultEncoding = true, Encoding encode = null)
        {
            // Read file and return its content
            msgErr = "";
            try
            {
                if (defaultEncoding) encode = Encoding.Default;
                return System.IO.File.ReadAllLines(filePath, encode);
            }
            catch (Exception ex)
            {
                msgErr = "Can't read file : " + System.IO.Path.GetFileName(filePath) + crLf +
                    filePath + crLf + ex.Message;
                return null; 
            }
        }

        public static bool WriteFile(string filePath, StringBuilder content,
            out string msgErr, bool append = false, bool defaultEncoding = true,
            Encoding encode = null)
        {
            // Write file from StringBuilder
            msgErr = "";
            try
            {
                if (defaultEncoding) encode = Encoding.Default;
                using (StreamWriter sw = new StreamWriter(filePath, append, encode))
                { sw.Write(content.ToString()); }
                return true;
            }
            catch (Exception ex)
            {
                msgErr = "Can't write file : " + System.IO.Path.GetFileName(filePath) + crLf +
                    filePath + crLf + ex.Message + crLf + possibleWriteErrCause;
                return false;
            }
        }

        public static bool WriteFile(string filePath, string content,
            out string msgErr, bool append = false, bool bDefautEncoding = true,
            Encoding encode = null)
        {
            // Write file from string
            msgErr = "";
            try
            {
                if (bDefautEncoding) encode = Encoding.Default;
                using (StreamWriter sw = new StreamWriter(filePath, append, encode))
                { sw.Write(content); }
                return true;
            }
            catch (Exception ex)
            {
                msgErr = "Can't write file : " + System.IO.Path.GetFileName(filePath) + crLf +
                    filePath + crLf + ex.Message + crLf + possibleWriteErrCause;
                return false;
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand)]
        public static void StartProcess(string path, string arguments = "")
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(path);
            p.StartInfo.Arguments = arguments;
            p.Start();
        }

        public static void GetAssemblyVersion(string exePath,
            ref string exeVersion, ref DateTime exeDate)
        {
            Version exeV = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            exeVersion = exeV.ToString();
            FileInfo fi = new FileInfo(exePath);
            exeDate = fi.LastWriteTime;
        }
    }
}