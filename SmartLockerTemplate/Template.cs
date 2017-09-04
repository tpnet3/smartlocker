using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SmartLockerTemplate
{
    class Template
    {
        // exe 바이너리가 바운드 됩니다.
        private readonly string BINARY/*@BINARY*/;

        // exe 해시값이 바운드 됩니다.
        private readonly string EXE_HASH/*@EXE_HASH*/;

        // 원본 파일 이름이 바운드 됩니다.
        private static readonly string FILENAME/*@FILENAME*/;

        // .NET 여부가 바운드 됩니다. (0: .net 아님, 1: .net)
        public static readonly int IS_DOT_NET/*@IS_DOT_NET*/;

        // 공개키
        public static readonly string publicKey = "";
        
        // 맥주소
        public static readonly string macAddr =
            (
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                select nic.GetPhysicalAddress().ToString()
            ).FirstOrDefault();

        // 실행 파일 경로
        public static readonly string curFullname = Process.GetCurrentProcess().MainModule.FileName;
        public static readonly string curFilename = curFullname.Substring(curFullname.LastIndexOf("\\") + 1);
        public static readonly string exeSrc = Environment.CurrentDirectory + @"\_" + curFilename;

        private static readonly string fileLocation = Assembly.GetEntryAssembly().Location;
        private static readonly string curDir = Path.GetDirectoryName(fileLocation);
        private static readonly string licenseFile = curDir + @"\license.key";

        static void Main(string[] args)
        {
            // 템플릿 생성
            new Template(args);
        }

        private Template(string[] args)
        {
            // 강제 종료 이벤트 리스너
            new EventListener();

            // 프로그램 시작
            StartProgram(args);
        }

        private void StartProgram(string[] args)
        {
            // 라이센스 파일 가져오기, 없을 경우 null
            string lic = GetLic();

            if (lic != null)
            {
                // 라이센스 만료 확인
                if (CheckLic(lic))
                {
                    // 프로그램 실행
                    new SourceProgram(args, Convert.FromBase64String(BINARY));
                }
                else
                {
                    // 라이센스 갱신
                    if (RefreshLic())
                    {
                        // 프로그램 실행
                        new SourceProgram(args, Convert.FromBase64String(BINARY));
                    }
                    else
                    {
                        MessageBox.Show("라이센스 구입이 필요합니다.", "라이센스 오류",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                RunLoginForm();
            }
        }

        private void RunLoginForm()
        {
            // 로그인 폼 실행
            Application.Run(new LoginForm());
        }

        private string GetLic()
        {
            // TODO: 라이센스 존재 여부 확인

            if (File.Exists(licenseFile))
            {
                return Encoding.UTF8.GetString(File.ReadAllBytes(licenseFile));
            }

            return null;
        }

        private bool CheckLic(string lic)
        {
            // TODO: 올바른 라이센스인지 확인

            string[] keyStr = lic.Split('\"');
            
            if (keyStr.Length != 3)
            {
                return false;
            }

            string[] keyList = keyStr[1].Split(',');

            if (keyList.Length != 2)
            {
                return false;
            }

            if (keyList[0].Equals(macAddr) && keyList[1].Equals(EXE_HASH))
            {
                return true;
            }
            
            return false;
        }

        private bool RefreshLic()
        {
            // TODO: 라이센스 갱신
            //File.Delete(licenseFile);
            //File.WriteAllBytes(licenseFile, Encoding.UTF8.GetBytes("true"));

            return false;
        }
    }

    class SourceProgram
    {
        public static Process process;
        public static FileStream fs;

        private string[] args;
        private byte[] bin;

        public SourceProgram(string[] args, byte[] bin)
        {
            this.args = args;
            this.bin = bin;

            if (Template.IS_DOT_NET == 1)
            {
                AsmRun();
            }
            else
            {
                // .NET 이 아닐 경우

                // 콘솔창 숨김
                ConsoleWindow.Hide();

                // 실행
                SaveAndRun();
            }
        }

        private void AsmRun()
        {
            Assembly assembly = Assembly.Load(bin);
            MethodInfo method = assembly.EntryPoint;

            if (method != null)
            {
                object obj = assembly.CreateInstance(method.Name);
                method.Invoke(obj, new object[] { args == null ? new string[0] : args });
            }
        }

        private void SaveAndRun()
        {
            // 이미 파일이 존재할 경우 파일 삭제
            if (File.Exists(Template.exeSrc))
            {
                try
                {
                    Console.Write("Delete File... ");
                    File.Delete(Template.exeSrc);
                    Console.WriteLine("Success");
                }
                catch (IOException e)
                {
                    Console.WriteLine("Error");
                }
            }

            // 실행 파일 생성
            Console.Write("Create File... ");
            File.WriteAllBytes(Template.exeSrc, bin);
            Console.WriteLine("Success");

            // 파일 숨기기
            Console.Write("Hidden File... ");
            File.SetAttributes(Template.exeSrc, FileAttributes.Hidden);
            Console.WriteLine("Success");

            // 파일 실행
            Console.Write("Process File... ");
            process = Process.Start(Template.exeSrc, String.Join(" ", args));
            Console.WriteLine("Success");

            // 파일 읽기 락
            Console.Write("Lock File... ");
            fs = new FileStream(Template.exeSrc, FileMode.Open, FileAccess.Read, FileShare.None);
            Console.WriteLine("Success");

            // 프로세스 종료 대기
            Console.Write("Wait File... ");

            if (process != null)
            {
                process.WaitForExit();
            }

            Console.WriteLine("Success");

            // 파일 읽기 락 종료
            Console.Write("UnLock File... ");
            fs.Close();
            Console.WriteLine("Success");

            // 파일 삭제 
            Console.Write("Delete File... ");
            File.Delete(Template.exeSrc);
            Console.WriteLine("Success");
        }
    }

    class EventListener
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        private static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public EventListener()
        {
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
        }

        private bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                default:

                    // 실행중인 프로세스 종료
                    if (SourceProgram.process != null)
                    {
                        SourceProgram.process.Kill();
                        SourceProgram.process.WaitForExit();
                    }

                    // 파일 락 종료
                    if (SourceProgram.fs != null)
                    {
                        SourceProgram.fs.Close();
                    }

                    // 파일 삭제
                    if (Template.exeSrc != null && File.Exists(Template.exeSrc))
                    {
                        File.Delete(Template.exeSrc);
                    }

                    return false;
            }
        }
    }

    // 콘솔창 숨기기
    class ConsoleWindow
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        static IntPtr handle = GetConsoleWindow();

        public static void Hide()
        {
            ShowWindow(handle, SW_HIDE);
        }

        public static void Show()
        {
            ShowWindow(handle, SW_SHOW);
        }
    }
}
