using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;
using System.Speech.Synthesis;
using MIU;

namespace LogOutputDisplay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        static string windowLeft = "";
        static string windowTop = "";
        static string threadSleep = "";
        public static bool endlessMode = false;
        public static DateTime initTime = DateTime.Now;
        public static List<string> allowedLevels = new List<string>();
        static bool setSeed = false;
        static int seed = 0;
        public static Random rnd;
        public static int randomSeed = Environment.TickCount;
        public static TimeSpan counter;
        public static TimeSpan finishedTime;

        public static MainWindow instance;

        public MainWindow()
        {
            File.WriteAllText(@".\ROHC Files\ErrorLog.txt", "");
            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
            {
                File.AppendAllText(@".\ROHC Files\ErrorLog.txt", $"" +
                    $"TIMESTAMP: {DateTime.Now} - " +
                    $"EXCEPTION: {eventArgs.Exception.ToString()}" +
                    $"\n");
            };

            InitializeComponent();
            instance = this;
            // ################################################ YOU SHALL NOT PASS ##########################################################################
            Logger.Init();
            Config.Init();

            Logger.isActive = Config.configValues["Debug"].ToLower().Trim() == "true";
            Logger.Log($"Logger UI: {Logger.isActive}");
            Height = Logger.isActive ? 400 : 235;

            windowLeft = Config.configValues["WindowLeft"];
            windowTop = Config.configValues["WindowTop"];
            threadSleep = Config.configValues["LayoutUpdate"];
            endlessMode = Config.configValues["EndlessMode"].ToLower().Trim() == "true";

            setSeed = Config.configValues["Seed"].ToLower().Trim() != "false";
            seed = Config.configValues["Seed"].GetHashCode();

            if (setSeed)
            {
                //setseed here
                Program.rnd = new Random(seed);
                Logger.Log($"Set Seed: {seed}");
            }
            else
            {
                Program.rnd = new Random(randomSeed);
                Logger.Log($"Random Seed: {randomSeed}");
            }
            if (seed.ToString().Contains("69") || randomSeed.ToString().Contains("420")) {
                authorField.Content = "ROHC By Dimden";
            }

            this.Left = Double.Parse(windowLeft);
            this.Top = Double.Parse(windowTop);


            DebugLabel.Visibility = Logger.isActive ? Visibility.Visible : Visibility.Hidden;
            DebugContainer.Visibility = Logger.isActive ? Visibility.Visible : Visibility.Hidden;
            int currentmode = Convert.ToInt32(Config.configValues["ChallengeMode"]) - 1;
            string[] modeArray = new string[] { "Everything", "Chaos", "Intermediate", "Normal", "Shorty", "Custom" };
            Program.currentLevel = $"Press Start! | Mode: [{(endlessMode ? "Endless" : modeArray[currentmode])}]";
            string seconds = "";
            string minutes = "";
            Task.Run(() =>
            {
                while (true)
                {
                    TimeSpan timeSince = Program.GetCountUp(initTime);
                    string medalCount = Program.medalCount.ToString();
                    string medalTime = Program.diamondTime.ToString();
                    counter = (!endlessMode ? Program.GetCountDown(Program.initTime) : timeSince);
                    if (Program.challengeEnded)
                    {
                        counter = finishedTime;
                    }

                    if (counter.Seconds < 10) {
                        seconds = "0" + counter.Seconds.ToString();
                    }
                    else { seconds = counter.Seconds.ToString(); }
                    if (counter.Minutes < 10)
                    {
                        minutes = "0" + counter.Minutes.ToString();
                    }
                    else { minutes = counter.Minutes.ToString(); }

                    string timeLeft = (endlessMode ? "Time Spent:" : "Time Left:") + $" {minutes}:{seconds}";
                    if (!Program.challengeStarted || (Program.challengeEnded && !endlessMode)) {
                        timeLeft = (endlessMode ? "Time Spent:" : "Time Left:") + " 00:00";
                    }
                    string skipsAvailable = Program.skipRemain.ToString() + " Skips - ";
                    string levelName = Program.currentLevel;
                    string goldTime = Program.goldTime.ToString();
                    string goldSkip = " - Gold Skip: " +Program.goldSkip.ToString();
                    
                    if (endlessMode){
                        //timeLeft = "";
                        skipsAvailable = "";
                        goldSkip = "";
                    }
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        medals.Content = medalCount + " Medals - " + medalTime + " Seconds";
                        skipsAndTime.Content = skipsAvailable + timeLeft;
                        level.Content = "" + levelName;
                        gold.Content = (endlessMode ? $"{Program.goldCount} Medals - " : "") + goldTime + " Seconds" + goldSkip;
                    }), DispatcherPriority.Render);
                    Thread.Sleep(int.Parse(threadSleep));
                }
            });

        }

        public void closeApp(object sender, RoutedEventArgs e)
        {
            Logger.Log($"closeApp click event");
            Environment.Exit(0);
        }
        public void startApp(object sender, RoutedEventArgs e)
        {
            Logger.Log("startApp click event");
            if (endlessMode) {
                Logger.Log("EndlessMode Init");
                Program.allowedLevels.AddRange(Directory.GetDirectories(Config.configValues["SteamPath"]));
                doneButtonName.Visibility = Visibility.Visible;
                initTime = DateTime.Now;
                Program.challengeStarted = true;
                Program.CopyRandomLevel(Program.challengePath);
                Program.diamondTime = Program.GetDiamondTime(Program.challengePath + "ROHC.level");
                Program.goldTime = Program.GetGoldTime(Program.challengePath + "ROHC.level");
                Program.skipRemain = 0;

                Thread endlog = new Thread(EndlessLog);
                endlog.IsBackground = true;
                endlog.Start();
                Logger.Log("Finished Setting Up EndlessMode");

                button1.Content = "Next";
                button1.Click -= startApp;
                button1.Click += nextButton;
            }
            else {
                Logger.Log("Normal ROHC Mode Init");
                Program.currentLevel = "### Initializing...";
                Thread MainStart = new Thread(Program.MainStartUp);
                MainStart.IsBackground = true;
                MainStart.Start();

                button1.Content = "Skip";
                button1.Click -= startApp;
                button1.Click += skipButton;
            }
            
        }
        public void skipButton(object sender, RoutedEventArgs e)
        {
            Logger.Log("skipButton click event");
            Program.WaitForSkip();
        }
        public void nextButton(object sender, RoutedEventArgs e)
        {
            Logger.Log("nextButton click event");
            Program.CopyRandomLevel(Program.challengePath);
            Program.medalHistory.Add("Skipped");
            Program.diamondTime = Program.GetDiamondTime(Program.challengePath + "ROHC.level");
            Program.goldTime = Program.GetGoldTime(Program.challengePath + "ROHC.level");
            Logger.Log("Finished nextButton sequence");
        }
        public void doneButton(object sender, RoutedEventArgs e)
        {
            Logger.Log("doneButton click event");
            Program.challengeEnded = true;
            Program.currentLevel = "Finished! Great Attempt!";
            Program.goldTime = 0;
            Program.diamondTime = 0;
            Program.HighscoreSaving();
            finishedTime = counter;

            button1.Visibility = Visibility.Hidden;
            doneButtonName.Visibility = Visibility.Hidden;
            Logger.Log("Finished doneButton events");
        }

        public static void EndlessLog()
        {
            Logger.Log("Began EndlessMode Logging Thread");
            string playerPath = @".\ROHC Files\Player.log";
            string logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow"), @"Bad Habit\MarbleItUp\Player.log");
            File.Delete(playerPath);
            File.Copy(logPath, playerPath);
            int latestScoreIndex = File.ReadAllText(playerPath).Length - 1;
            while (true)
            {
                string logContent = "";
                File.Delete(playerPath);
                File.Copy(logPath, playerPath);
                logContent = File.ReadAllText(playerPath);

                try
                {
                    logContent = logContent.Substring(latestScoreIndex);
                }
                catch
                {
                    latestScoreIndex = 0;
                    logContent = File.ReadAllText(@".\ROHC Files\Player.log");
                }

                int logIndex = logContent.IndexOf("Level Complete ");
                if (logIndex != -1)
                {
                    latestScoreIndex = logIndex + latestScoreIndex + 15;

                    int newLineIndex = logContent.Substring(logIndex).IndexOf("\n");
                    string completeLine = "";

                    if (newLineIndex != -1)
                    {
                        completeLine = logContent.Substring(logIndex, newLineIndex);
                    }
                    else
                    {
                        completeLine = logContent.Substring(logIndex);
                    }

                    Logger.Log(completeLine);
                    int timeIndex = completeLine.IndexOf("Time: ");
                    timeIndex += 6;
                    float time = -1f;
                    bool success = float.TryParse(completeLine.Substring(timeIndex), out time);
                    if (!success) {
                        throw new Exception("Time was not a valid float");
                    }
                    string levelLine = logContent.Substring(logIndex + 16, timeIndex - 26);

                    if (time < Program.GetGoldTime(Program.challengePath + "ROHC.level") && time > Program.GetDiamondTime(Program.challengePath + "ROHC.level"))
                    {
                        Program.goldCount++;
                        Program.medalHistory.Add("Gold Medal");
                        Program.CopyRandomLevel(Program.challengePath);
                        Program.diamondTime = Program.GetDiamondTime(Program.challengePath + "ROHC.level");
                        Program.goldTime = Program.GetGoldTime(Program.challengePath + "ROHC.level");
                    }
                    else if (time < Program.diamondTime && levelLine == "ROHC")
                    {
                        Program.medalCount++;
                        Program.medalHistory.Add("Diamond Medal");
                        Program.CopyRandomLevel(Program.challengePath);
                        Program.diamondTime = Program.GetDiamondTime(Program.challengePath + "ROHC.level");
                        Program.goldTime = Program.GetGoldTime(Program.challengePath + "ROHC.level");
                    }


                }
                Thread.Sleep(1000);
            }
        }

        public void UpdateDebugLog(string newText)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                DebugLabel.Text = newText;
                DebugContainer.ScrollToVerticalOffset(DebugContainer.ExtentHeight);
            }), DispatcherPriority.Render);

            
        }
    }
    
    // RACOON CLASS
    public class Logger
    {
        private static readonly uint maxBuffer = 50;
        private static readonly string logFile = @".\ROHC Files\Log.txt";

        private static List<string> logBuffer = new List<string>();

        private static DateTime startTime;

        public static bool isActive = false;

        public static void Init()
        {
            startTime = DateTime.Now;
            File.WriteAllText(logFile, "");
        }

        public static void Log(object msg)
        {
            string message = msg.ToString();

            if (logBuffer.Count > maxBuffer)
            {
                logBuffer = logBuffer.Skip((int)(logBuffer.Count - maxBuffer)).ToList();
            }

            logBuffer.Add(message);
            
            string logText = string.Join("\n", logBuffer);

            if (isActive)
            {
                MainWindow.instance.UpdateDebugLog(logText);
            }
            
            double secondsSinceStart = (DateTime.Now - startTime).TotalSeconds;

            message = message.Replace("\n", $"\n[{secondsSinceStart}]");

            File.AppendAllText(logFile, $"[{secondsSinceStart}] {message}\n");
        }
    }

    // RACOON CLASS
    public class Config
    {
        private static Dictionary<string, string> _configValues = new Dictionary<string, string>();
        public static Dictionary<string, string> configValues
        {
            get { return _configValues; }
        }

        private static readonly string configPath = @".\ROHC Files\config.txt";

        public static void Init()
        {
            string[] configLines = File.ReadAllLines(configPath);

            for (int i = 0; i < configLines.Length; i++)
            {
                string configLine = configLines[i];
                string[] splitConfig = configLine.Split(':');

                if (configLine == "" || configLine.StartsWith("#") || splitConfig.Length < 2)
                {
                    Logger.Log($"Skipping line: {i + 1}");
                    continue;
                }
                
                string configKey = splitConfig[0];
                string configValue = string.Join(":", splitConfig.Skip(1).ToArray());

                if (_configValues.ContainsKey(configKey)) {
                    Logger.Log($"Line: {i + 1} CONTAINS DUPLICATE KEY, skipping");
                    continue;
                }

                _configValues[configKey] = configValue;

                Logger.Log($"Config registered [{configKey}] : [{configValue}]");
            }
        }

        public static bool HasConfigValue(string key)
        {
            return _configValues.ContainsKey(key);
        }
        
    }

    public class Program
    {
        //silly dimden
        //thanks to j2 for some help with troubleshooting and rng

        public static string steamFilePath;
        public static string configPath = @".\ROHC Files\config.txt";
        static SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        static bool usingTTS = false;
        static bool goldSkipping = false;
        public static bool goldSkip = false;
        public static bool skipGold = false;

        static string originPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow"), @"Bad Habit\MarbleItUp\");
        static string customPath = originPath + @"CustomLevels\";
        public static string challengePath = customPath + @"RandomChallenge\";
        public static string currentLevel = "Starting Up...";

        public static int medalCount = 0;
        public static float diamondTime = 0;
        public static float goldTime = 0;
        public static bool logOut = true;
        public static DateTime initTime;

        static string random = "";
        public static List<string> allowedLevels = new List<string>();
        static bool setSeed = false;
        static int seed = 0;
        public static Random rnd;
        public static int randomSeed = Environment.TickCount;

        public static List<string> levelHistory = new List<string>();
        public static List<string> medalHistory = new List<string>();
        static int minutes = 0;
        public static bool challengeStarted = false;
        public static bool challengeEnded = false;
        public static List<string> bannedLevels = new List<string>();
        public static string mode = "";
        public static int goldCount = 0;

        public static void MainStartUp()
        {
            Logger.Log("Setting Up Program...");

            usingTTS = Config.configValues["TTS"].ToLower().Trim() == "true";
            goldSkipping = Config.configValues["GoldSkip"].ToLower().Trim() == "true";

            minutes = Convert.ToInt32(Config.configValues["TimeLimit"].ToLower().Trim());
            Logger.Log("Setup MainStartUp Configs");

            if (goldSkipping) {
                skipRemain = 0;
            }
            else {
                skipRemain = 3;
            }

            Directory.CreateDirectory(customPath + "RandomChallenge");
            string logPath = originPath + "Player.log";
            steamFilePath = Config.configValues["SteamPath"];
            steamFilePath = Config.configValues["SteamPath"].Substring(0, steamFilePath.Length - 1);
            File.Delete(challengePath + "ROHC.level");
            File.Copy(@".\ROHC Files\ROHC.level", challengePath + "ROHC.level");
            Logger.Log("Fixed Temp ROHC.level");



            synthesizer.SetOutputToDefaultAudioDevice();
            if (usingTTS) {
                synthesizer.Speak("TTS Activated");
                Logger.Log("Activated TTS");
            }
            float DTmax = 0;
            bool isEverything = false;

            List<String> impossibleLevels = new List<String>() { "2506559064", "2505197399", "2573074416", "1569010731", "1579037685" };
            // massive orb gravity // head reupload // ansons mayhem level // pain // impossible ring jump // ^^^^
            List<String> challengingLevels = new List<String>() { "2684082316", "2791461738", "2790082937", "2788029425", "2787586183", "2070710576", "1800627073", "2492180870", "2465277976", "1617593121", "1627866427", "2508568039", "2075463805", "2078203299", "2499538173", "2575792735", "1930341412", "1800971629", "2638960497", "2727269080", "1577400348", "2755282390", };

            GetPhysParams(challengePath + "ROHC.level");

            string menuInput = Config.configValues["ChallengeMode"].ToLower().Trim();
            if (menuInput == "1")
            {
                DTmax = -1;
                isEverything = true;
                mode = "Everything";
                Logger.Log("'Everything' Mode Choosen");
            }
            else if (menuInput == "2")
            {
                Logger.Log("'Chaos' Mode Choosen");
                DTmax = 600;
                mode = "Chaos";
                bannedLevels.AddRange(impossibleLevels);
            }
            else if (menuInput == "3")
            {
                Logger.Log("'Intermediate' Mode Choosen");
                DTmax = 300;
                mode = "Intermediate";
                bannedLevels.AddRange(impossibleLevels);
            }
            else if (menuInput == "4")
            {
                Logger.Log("'Normal' Mode Choosen");
                DTmax = 90;
                mode = "Normal";
                bannedLevels.AddRange(challengingLevels);
                bannedLevels.AddRange(impossibleLevels);
            }
            else if (menuInput == "5")
            {
                Logger.Log("'Shorty' Mode Choosen");
                DTmax = 60;
                mode = "Shorty";
                bannedLevels.AddRange(impossibleLevels);
            }
            else if (menuInput == "6")
            {
                DTmax = -1;
                mode = "Custom";
                bannedLevels.AddRange(new List<String>() { File.ReadAllText(@".\ROHC Files\userLevels.txt") });
            }
            else
            {
                Logger.Log("No Mode Specified, Defaulted to 'Normal'");
                DTmax = 90;
                mode = "Normal";
                bannedLevels.AddRange(challengingLevels);
                bannedLevels.AddRange(impossibleLevels);
            }

            Logger.Log("\nSetting Up Custom Lists... \n");
            if (!isEverything)
            {
                foreach (var currentDir in Directory.GetDirectories(steamFilePath))
                {
                    string[] splitCurry = currentDir.Split('\\');
                    string curryId = splitCurry[splitCurry.Length - 1];
                    string physParams = GetPhysParams(Directory.GetFiles(currentDir)[0]);

                    if (!bannedLevels.Contains(curryId) && (DTmax >= 0 ? (GetDiamondTime(Directory.GetFiles(currentDir)[0]) < DTmax) : true) && (physParams == "" || physParams[0] != '{'))
                    {
                        allowedLevels.Add(currentDir);
                    }
                    else
                    {
                        bannedLevels.Add(curryId);
                    }
                    currentLevel = System.IO.Path.GetFileNameWithoutExtension(Directory.GetFiles(currentDir)[0]);
                    //currentLevel = $"Processing: {curryId}";
                    //Logger.Log("DIR " + Directory.GetFiles(currentDir)[0] + "   PARAM; " + physParams);
                }
            }
            else
            {
                allowedLevels.AddRange(Directory.GetDirectories(steamFilePath));
            }
            Logger.Log("Finished Setting Up Custom Lists");

            CopyRandomLevel(challengePath);
            initTime = DateTime.Now.AddMinutes(minutes);
            challengeStarted = true;
            double pog = GetCountDown(initTime).TotalSeconds;
            diamondTime = GetDiamondTime(challengePath + "ROHC.level");
            goldTime = GetGoldTime(challengePath + "ROHC.level");

            Logger.Log("First Level Setup Finished");

            Logger.Log("Challenge Has Begun, Start Your Gaming!" +
                "\n");

            Logger.Log("Diamond Time On Current Level: " + diamondTime + " Seconds");
            Logger.Log("Gold Time On Current Level: " + goldTime + " Seconds \n");
            TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");

            string playerPath = @".\ROHC Files\Player.log";
            File.Delete(playerPath);
            File.Copy(logPath, playerPath);

            int latestScoreIndex = File.ReadAllText(playerPath).Length - 1;


            /*Thread inputThread = new Thread(WaitForSkip);
            inputThread.IsBackground = true;
            inputThread.Start();*/
            if (usingTTS == true)
            {
                Thread TTSThread = new Thread(TTSSpeaker);
                TTSThread.IsBackground = true;
                TTSThread.Start();
            }


            while (true)
            {
                string logContent = "";
                File.Delete(playerPath);
                File.Copy(logPath, playerPath);
                logContent = File.ReadAllText(playerPath);

                try
                {
                    logContent = logContent.Substring(latestScoreIndex);
                }
                catch
                {
                    latestScoreIndex = 0;
                    logContent = File.ReadAllText(@".\ROHC Files\Player.log");
                }

                int logIndex = logContent.IndexOf("Level Complete ");
                if (logIndex != -1)
                {
                    latestScoreIndex = logIndex + latestScoreIndex + 15;

                    int newLineIndex = logContent.Substring(logIndex).IndexOf("\n");
                    string completeLine = "";

                    if (newLineIndex != -1)
                    {
                        completeLine = logContent.Substring(logIndex, newLineIndex);
                    }
                    else
                    {
                        completeLine = logContent.Substring(logIndex);
                    }

                    Logger.Log(completeLine);


                    int timeIndex = completeLine.IndexOf("Time: ");
                    if (timeIndex == -1)
                    {
                        throw new Exception("Time not found in complete line");
                    }

                    timeIndex += 6;
                    float time = -1f;
                    bool success = float.TryParse(completeLine.Substring(timeIndex), out time);

                    if (time < GetGoldTime(challengePath + "ROHC.level") && time > GetDiamondTime(challengePath + "ROHC.level") && goldSkipping)
                    {
                        Logger.Log("Beat Gold Time! You Can Now Skip This Level! \n Just Type 's' To Skip!");
                        goldSkip = true;
                        goldCount++;
                    }

                    if (!success)
                    {
                        throw new Exception("Time was not a valid float");
                    }
                    string levelLine = logContent.Substring(logIndex + 16, timeIndex - 26);

                    if (time < diamondTime && levelLine == "ROHC")
                    {
                        Logger.Log("You Beat The Diamond Time!");
                        TTSQueue.Enqueue("You Beat The Diamond Time!");
                        CopyRandomLevel(challengePath);
                        diamondTime = GetDiamondTime(challengePath + "ROHC.level");
                        goldTime = GetGoldTime(challengePath + "ROHC.level");
                        Logger.Log("Diamond Time On Current Level: " + diamondTime + " Seconds");
                        Logger.Log("Gold Time On Current Level: " + goldTime + " Seconds");
                        TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");
                        medalCount += 1;
                        Logger.Log("Diamond Count: " + medalCount + " / Gold Count: " + goldCount);
                        TimeSpan timeLeft = GetCountDown(initTime);
                        Logger.Log($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds} \n");
                        if (medalCount == 5)
                        {
                            TTSQueue.Enqueue($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds}");
                        }
                        else if (medalCount == 10)
                        {
                            TTSQueue.Enqueue($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds}");
                        }
                        else if (medalCount == 20)
                        {
                            TTSQueue.Enqueue($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds}");
                        }
                        medalHistory.Add("Diamond Medal");

                    }
                }

                if (skip)
                {
                    skip = false;
                    CopyRandomLevel(challengePath);
                    diamondTime = GetDiamondTime(challengePath + "ROHC.level");
                    goldTime = GetGoldTime(challengePath + "ROHC.level");
                    Logger.Log("Skipped This Level, Skips Remaining: " + skipRemain);
                    TTSQueue.Enqueue("You Skipped This Level, Skips Remaining: " + skipRemain);

                    Logger.Log("Diamond Time On Current Level: " + diamondTime + " Seconds");
                    Logger.Log("Gold Time On Current Level:" + goldTime + " Seconds");
                    TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");
                    TimeSpan timeLeft = GetCountDown(initTime);
                    Logger.Log($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds} \n");
                    GetPhysParams(challengePath + "ROHC.level");
                    medalHistory.Add("Skipped");

                }
                if (skipGold)
                {
                    skipGold = false;
                    goldSkip = false;
                    CopyRandomLevel(challengePath);
                    diamondTime = GetDiamondTime(challengePath + "ROHC.level");
                    goldTime = GetGoldTime(challengePath + "ROHC.level");

                    Logger.Log("Diamond Time On Current Level: " + diamondTime + " Seconds");
                    Logger.Log("Gold Time On Current Level:" + goldTime + " Seconds");
                    TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");
                    TimeSpan timeLeft = GetCountDown(initTime);
                    Logger.Log($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds} \n");
                    medalHistory.Add("Gold Skipped");
                }


                pog = GetCountDown(initTime).TotalSeconds;
                if (pog <= 0)
                {
                    break;
                }
                Thread.Sleep(1000);
            }
            currentLevel = "Time's Up!";
            challengeEnded = true;
            diamondTime = 0;
            goldTime = 0;

            File.Delete(challengePath + "ROHC.level");
            File.Copy(@".\ROHC Files\ROHC.level", challengePath + "ROHC.level");
            Logger.Log("Fixed Temp ROHC.level");

            Logger.Log("Time Is Up! No More Gaming!");
            TTSQueue.Enqueue("Time Is Up! No More Gaming!");
            Logger.Log(

                "You Obtained: " + medalCount + " Diamond Medals In One Hour, Impressive! \n" +
                "If You Wish To Play Again \n" +
                "Restart This Program And See If You Can Beat Your Best Score!"

                );
            HighscoreSaving();

            skipRemain = 0;

            //File.AppendAllText(@".\highscore.txt","Attempt " + File.ReadAllLines(@".\highscore.txt").Length + " - " + medalCount + " Diamond Medals / " + goldCount + " Gold Medals - Mode > " + mode + "\n");
        }
        public static void HighscoreSaving()
        {
            int fileCount = Directory.GetFiles(@".\Highscores", "*.txt").Length + 1;
            string highscorePath = $@".\Highscores\Attempt {fileCount}.txt";

            string highscoreSkipRemain = "";
            if (skipRemain == 0)
            {
                highscoreSkipRemain = "";
            }
            else
            {
                highscoreSkipRemain = "\n" + "Skips Left: " + skipRemain.ToString();
            }
            var fileStream = File.Create(highscorePath);
            fileStream.Close();

            if (medalHistory.Count() == 0)
            {
                medalHistory.Add("Didn't Finish");
            }
            else
            {
                medalHistory.Insert(medalHistory.Count(), "Didn't Finish");
            }

            File.AppendAllText(highscorePath, $@"
Attempt Number #{fileCount}
{highscoreSkipRemain}
Diamond Medals: {medalCount}
Gold Medals: {goldCount}
Challenge Mode: {mode}
Endless Mode: {MainWindow.endlessMode}

Level History:
");
            for (int i = 0; i < levelHistory.Count; i++)
            {
                File.AppendAllText(highscorePath, $@"#{i + 1} - {levelHistory[i]} / {medalHistory[i]}" + "\n");
            }
            int highscoreSeed = randomSeed;
            if (setSeed)
            {
                highscoreSeed = seed;
            }

            File.AppendAllText(highscorePath, "\n\n" +
                $@"Set Seed: {setSeed}" + "\n" +
                $@"Seed: {highscoreSeed}" + "\n" +
                $@"Used TTS: {usingTTS}" + "\n" +
                $@"Used Gold Skipping: {goldSkipping}" + "\n" +
                (!MainWindow.endlessMode ? $@"Time Limit: {minutes} Minute(s)\n" : $"Time Spent {(MainWindow.counter.TotalMinutes < 1 ? 0 : Math.Floor(MainWindow.counter.TotalMinutes))}:{(MainWindow.counter.Seconds < 10 ? $"0{MainWindow.counter.Seconds}" : $"{MainWindow.counter.Seconds}")}\n") +
                $@"Allowed Level Count: {allowedLevels.Count()}");

            File.AppendAllText(highscorePath, "\n\n\n" + (MainWindow.endlessMode ? "" : $@"Banned Levels:") + "\n");
            for (int i = 0; i < bannedLevels.Count; i++)
            {
                int lineAmount = File.ReadLines(highscorePath).Count();
                File.AppendAllText(highscorePath, $"\"{bannedLevels[i]}\", ");
                if (File.ReadLines(highscorePath).Skip(lineAmount - 1).Take(1).First().Count() == 140)
                {
                    File.AppendAllText(highscorePath, " \n");
                }
            }
        }

        public static string GetRandomLevel()
        {
            random = allowedLevels[rnd.Next(allowedLevels.Count)];
            //Logger.Log("Level: " + System.IO.Path.GetFileName(Directory.GetFiles(random)[0]));
            levelHistory.Add(System.IO.Path.GetFileNameWithoutExtension(Directory.GetFiles(random)[0]));
            currentLevel = System.IO.Path.GetFileNameWithoutExtension(Directory.GetFiles(random)[0]);

            allowedLevels.Remove(random);
            Logger.Log($"Got New Random Level: {System.IO.Path.GetFileName(Directory.GetFiles(random)[0])}");
            return Directory.GetFiles(random)[0];
        }

        public static void CopyRandomLevel(string challengePath)
        {
            Logger.Log("Copied Random Level");
            File.Delete(challengePath + "ROHC.level");
            File.Copy(GetRandomLevel(), challengePath + "ROHC.level");
        }
        public static TimeSpan GetCountDown(DateTime initTime)
        {
            TimeSpan t = initTime - DateTime.Now;
            return t;
        }
        public static TimeSpan GetCountUp(DateTime initTime)
        {
            TimeSpan t = (DateTime.Now - initTime);
            return t;
        }

        public static float GetDiamondTime(string filePath)
        {
            var stream = new ByteStream();
            stream.Buffer = File.ReadAllBytes(filePath);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out _);

            float diamondTime;
            stream.ReadSingle(out _);
            stream.ReadSingle(out _);
            stream.ReadSingle(out diamondTime);
            //Logger.Log($"Got DT For: {filePath}");
            return diamondTime;
        }

        public static float GetGoldTime(string filePath)
        {
            var stream = new ByteStream();
            stream.Buffer = File.ReadAllBytes(filePath);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out _);

            float goldTime;
            stream.ReadSingle(out _);
            stream.ReadSingle(out goldTime);
            //Logger.Log($"Got GT For: {filePath}");
            return goldTime;
        }
        public static string GetPhysParams(string filePath)
        {
            byte pog;
            var stream = new ByteStream();
            stream.Buffer = File.ReadAllBytes(filePath);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out _);
            stream.ReadByte(out pog);
            stream.ReadByte(out _);
            stream.ReadByte(out _);

            string physParams;
            stream.ReadSingle(out _);
            stream.ReadSingle(out _);
            stream.ReadSingle(out _);

            bool legacy = false;

            if ((char)pog == '5')
            {
                legacy = true;
            }
            else
            {
                stream.ReadString(out _);
            }

            stream.ReadString(out physParams);
            //Logger.Log($"Got PhysParams For: {filePath}");
            return physParams;
        }

        public static bool skip = false;
        public static int skipRemain = 3;

        public static void WaitForSkip()
        {


            if (goldSkip)
            {
                Console.Write("\n");
                skipGold = true;
                //Logger.Log("GOLDSKIP");
            }
            else if (!goldSkip)
            {
                Console.Write("\n");
                if (skipRemain > 0)
                {
                    skip = true;
                    skipRemain--;
                    //Logger.Log("NORMALSKIP");
                }
                else
                {
                    Logger.Log("No Skips Available");
                }
            }
            else
            {
                Logger.Log("No Skips Available");
            }
        }

        public static Queue<string> TTSQueue = new Queue<string>();

        public static void TTSSpeaker()
        {
            while (true)
            {
                if (TTSQueue.Count == 0)
                {
                    Thread.Sleep(500);
                    continue;
                }
                string current = TTSQueue.Dequeue();
                synthesizer.Speak(current);
            }
        }
    }
}
