using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using MIU;
using System.Speech.Synthesis;

namespace OneHourRandom
{
    class Program
    {
        static string steamFilePath;
        static SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        static bool usingTTS = File.ReadAllText(@".\config.txt").Split('\n')[1].ToLower() == "true";

        static string originPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow"), @"Bad Habit\MarbleItUp\");
        static string customPath = originPath + @"CustomLevels\";
        static string challengePath = customPath + @"RandomChallenge\";

        public static void Main(string[] args)
        {
            Console.WriteLine("Setting Up Program... Please Wait");
            Console.SetWindowSize(100,40);

            Directory.CreateDirectory(customPath + "RandomChallenge");
            string logPath = originPath + "Player.log";
            steamFilePath = File.ReadAllText(@".\config.txt").Split('\n')[0];
            steamFilePath = File.ReadAllText(@".\config.txt").Split('\n')[0].Substring(0, steamFilePath.Length - 1);
            File.Delete(challengePath + "ROHC.level");
            File.Copy(@".\ROHC.level", challengePath + "ROHC.level");

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

This Can Be Useful If You Only Have One Monitor And Can't Have The Console Window Open Or-
If You Just Prefer Not To Read The Console Window And Just Get It Read Out For You.
Just Change It To 'true' In The Second Row To Enable It.

You Have 3 Skips Available Through The Game And Just Type 's' In The Console Window To Skip A Level

A Few Levels Have Been Banned From This To Give A Better Experience
Most Notably Levels Like Hell Train For Its Length, And Impossible Levels Like The HITC Re-upload
Some Levels Have Also A Less Chance Of Appearing But Can Still Show Up, These Include Levels That
Is Almost Only Possible With Analog/XInput Turning.

Levels That Have Impossible/Annoyingly Hard Diamond Times Have Been Removed
Levels That Also Is Unfair/Can Crash Certain Peoples Games Is Also Removed

Important: If Your Steam Directory Is Not The Default Path in 'Program Files (x86)\Steam',
Change The Path in 'config.txt' To Suit Your Installation Path.

To Start The Challenge, Just Type 'Start'
");

            string menuInput = Console.ReadLine().ToLower();
            if (menuInput != "start") {
                Console.WriteLine("So you've choosen death...");
                Thread.Sleep(2500);
                Environment.Exit(1);
            }
            Console.Clear();
            Console.WriteLine("Challenge Has Begun, Start Your Gaming!" +
                "\n");
            CopyRandomLevel(challengePath);
            DateTime initTime = DateTime.Now.AddHours(1);
            double pog = GetCountDown(initTime).TotalSeconds;

            float diamondTime = GetDiamondTime(challengePath + "ROHC.level");
            Console.WriteLine("Diamond Time On Current Level: " + diamondTime + " Seconds \n");
            TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");

            File.Delete(@".\Player.log");
            File.Copy(logPath, @".\Player.log");

            int latestScoreIndex = File.ReadAllText(@".\Player.log").Length - 1;
            int medalCount = 0;

            Thread inputThread = new Thread(WaitForSkip);
            inputThread.IsBackground = true;
            inputThread.Start();
            if (usingTTS == true) {
                Thread TTSThread = new Thread(TTSSpeaker);
                TTSThread.IsBackground = true;
                TTSThread.Start();
            }


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
                        Console.WriteLine("Diamond Time On Current Level: " + diamondTime + " Seconds");
                        TTSQueue.Enqueue("Diamond Time On Current Level: " + diamondTime + " Seconds");
                        medalCount += 1;
                        Console.WriteLine("Diamond Count: " + medalCount);
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
                    Console.WriteLine("You Skipped This Level, Skips Remaining: " + skipRemain);
                    TTSQueue.Enqueue("You Skipped This Level, Skips Remaining: " + skipRemain);

                    Console.WriteLine("Diamond Time On Current Level: " + diamondTime + " Seconds");
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
            Console.Clear();
            Console.WriteLine("Time Is Up! No More Gaming!");
            TTSQueue.Enqueue("Time Is Up! No More Gaming!");
            Console.WriteLine(
                
                "You Obtained: " + medalCount + " Diamond Medals In One Hour, Impressive! \n" +
                "If You Wish To Play Again \n" +
                "Restart This Program And See If You Can Beat Your Best Score!"
                
                );
            File.AppendAllText(@".\highscore.txt","Attempt " + File.ReadAllLines(@".\highscore.txt").Length + " - " + medalCount + " Diamond Medals \n");

            Console.ReadKey();
        }


