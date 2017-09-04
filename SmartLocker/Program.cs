
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace SmartLocker
{
    class Program
    {
        private string[] args;

        static int Main(string[] args)
        {
            return new Program(args).Run();
        }
        
        Program(string[] args)
        {
            this.args = args;
        }

        private int Run()
        {
            string fileLocation = Assembly.GetEntryAssembly().Location;
            string curDir = Path.GetDirectoryName(fileLocation);
            string templateSrc = curDir + @"\SmartLockerTemplate\Template.cs";

            string exeSrc;
            string icoSrc = null;

            switch (args.Length)
            {
                case 1:
                    exeSrc = args[0];
                    break;
                case 2:
                    exeSrc = args[0];
                    icoSrc = args[1];
                    break;
                default:
                    Console.WriteLine("exe 파일이 입력되지 않았습니다.");
                    return 1;
            }

            string filename = exeSrc.Split('\\').Last();

            // 암호화 할 exe 파일의 바이너리
            byte[] bin = ByteArrayFromFile(exeSrc);

            // 바이너리 해쉬값
            string hash = Sha256(bin);

            // 임시 디렉토리
            string tmpDir = Path.GetTempPath() + @"smartlocker\" + hash + @"\";

            // 임시 디렉토리 생성
            CreateDirIfNotExist(tmpDir);

            // 임시 파일 설정
            string tmpCsSrc = tmpDir + filename + ".cs";
            string outSrc = tmpDir + filename + ".out.exe";

            if (File.Exists(tmpCsSrc))
            {
                File.Delete(tmpCsSrc);
            }

            if (File.Exists(outSrc))
            {
                File.Delete(outSrc);
            }

            // 실행파일에 맞는 cs 파일 생성
            SaveCs(templateSrc, tmpCsSrc, bin, filename, hash);

            // 아이콘 파일 생성
            if (icoSrc == null)
            {
                icoSrc = tmpDir + filename + ".ico";

                if (File.Exists(icoSrc))
                {
                    File.Delete(icoSrc);
                }

                Icon icon = IconFromFilePath(exeSrc);
                SaveIcon(icon, icoSrc);
            }

            // 생성된 cs 파일을 바탕으로 exe 파일 생성
            string cscPath = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc";
            string argStr = " /out:\"" + outSrc + "\"";
            argStr += icoSrc != null ? " /win32icon:\"" + icoSrc + "\"" : "";
            argStr += " /main:SmartLockerTemplate.Template";
            argStr += " \"" + tmpCsSrc + "\"";
            argStr += " \"" + curDir + @"\SmartLockerTemplate\LoginForm.cs" + "\"";
            argStr += " \"" + curDir + @"\SmartLockerTemplate\LoginForm.Designer.cs" + "\"";

            Process proc = ProcessStart(cscPath, argStr);

            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();

                if (line.Contains("error"))
                {
                    Console.WriteLine(line);
                }
            }

            proc.WaitForExit();
            
            if (proc.ExitCode == 0)
            {
                string finalDir = curDir + @"\" + hash;
                string finalSrc = finalDir + @"\" + filename;
                
                // 파일을 저장할 디렉토리 생성
                CreateDirIfNotExist(finalDir);

                // 생성된 파일 이동
                if (File.Exists(finalSrc))
                {
                    File.Delete(finalSrc);
                }

                File.Move(outSrc, finalSrc);

                // 임시 폴더 삭제
                Directory.Delete(tmpDir, true);
                
                Console.WriteLine("SUCCESS: " + finalSrc);

                return 0;
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("ERROR");
                return 1;
            }
        }

        private void CreateDirIfNotExist(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private Process ProcessStart(string path, string args)
        {
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            return proc;
        }

        private string Sha256(byte[] bin)
        {
            StringBuilder Sb = new StringBuilder();

            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(bin);

                foreach (Byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }

        private void SaveCs(string templateLoc, string csLoc, byte[] bin, string filename, string hash)
        {
            int isDotNet;

            try
            {
                Assembly.Load(bin);
                isDotNet = 1;
            }
            catch (BadImageFormatException e)
            {
                isDotNet = 0;
            }

            StreamWriter file = new StreamWriter(csLoc);

            string csBinary = "=\"" + Convert.ToBase64String(bin) + "\"";
            string csFilename = "=\"" + filename + "\"";
            string csIsdotnet = "=" + isDotNet;

            using (StreamReader sr = new StreamReader(templateLoc))
            {
                while (sr.Peek() >= 0)
                {
                    file.WriteLine(sr.ReadLine()
                            .Replace("/*@BINARY*/", csBinary)
                            .Replace("/*@FILENAME*/", csFilename)
                            .Replace("/*@IS_DOT_NET*/", csIsdotnet)
                            .Replace("/*@EXE_HASH*/", "=\"" + hash + "\""));
                }
            }

            file.Close();
        }

        private byte[] ByteArrayFromFile(string file)
        {
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            byte[] bin = br.ReadBytes(Convert.ToInt32(fs.Length));
            fs.Close();
            br.Close();

            return bin;
        }

        public void SaveIcon(Icon icon, string path)
        {
            if (icon != null)
            {
                // Save it to disk, or do whatever you want with it.
                using (var stream = new FileStream(path, FileMode.CreateNew))
                {
                    icon.Save(stream);
                }
            }
        }

        public Icon IconFromFilePath(string filePath)
        {
            Icon result = (Icon)null;

            try
            {
                result = Icon.ExtractAssociatedIcon(filePath);
            }
            catch (System.Exception)
            {
                // swallow and return nothing. You could supply a default Icon here as well
            }

            return result;
        }
    }
}
