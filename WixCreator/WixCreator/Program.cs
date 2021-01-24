using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;
using WixSharp;

namespace WixCreator
{
    class MainClass
    {

        static void Main(string[] args)
        {
            if (args != null)
            {
                if (args.Length != 5)
                {
                    Console.WriteLine("Usage: WiXCreatorConsole.exe LicensePath PathToBuild VersionText ResultBuildPath");
                    Console.WriteLine("Where LicensePath is path to rtf file");
                    Console.WriteLine("Where PathToBuild is path to folder with build");
                    Console.WriteLine("Where VersionText is string in format like 1.0.0.0");
                    Console.WriteLine("Where ResultBuildPath is path to file for creation, like \".../Folder/Xcalscan.msi\"");
                    Console.WriteLine("Where WixPath is path to WiX, like \".../packages/WixSharp.wix.bin.3.11.2/tools/bin\"");
                    return;
                }

                Console.WriteLine("Starting msi creation...");
                CreateMSI(args[0], args[1], args[2], args[3], args[4]);
                Console.WriteLine("Done!");
            }
            else
            {
                Console.WriteLine("Usage: WiXCreatorConsole.exe LicensePath PathToBuild VersionText ResultBuildPath");
                Console.WriteLine("Where LicensePath is path to rtf file");
                Console.WriteLine("Where PathToBuild is path to folder with build");
                Console.WriteLine("Where VersionText is string in format like 1.0.0.0");
                Console.WriteLine("Where ResultBuildPath is path to file for creation, like \".../Folder/Xcalscan.msi\"");
                Console.WriteLine("Where WixPath is path to WiX, like \".../packages/WixSharp.wix.bin.3.11.2/tools/bin\"");
            }

            //HACK for wine unfreeze after app closed
            Environment.SetEnvironmentVariable("PATHEXT", "%PATHEXT%;.");
            RunCommand("/C set PATHEXT=%PATHEXT%;.");
            RunCommand(@"/C Z:\bin\sh -c ""killall timelimit"" ");
        }

        private static void RunCommand(string command)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();


            startInfo.FileName = @"cmd.exe";

            startInfo.Arguments = $@"{command} && exit";
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        private static readonly Feature Feature = new Feature(new Id("Feature_App"));

        private static readonly Id AppId = new Id("File_App_exe");


        private static void CreateMSI(string LicensePath, string PathToBuild, string VersionText, string ResultBuildPath, string WixPath)
        {
            List<WixEntity> files = new List<WixEntity>();
          
            var paths = System.IO.Directory.GetFiles(PathToBuild);


            foreach (var path in paths)
            {
                files.Add(new File(path));

                if (path.EndsWith("MemcardRex.exe"))
                {
                    (files[files.Count - 1] as File).Shortcuts = new FileShortcut[] {  new FileShortcut("MemcardRex", "INSTALLDIR"),
                            new FileShortcut("MemcardRex", @"%Desktop%"), new FileShortcut("MemcardRex",@"%ProgramMenu%/MemcardRex") { WorkingDirectory = "%Temp%", Arguments = "777" } };
                    (files[files.Count - 1] as File).Id = AppId;
                    (files[files.Count - 1] as File).Feature = Feature;
                }
            }


            List<WixEntity> InnerDirs = GetInnerDirs(PathToBuild);
            files.AddRange(InnerDirs);


            files.Add(new RemoveFolderEx { On = InstallEvent.uninstall, Property = "DIR_PATH_PROPERTY_NAME" });
            files.Add(new ExeFileShortcut("Uninstall MemcardRex",
                                "[System64Folder]msiexec.exe",
                                "/x [ProductCode]"));

            var dir = new InstallDir(@"%ProgramFiles%\MemcardRex",
                             files.ToArray());

            var dirStartMenu = new Dir("%ProgramMenu%/MemcardRex", new ExeFileShortcut("Uninstall MemcardRex",
                                "[System64Folder]msiexec.exe",
                                "/x [ProductCode]"), new RemoveFolderEx { On = InstallEvent.uninstall, Property = "DIR_PATH_PROPERTY_NAME" });



            var project = new Project("MemcardRex",
                          dir, dirStartMenu);

            project.Version = Version.Parse(VersionText);
            project.GUID = new Guid("6f332b47-1434-42bd-9195-1362ba35889b");

            project.MajorUpgradeStrategy = new MajorUpgradeStrategy() {
                UpgradeVersions = VersionRange.OlderThanThis,
                PreventDowngradingVersions = VersionRange.NewerThanThis,
                NewerProductInstalledErrorMessage = "Newer version already installed"
            };
            project.UI = WUI.WixUI_InstallDir;
            project.LicenceFile = LicensePath;
            project.WixSourceGenerated += (document) => {

                var productElement = document.Root.Select("Product");

                productElement.Add(new XElement("WixVariable",
                                        new XAttribute("Id", "WixUIDialogBmp"),
                                        new XAttribute("Value", "setup_background.bmp")));



                productElement.Add(new XElement("WixVariable",
                                        new XAttribute("Id", "WixUIBannerBmp"),
                                        new XAttribute("Value", "setup_icon.bmp")));

            };

            Environment.SetEnvironmentVariable("WIXSHARP_WIXDIR", WixPath);
            Compiler.WixLocation = WixPath;
            Compiler.LightOptions = "-sval -sh";
            Console.WriteLine("Starting building MSI.");
            Compiler.BuildMsi(project, ResultBuildPath);
        }

        private static List<WixEntity> GetInnerDirs(string pathToBuild)
        {
            List<WixEntity> result = new List<WixEntity>();
            var paths = System.IO.Directory.GetDirectories(pathToBuild);

            foreach (var dir in paths)
                result.Add(GetDirContent(dir));

            return result;
        }

        private static Dir GetDirContent(string dir)
        {
            List<WixEntity> files = new List<WixEntity>();

            var paths = System.IO.Directory.GetFiles(dir);
            foreach (var path in paths)
            {
                files.Add(new File(path));
            }

            List<WixEntity> InnerDirs = GetInnerDirs(dir);
            files.AddRange(InnerDirs);

            return new Dir(new System.IO.DirectoryInfo(dir).Name, files.ToArray());
        }

        public enum InstallEvent
        {
            install,
            uninstall,
            both
        }

        public class RemoveFolderEx : WixEntity, IGenericEntity
        {
            [Xml]
            public InstallEvent? On;

            [Xml]
            public string Property;

            [Xml]
            new public string Id;

            public void Process(ProcessingContext context)
            {
                // indicate that candle needs to use WixUtilExtension.dll
                context.Project.Include(WixExtension.Util);

                XElement element = this.ToXElement(WixExtension.Util.ToXName("RemoveFolderEx"));

                context.XParent
                       .FindFirst("Component")
                       .Add(element);
            }
        }
    }

}
