using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using MIU;
using System.Speech.Synthesis;
using System.Diagnostics;

namespace OneHourRandom
{
    class Program
    {
        //silly dimden
        //thanks to j2 for some help with troubleshooting and rng

        static string steamFilePath;
        static SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        static bool usingTTS = File.ReadAllText(@".\config.txt").Split('\n')[1].ToLower().Trim() == "true";
        static bool goldSkipping = File.ReadAllText(@".\config.txt").Split('\n')[2].ToLower().Trim() == "true";
        static bool goldSkip = false;
        public static bool skipGold = false;

        static string originPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow"), @"Bad Habit\MarbleItUp\");
        static string customPath = originPath + @"CustomLevels\";
        static string challengePath = customPath + @"RandomChallenge\";
        static string currentLevel = "";

        static int medalCount = 0;
        static float diamondTime = 0;
        static float goldTime = 0;
        static bool logOut = true;
        static DateTime initTime;

        static string random = "";
        public static List<string> allowedLevels = new List<string>();
        static bool setSeed = File.ReadAllText(@".\config.txt").Split('\n')[4].ToLower().Trim() != "false";
        static int seed = File.ReadAllText(@".\config.txt").Split('\n')[4].GetHashCode();
        public static Random rnd;

        public static void Main(string[] args)
        {
            Console.WriteLine("Setting Up Program... Please Wait");
            Console.SetWindowSize(130,50);

            Directory.CreateDirectory(customPath + "RandomChallenge");
            string logPath = originPath + "Player.log";
            steamFilePath = File.ReadAllText(@".\config.txt").Split('\n')[0];
            steamFilePath = File.ReadAllText(@".\config.txt").Split('\n')[0].Substring(0, steamFilePath.Length - 1);
            File.Delete(challengePath + "ROHC.level");
            File.Copy(@".\ROHC.level", challengePath + "ROHC.level");

            if (File.ReadAllText(@".\config.txt").Split('\n')[3].ToLower().Trim() == "true" && Process.GetProcessesByName("LogOutputDisplay").Length == 0)
            {
                Process.Start(@".\LogOutputDisplay.exe");
                Console.WriteLine("Launched UI");
            }

            if (setSeed)
            {
                //setseed here
                rnd = new Random(seed);
            }
            else
            {
                rnd = new Random();
            }

            synthesizer.SetOutputToDefaultAudioDevice();
            if (usingTTS) {
                synthesizer.Speak("TTS Activated");
            }

            Console.Clear();
            Console.WriteLine(@"
Welcome to ROHC (Random One Hour Challenge)!!
- Made by VilleOlof
- With A Lot Of Help From TalentedPlatinum (Most Notably ByteStream & Proper Log Reading)

- You Got One Hour on You to Get As Many Diamond Medals As Possible. -


Once You've Ran This Program (Which Has Happened Already)- 
You Should Have A New Custom Chapter Called 'RandomChallenge' With An Empty Level Inside.

Once You've Begun The Challenge, You Just Need to Refresh The Level Inside By Just Going In & Out Of The Chapter
And Then A Random Level Will Have Appeared.

This Program Keeps Track Of All The Medals You've Obtained And Reports It At The End.

You Can Enable TTS in the config.txt File If You Prefer, It Will Read Out New Diamond Times, 
Completions & Time Left At Some Intervals And Times Up

There's Two Skip Modes (3 Skips Only & Only Skip If Gold Medal Is Aquired)
Gold Medal Skip Mode Is Enabled By Default (Can Change in config.txt on line 3)

The Program (As You Might Have Noticed) Auto Launched A UI For You
(Can Be Disabled On Line 4 in config.txt)

The Program Generates A Random Seed If Line 5 In config.txt is 'false'
If You Change This To Anything Else That Will Be The Random Seed

You Can Change The UI (LogOutputDisplay.exe) Configs in layoutconfig.txt
Line 1 Is How Far From The Left The Window Should Be  
Line 2 Is How Far From The Top The Window Should Be  
Line 3 Is The Amount Of Time It Should Take For The Overlay To Update (Default 1500)  

Important: If Your Steam Directory Is Not The Default Path in 'Program Files (x86)\Steam',
Change The Path in 'config.txt' To Suit Your Installation Path.


!!! Before Starting, Choose Your Challenge Mode Below By Typing The Correlating Number !!!

'1' - Everything (Nothing Is Banned & Every Level Has a Chance To Get Rolled)
'2' - Chaos (Only Impossible DTs And DTs Above 10 Minutes Are Banned)
'3' - Intermediate (Only Impossible DTs And DTs Above 5 Minutes Are Banned)
'4' - 'Beginner' (Only Impossible DTs And DTs Above 3 Minutes Are Banned, Alongside A Few Noticably Hard Levels)
'5' - Shorty (Only Impossible DTs And DTs Above 1 Minutes Are Banned)
'6' - Custom (Reads Banned Levels From 'userLevels.txt', Default Is An Example File Content & Format)

");
            float DTmax = 0;
            int goldCount = 0;
            bool isEverything = false;
            string mode = "";
            List<String> bannedLevels = new List<String>();
            List<String> impossibleLevels = new List<String>() { "2506559064", "2505197399", "2573074416", "1569010731", "1579037685" };
            // massive orb gravity // head reupload // ansons mayhem level // pain // impossible ring jump // ^^^^
            List<String> challengingLevels = new List<String>() {"2684082316","2791461738","2790082937","2788029425", "2787586183","2070710576","1800627073","2492180870","2465277976","1617593121","1627866427","2508568039","2075463805","2078203299","2499538173","2575792735","1930341412","1800971629","2638960497","2727269080","1577400348","2755282390",};

            GetPhysParams(challengePath + "ROHC.level");

            string menuInput = Console.ReadLine().ToLower();
            if (menuInput == "1") {
                DTmax = -1;
                isEverything = true;
                mode = "Everything";
                Console.WriteLine("'Everything' Mode Choosen");
            }
            else if (menuInput == "2") {
                Console.WriteLine("'Chaos' Mode Choosen");
                DTmax = 600;
                mode = "Chaos";
                bannedLevels.AddRange(impossibleLevels);
            }
            else if (menuInput == "3") {
                Console.WriteLine("'Intermediate' Mode Choosen");
                DTmax = 300;
                mode = "Intermediate";
                bannedLevels.AddRange(impossibleLevels);
            }
            else if (menuInput == "4") {
                Console.WriteLine("'Beginner' Mode Choosen");
                DTmax = 90;
                mode = "Beginner";
                bannedLevels.AddRange(challengingLevels);
                bannedLevels.AddRange(impossibleLevels);
            }
            else if (menuInput == "5") {
                Console.WriteLine("'Shorty' Mode Choosen");
                DTmax = 60;
                mode = "Shorty";
                bannedLevels.AddRange(impossibleLevels);
            }
            else if (menuInput == "6") {
                DTmax = -1;
                mode = "Custom";
                bannedLevels.AddRange(new List<String>() { File.ReadAllText(@".\userLevels.txt")});
            }
            else {
                Console.WriteLine("No Mode Specified, Defaulted to 'Beginner'");
                DTmax = 90;
                mode = "Beginner";
                bannedLevels.AddRange(challengingLevels);
                bannedLevels.AddRange(impossibleLevels);
            }

                Console.WriteLine("\nSetting Up Custom Lists... \n");
            if (!isEverything)
            {
                foreach (var currentDir in Directory.GetDirectories(steamFilePath))
                {
                    string[] splitCurry = currentDir.Split('\\');
                    string curryId = splitCurry[splitCurry.Length - 1];
                    string physParams = GetPhysParams(challengePath + "ROHC.level");

                    if (!bannedLevels.Contains(curryId) && (DTmax >= 0 ? (GetDiamondTime(Directory.GetFiles(currentDir)[0]) < DTmax) : true) && (physParams == "" || physParams[0] != '{'))
                    {
                        allowedLevels.Add(currentDir);
                    }
                }
            }
            else
            {
                allowedLevels.AddRange(Directory.GetDirectories(steamFilePath));
            }

            Console.WriteLine("To Start The Challenge, Just Type 'Start'\n");
            string menuStart = Console.ReadLine().ToLower();
            if (menuStart != "start")
            {
                Console.WriteLine("So you've choosen death...");
                Thread.Sleep(2500);
                Environment.Exit(1);
            }

            CopyRandomLevel(challengePath);
            initTime = DateTime.Now.AddHours(1);
            double pog = GetCountDown(initTime).TotalSeconds;
            diamondTime = GetDiamondTime(challengePath + "ROHC.level");
            goldTime = GetGoldTime(challengePath + "ROHC.level");

            Console.Clear();
            Console.WriteLine("Challenge Has Begun, Start Your Gaming!" +
                "\n");

            Console.WriteLine("Diamond Time On Current Level: " + diamondTime + " Seconds");
            Console.WriteLine("Gold Time On Current Level: " + goldTime + " Seconds \n");
            TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");

            File.Delete(@".\Player.log");
            File.Copy(logPath, @".\Player.log");

            int latestScoreIndex = File.ReadAllText(@".\Player.log").Length - 1;


            Thread inputThread = new Thread(WaitForSkip);
            inputThread.IsBackground = true;
            inputThread.Start();
            if (usingTTS == true) {
                Thread TTSThread = new Thread(TTSSpeaker);
                TTSThread.IsBackground = true;
                TTSThread.Start();
            }
            Thread logThread = new Thread(LogOutput);
            logThread.IsBackground = true;
            logThread.Start();


            while (true) {
                string logContent = "";
                File.Delete(@".\Player.log");
                File.Copy(logPath, @".\Player.log");
                logContent = File.ReadAllText(@".\Player.log");

                try {
                    logContent = logContent.Substring(latestScoreIndex);
                }
                catch {
                    latestScoreIndex = 0;
                    logContent = File.ReadAllText(@".\Player.log");
                }

                int logIndex = logContent.IndexOf("Level Complete ");
                if (logIndex != -1) {
                    latestScoreIndex = logIndex + latestScoreIndex + 15;

                    int newLineIndex = logContent.Substring(logIndex).IndexOf("\n");
                    string completeLine = "";

                    if (newLineIndex != -1)
                    {
                        completeLine = logContent.Substring(logIndex, newLineIndex);
                    } else
                    {
                        completeLine = logContent.Substring(logIndex);
                    }

                    Console.WriteLine(completeLine);
                    

                    int timeIndex = completeLine.IndexOf("Time: ");
                    if (timeIndex == -1) {
                        throw new Exception("Time not found in complete line");
                    }

                    timeIndex += 6;
                    float time = -1f;
                    bool success = float.TryParse(completeLine.Substring(timeIndex), out time);

                    if (time < GetGoldTime(challengePath + "ROHC.level") && time > GetDiamondTime(challengePath + "ROHC.level") && goldSkipping)
                    {
                        Console.WriteLine("Beat Gold Time! You Can Now Skip This Level! \n Just Type 's' To Skip!");
                        goldSkip = true;
                        goldCount++;
                    }

                    if (!success) {
                        throw new Exception("Time was not a valid float");
                    }
                    string levelLine = logContent.Substring(logIndex+16, timeIndex-26);

                    if (time < diamondTime && levelLine == "ROHC")
                    {
                        Console.WriteLine("You Beat The Diamond Time!");
                        TTSQueue.Enqueue("You Beat The Diamond Time!");
                        CopyRandomLevel(challengePath);
                        diamondTime = GetDiamondTime(challengePath + "ROHC.level");
                        goldTime = GetGoldTime(challengePath + "ROHC.level");
                        Console.WriteLine("Diamond Time On Current Level: " + diamondTime + " Seconds");
                        Console.WriteLine("Gold Time On Current Level: " + goldTime + " Seconds");
                        TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");
                        medalCount += 1;
                        Console.WriteLine("Diamond Count: " + medalCount + " / Gold Count: " + goldCount);
                        TimeSpan timeLeft = GetCountDown(initTime);
                        Console.WriteLine($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds} \n");
                        if (medalCount == 5) {
                            TTSQueue.Enqueue($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds}");
                        }
                        else if (medalCount == 10) {
                            TTSQueue.Enqueue($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds}");
                        }
                        else if (medalCount == 20) {
                            TTSQueue.Enqueue($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds}");
                        }

                    }
                }

                if (skip) {
                    skip = false;
                    CopyRandomLevel(challengePath);
                    diamondTime = GetDiamondTime(challengePath + "ROHC.level");
                    goldTime = GetGoldTime(challengePath + "ROHC.level");
                    Console.WriteLine("You Skipped This Level, Skips Remaining: " + skipRemain);
                    TTSQueue.Enqueue("You Skipped This Level, Skips Remaining: " + skipRemain);

                    Console.WriteLine("Diamond Time On Current Level: " + diamondTime + " Seconds");
                    Console.WriteLine("Gold Time On Current Level:" + goldTime + " Seconds");
                    TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");
                    TimeSpan timeLeft = GetCountDown(initTime);
                    Console.WriteLine($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds} \n");
                    GetPhysParams(challengePath + "ROHC.level");

                }
                if (skipGold) {
                    skipGold = false;
                    goldSkip = false;
                    CopyRandomLevel(challengePath);
                    diamondTime = GetDiamondTime(challengePath + "ROHC.level");
                    goldTime = GetGoldTime(challengePath + "ROHC.level");

                    Console.WriteLine("Diamond Time On Current Level: " + diamondTime + " Seconds");
                    Console.WriteLine("Gold Time On Current Level:" + goldTime + " Seconds");
                    TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");
                    TimeSpan timeLeft = GetCountDown(initTime);
                    Console.WriteLine($"Time Left: {timeLeft.Minutes}:{timeLeft.Seconds} \n");
                }


                pog = GetCountDown(initTime).TotalSeconds;
                if (pog <= 0) {
                    break;
                }
                Thread.Sleep(1000);
            }
            logOut = false;
            Console.Clear();
            Console.WriteLine("Time Is Up! No More Gaming!");
            TTSQueue.Enqueue("Time Is Up! No More Gaming!");
            Console.WriteLine(
                
                "You Obtained: " + medalCount + " Diamond Medals In One Hour, Impressive! \n" +
                "If You Wish To Play Again \n" +
                "Restart This Program And See If You Can Beat Your Best Score!"
                
                );
            File.AppendAllText(@".\highscore.txt","Attempt " + File.ReadAllLines(@".\highscore.txt").Length + " - " + medalCount + " Diamond Medals / " + goldCount + " Gold Medals - Mode > " + mode + "\n");

            Console.ReadKey();
        }
        public static string GetRandomLevel() {
            

            random = allowedLevels[rnd.Next(allowedLevels.Count)];
            Console.WriteLine("Level: " + Path.GetFileName(Directory.GetFiles(random)[0]));
            currentLevel = Path.GetFileNameWithoutExtension(Directory.GetFiles(random)[0]);

            allowedLevels.Remove(random);
            return Directory.GetFiles(random)[0];
        }

        public static void CopyRandomLevel(string challengePath) {
            File.Delete(challengePath + "ROHC.level");
            File.Copy(GetRandomLevel(), challengePath + "ROHC.level");
        }
        public static TimeSpan GetCountDown(DateTime initTime)
        {
            TimeSpan t = initTime - DateTime.Now;
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
            } else
            {
                stream.ReadString(out _);
            }

            stream.ReadString(out physParams);
            return physParams;
        }