        public static string[] IllegalLevels = new string[]
        {
            "2506559064", // Massive Gravity Orb Level
            "2684082316", // Cap Kingdom Level
            "2791461738", // 7-7
            "2790082937", // 7-6
            "2788029425", // 7-5
            "2787586183", // 7-4
            "2070710576", // Surfing Room
            "1800627073", // Chill Zone, all other chill zone has DT above 10 minute
            "2492180870", // Gravity Donut
            "2465277976", // Sisphysicsfhbeyrh day off
            "1617593121", // Tower Mistake 1
            "1627866427", // Tower Mistake 2
            "2508568039", // Hell Train
            "2075463805", // Diamond in the rhoughghgh
            "2078203299", // Bunny Hop
            "2499538173", // Bunny Hop 2
            "2505197399", // Head reupload
            "2573074416", // Ansons mayhem level
            "1569010731", // Pain, Privated from workshop?
            "1579037685", // Impossible ring jump
            "2575792735", // rolling over it
            "1930341412", // chill zone tall
            "1800971629", // Chill zone lite
            "2638960497", // square galaxy
            "2727269080", // burning PP
            "1577400348", // pickle planet
            "2755282390", // clusterstorm category 5 thing
        };
        public static string[] SoftIllegalLevels = new string[]
        {
            "2284477064", // Road To The Beacon
            "2712867108", // Skill Issue
            "2569510808", // Shifting Gears
            "1724086694", // Cold
            "2435422163", // stinky kickflip
            "2499791195", // neglected stars
            "2368783983", // nebula thing
            "2368611740", // altitude
            "2372621296", // gem box air control
            "2365599746", // friction gems
            "2572614504", // learning to fricking controller fricking turn
            "2546504159", // friction start
            "2372872746", // light as air jumps
            "2372839234", // simple ice skating
            "2365303808", // up & up
            "1757516807", // backroom
            "1658291414", // dark world
            "1658297109", // fractal judgement
            "1680489798", // ghost
            "1832657727", // collection kings
            "2449116866", // mountain goat
            "2276691009", // mounbain of mishaaaps
            "2727158488", // refinery bowl
        };
        public static string GetRandomLevel() {
            var folders = Directory.GetDirectories(steamFilePath);
            Random rnd = new Random();
            string random = folders[rnd.Next(folders.Length)];
            string[] splitRandom = random.Split('\\');
            string randomId = splitRandom[splitRandom.Length - 1];
            if (IllegalLevels.Contains(randomId))
            {
                return GetRandomLevel();
            }
            if (SoftIllegalLevels.Contains(randomId) && rnd.Next(2) == 1)
            {
                return GetRandomLevel();
            }

            Console.WriteLine("Level: " + Path.GetFileName(Directory.GetFiles(random)[0]));

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
        public static bool skip = false;
        public static int skipRemain = 3;

        public static void WaitForSkip()
        {
            while (true)
            {
                string input = Console.ReadKey().KeyChar.ToString();
                if (input.ToLower() == "s")
                {
                    Console.Write("\n");
                    if (skipRemain > 0)
                    {
                        skip = true;
                        skipRemain--;
                    } else
                    {
                        Console.WriteLine("No More Skips Available");
                    }
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


    }
}
