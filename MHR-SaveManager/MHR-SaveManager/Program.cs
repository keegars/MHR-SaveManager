using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MHR_SaveManager
{
    internal static class Program
    {
        //Reg values
        private const string regSteamPath = @"SOFTWARE\Valve\Steam";
        private const string regSteamPathValue = "SteamPath";
        private const string regSteamExePathValue = "SteamExe";
        private const string regSteamActiveProcessPath = regSteamPath + @"\ActiveProcess";
        private const string regSteamActiveUserValue = "ActiveUser";
        private const string regSteamAppsPath = regSteamPath + @"\Apps";
        private const string regSteamAppsNameValue = "Name";
        private const string steamFilename = "steam";
        private const string mhrInjector = "MHR - Reshade Injector Helper.exe";

        //Game info
        private const string gameId = "1446780";
        private const string gameName = "MonsterhunterRise";

        //Save settings
        private const int saveLimit = 100;
        private const int saveTime = 15 * 60 * 1000;
        private const int waitTime = 5 * 1000;

        //File/Folder info
        private static readonly string currentFilename = Process.GetCurrentProcess().ProcessName;
        private static readonly string steamData = $@"{GetRegKey(regSteamPath, regSteamPathValue)}/userdata";

        //User interaction
        private static readonly ConsoleKey DESIRED_INPUT = ConsoleKey.Enter;
        private static ConsoleKeyInfo USER_INPUT;

        //User Id
        private static string userId = null;

        private static async Task Main()
        {
            //Disable close button, as we want it to close gracefully
            EnableCloseButton(Process.GetCurrentProcess().MainWindowHandle, false);

            try
            {
                //Check if program is already running and if so, close this program
                if (ProcessCount(currentFilename) > 1)
                {
                    Console.WriteLine("Program is already running...");
                    await ExitApplication();
                }

                //Check steam is running
                if (!ProcessExists(steamFilename))
                {
                    Console.WriteLine("Steam is not running, starting steam...please re-launch once you have logged in.");

                    var steamExePath = GetRegKey(regSteamPath, regSteamExePathValue);

                    Process.Start(steamExePath);
                    await ExitApplication();
                }
                else
                {
                    Console.WriteLine("Steam is running.");
                }

                //Check we can get the the steam active user, if 0, means there is no one logged in
                var activeUser = GetRegKey(regSteamActiveProcessPath, regSteamActiveUserValue);

                if (activeUser == null || activeUser == "0")
                {
                    Console.WriteLine("Steam is running, however there is no active user logged in. Please log in and then relaunch program...");

                    await ExitApplication();
                }
                else
                {
                    Console.WriteLine($"User: {activeUser} is currently logged in");
                }

                userId = activeUser;

                //Create .bat to open save data folder, to make it easy to move files over when they want to recover it
                CreateBatOpenFolder(steamData, activeUser, gameId);

                //Check if game process is already running, and ask user if they want to terminate it or skip launching game
                var launchGame = true;
                if (ProcessExists(gameName))
                {
                    Console.Clear();
                    Console.WriteLine($"Running process for {gameName}, do you wish to terminate the process to start a new process? ");
                    Console.WriteLine("(Games can sometimes be left hanging, when steam hasn't closed them properly)");
                    Console.WriteLine("");
                    Console.WriteLine("Please press Y for yes, or N for no");

                    ConsoleKeyInfo keyPress;

                    do
                    {
                        keyPress = Console.ReadKey(false);
                    } while (!(keyPress.Key == ConsoleKey.Y || keyPress.Key == ConsoleKey.N));

                    if (keyPress.Key == ConsoleKey.Y)
                    {
                        //Kill game process
                        KillProcess(gameName);

                        //Wait 3 seconds to give it time to kill them
                        await Task.Delay(3 * 1000);
                    }
                    else
                    {
                        launchGame = false;
                    }
                }

                if (launchGame)
                {
                    //Check to see if injector program is in the folder, if so, launch that to launch rise
                    if (CurrentDirectoryHasFile(mhrInjector))
                    {
                        Console.WriteLine("Starting game using ReShade injector");
                        Process.Start(Path.Combine(Environment.CurrentDirectory, mhrInjector));
                    }
                    //Else, launch rise ourselves!
                    else
                    {
                        //Launch Game
                        Console.WriteLine("Starting game using steam api");
                        Process.Start($"steam://run/{gameId}");
                    }
                }

                ////Set up save back up process
                //Set this thread to low priority to avoid potentially hiccuping the game
                Console.WriteLine("Setting save manager to below normal thread priority");

                var currentProcess = Process.GetCurrentProcess();
                currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

                if (launchGame)
                {
                    //Wait 10 seconds to ensure that the game has had time to launch
                    Console.WriteLine("Waiting 10 seconds before starting save process, this is to give the game time to run...");
                    await Task.Delay(10 * 1000);
                }

                //Schedule backup of saves
                var backupTask = Task.Run(() => ScheduledBackupSaveAsync());

                //Await user to press desired input (ENTER at present)
                do
                {
                    USER_INPUT = Console.ReadKey();
                } while (USER_INPUT.Key != DESIRED_INPUT);

                //Wait for backup task to exit, just in case it is executing
                backupTask.Wait();
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "ErrorLog.txt"), $"{Environment.NewLine} {DateTime.Now:dd/MM/yyyy HH:mm:ss} {Environment.NewLine}  {ex.Source} {ex.Message} -  {ex.InnerException} - {ex.StackTrace}");
            }
        }

        private static string GetSteamGameNameById(string gameId)
        {
            return GetRegKey(regSteamAppsPath + $@"\{gameId}", regSteamAppsNameValue);
        }

        private static void CreateBatOpenFolder(string steamData, string activeUser, string gameId)
        {
            //Check to see if there is already a bat file for this game and user
            var batFileName = $"Open save data folder - User {activeUser} - {GetSteamGameNameById(gameId)}.bat";
            var batFilePath = Path.Combine(Environment.CurrentDirectory, batFileName);

            if (!File.Exists(batFilePath))
            {
                var steamSaveDataPath = Path.Combine(steamData, activeUser, gameId).Replace(@"/", @"\");
                File.WriteAllText(batFilePath, $@"%SystemRoot%\explorer.exe ""{steamSaveDataPath}\""");
            }
        }

        private static bool CurrentDirectoryHasFile(string file)
        {
            var directory = Environment.CurrentDirectory;
            var fileExists = new FileInfo(Path.Combine(directory, file));

            return fileExists.Exists;
        }

        private static string GetRegKey(string key, string value)
        {
            try
            {
                using (var regKey = Registry.CurrentUser.OpenSubKey(key))
                {
                    if (regKey != null)
                    {
                        var o = regKey.GetValue(value);
                        if (o != null)
                        {
                            return $"{o}";
                        }
                        else
                        {
                            throw new Exception("Registry entry was empty");
                        }
                    }
                    else
                    {
                        throw new Exception("Registry key was empty");
                    }
                }
            }
            catch
            {
                throw new Exception("Registry key does not exist");
            }
        }

        private static bool ProcessHasExited(Process process)
        {
            try
            {
                return process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private static async Task ScheduledBackupSaveAsync()
        {
            //Get Process for checking if it is still running
            var gameProcess = Process.GetProcessesByName(gameName).FirstOrDefault();

            //Set up wait timer to minimise cpu time
            var totalWaitTime = 0;

            while (USER_INPUT.Key != DESIRED_INPUT && !ProcessHasExited(gameProcess))
            {
                //Clear and backup the save data
                Console.Clear();

                BackupSave(steamData, userId, gameId);

                Console.WriteLine();
                Console.WriteLine("Awaiting User Input, press ENTER to quit...");

                //Loop until the desired input is pressed or total wait time is exceeded
                do
                {
                    await Task.Delay(waitTime);

                    //Check if the game is still running
                    gameProcess.Refresh();

                    totalWaitTime += waitTime;
                } while (totalWaitTime < saveTime && USER_INPUT.Key != DESIRED_INPUT && !ProcessHasExited(gameProcess));

                //Reset wait time
                totalWaitTime = 0;
            }

            if (ProcessHasExited(gameProcess))
            {
                //Exit out of process and don't wait for user input
                Environment.Exit(0);
            }
        }

        private static void BackupSave(string steamDataFolder, string userId, string gameId)
        {
            var saveFolderLocation = Path.Combine(Environment.CurrentDirectory, "Save Backup Folder");

            if (!Directory.Exists(saveFolderLocation))
            {
                Directory.CreateDirectory(saveFolderLocation);
            }

            var gameSaveDataFolder = Path.Combine(steamDataFolder, userId, gameId);
            var tmpName = gameSaveDataFolder.Substring(steamDataFolder.Length + 1, (gameSaveDataFolder.Length - steamDataFolder.Length) - 1).Replace(@"\", "_");
            var newFolderName = Path.Combine(saveFolderLocation, tmpName);

            var saveDirectory = CopyAll(gameSaveDataFolder, newFolderName, true);

            ZipFolder(saveDirectory);

            Directory.Delete(saveDirectory, true);

            //Clean up saved files
            var savedZipFiles = Directory.GetFiles(newFolderName, "*.zip");

            if (savedZipFiles.Length >= saveLimit)
            {
                var keepFiles = savedZipFiles.OrderByDescending(z => z).Take(saveLimit);
                var weakestFiles = savedZipFiles.Except(keepFiles).ToList();

                foreach (var file in weakestFiles)
                {
                    File.Delete(file);
                }
            }
        }

        private static void ZipFolder(string directory)
        {
            var dirName = new DirectoryInfo(directory);

            using (ZipArchive zip = ZipFile.Open($@"{dirName.Parent.FullName}/{dirName.Name}.zip", ZipArchiveMode.Create))
            {
                var dirFiles = dirName.GetFiles("*", SearchOption.AllDirectories);

                foreach (var file in dirFiles)
                {
                    var newEntryName = file.FullName.Replace(dirName.Parent.FullName, "").Replace(@"\" + dirName.Name + @"\", "");
                    zip.CreateEntryFromFile(file.FullName, newEntryName);
                }
            }
        }

        private static void KillProcess(string gameName)
        {
            var processes = Process.GetProcessesByName(gameName);

            foreach (var process in processes)
            {
                process.Kill();
            }
        }

        private static bool ProcessExists(string processName)
        {
            return ProcessCount(processName) > 0;
        }

        private static int ProcessCount(string processName)
        {
            return Process.GetProcessesByName(processName).Length;
        }

        private static string CopyAll(string sourceDirectory, string targetDirectory, bool isRootFolder = false)
        {
            if (isRootFolder)
            {
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                var newTargetDirectory = Path.Combine(targetDirectory, $"SAVEDATA_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
                targetDirectory = newTargetDirectory;
            }

            var source = new DirectoryInfo(sourceDirectory);
            var target = new DirectoryInfo(targetDirectory);

            Console.WriteLine($"{DateTime.Now:dd_MM_yyyy HH:mm:ss} - Copying from {sourceDirectory} to {targetDirectory}");

            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (var fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir.FullName, nextTargetSubDir.FullName);
            }

            if (isRootFolder)
            {
                Console.WriteLine($"{DateTime.Now:dd-mm-yyyy HH:mm:ss} - Completed");
                return targetDirectory;
            }

            return string.Empty;
        }

        private static async Task ExitApplication()
        {
            Console.WriteLine("Closing application...");

            await Task.Delay(3 * 1000);

            Environment.Exit(0);
        }

        #region Close button logic

        internal const uint SC_CLOSE = 0xF060;
        internal const uint MF_ENABLED = 0x00000000;
        internal const uint MF_GRAYED = 0x00000001;
        internal const uint MF_DISABLED = 0x00000002;
        internal const uint MF_BYCOMMAND = 0x00000000;
        public static void EnableCloseButton(IntPtr handle, bool bEnabled)
        {
            var hSystemMenu = GetSystemMenu(handle, false);
            EnableMenuItem(hSystemMenu, SC_CLOSE, (uint)(MF_ENABLED | (bEnabled ? MF_ENABLED : MF_GRAYED)));
        }

        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        #endregion Close button logic
    }
}