        public static bool skip = false;
        public static int skipRemain = 3;

        public static void WaitForSkip()
        {
            string input = "";
            if (goldSkipping)
            {
                skipRemain = 0;
            }
            while (true)
            {
                input = Console.ReadKey().KeyChar.ToString();
                if (input.ToLower() == "s" && goldSkip)
                {
                    Console.Write("\n");
                    skipGold = true;
                    //Console.WriteLine("GOLDSKIP");
                }
                else if (input.ToLower() == "s" && !goldSkip)
                {
                    Console.Write("\n");
                    if (skipRemain > 0)
                    {
                        skip = true;
                        skipRemain--;
                        //Console.WriteLine("NORMALSKIP");
                    }
                    else
                    {
                        Console.WriteLine("No Skips Available");
                    }
                }
                else
                {
                    Console.WriteLine("No Skips Available");
                }
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

        public static void LogOutput()
        {
            // Medal Count, Current Medal Time, Time Left, Skips Available, Current Level,Current Gold Medal Time, GoldSkip 
            while (logOut)
            {
                File.WriteAllText(@".\logOutput.txt", "");
                File.AppendAllText(@".\logOutput.txt",medalCount + "\n" + diamondTime.ToString() + "\n" + $"{GetCountDown(initTime).Minutes}:{GetCountDown(initTime).Seconds}" + "\n" + skipRemain + "\n" + currentLevel + "\n" + goldTime.ToString() + "\n" + goldSkip.ToString() + "\n");


                Thread.Sleep(1000);
            }
        }
    }
}
