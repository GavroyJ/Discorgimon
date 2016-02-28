using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using Extensions;
/*
    //Add this to your main code:

    public deleage void DiscordQuestOutputCallback(string message);

    DiscordQuestOutputCallback discordQuestOutput = new DiscordQuestOutputCallback(Output);
    DiscordQuest discordQuest = new DiscordQuest(discordQuestOutput);
    Thread discordQuestThread = new Thread(new ThreadStart(discordQuest.Begin));
    discordQuestThread.Start();

    static void Output(string message)
    {
        //Add code to output message to chat here
    }
*/

namespace Discorgimon
{
    public delegate void DiscordQuestOutputCallback(string message);

    class Program
    {
        static void Main(string[] args)
        {
            DiscordQuestOutputCallback discordQuestOutput = new DiscordQuestOutputCallback(Output);
            DiscordQuest discordQuest = new DiscordQuest(discordQuestOutput);
            Thread discordQuestThread = new Thread(new ThreadStart(discordQuest.Begin));
            discordQuestThread.Start();

            do
            {
                discordQuest.Input("Josh", Console.ReadLine());
            } while (discordQuest.programRunning);

        }

        static void Output(string message)
        {
            Console.WriteLine($"{message}");
        }

    }

    public class DiscordQuest
    {
        //Public Datamembers
        public DiscordQuestOutputCallback outputCallbackMethod;
        public bool programRunning;

        //Private Datamembers
        List<Character> players;
        InputMessage input;
        Random random;

        //Constructor
        //callback: Method which outputs data to the text area
        public DiscordQuest(DiscordQuestOutputCallback callback)
        {
            outputCallbackMethod = callback;
            programRunning = true;
            players = new List<Character>();
            random = new Random();
        }

        //Main Thread
        public void Begin()
        {
            LoadPlayers();
            Output("Discorgi have been released, type 'help' for assistance.");

            while (programRunning)
            {
                Refresh();

                SavePlayers();
                Thread.Sleep(10000);
            }
        }

        //Input method
        //message: user input
        //  expected format - "<UserName> <command>"
        public void Input(string user, string message)
        {
            input = new InputMessage();
            input.User = user; //TODO remove hardcode
            input.Command = message.Substring(0, message.IndexOf(' ')).ToLower();
            input.Parameter = message.IndexOf(' ') > 0 ? message.Substring(message.IndexOf(' ') + 1) : "";

            switch (input.Command)
            {
                case "create": CreateCharacter(user); break;
                case "help": PrintHelp(input.Parameter); break;
                case "show": ShowPlayer(input.Parameter); break;
                case "attack": Attack(input.Parameter); break;
                case "exit": programRunning = false; break;
                case "level": Level(input.Parameter); break;
                default: Output($"Unknown Command: {input.Command}"); break;
            }
        }

        //Private Methods:
        void Refresh()
        {
            foreach (Character player in players)
            {
                if (player.Energy < 3)
                    player.Energy++;
            }
        }

        int FindPlayer(string playerName)
        {
            int playerIndex = 0;
            bool searching = true;

            while (searching && playerIndex < players.Count)
            {
                if (players[playerIndex].Name.Equals(playerName))
                {
                    searching = false;
                }
                else
                {
                    playerIndex++;
                }
            }
            if (searching)
            {
                playerIndex = -1;
            }
            return playerIndex;
        }

        void Output(string message)
        {
            if (outputCallbackMethod != null)
                outputCallbackMethod(message);
        }

        void CreateCharacter(string charactername)
        {
            Character player = new Character(charactername);
            players.Add(player);

            Output($"{player.Name} has been granted a Discorgi!");
        }

        void PrintHelp(string topic)
        {
            Output("Type, \"I wanna catch em all\" to join the battle.");
        }

        void ShowPlayer(string playerName)
        {
            if (!playerName.Equals(""))
            {
                int playerIndex = FindPlayer(playerName);

                if (playerIndex.IsFound()) //Player Found
                {
                    Output($"{players[playerIndex].Name}'s {players[playerIndex].Pet} is level {players[playerIndex].Level}");
                }
                else
                {
                    Output($"{playerName} doesn't have a Discori.");
                }
            }
            else
            {
                Output($"Usage: Show <Username>");
            }
        }

        void SavePlayers()
        {
            // serialize object to JSON
            using (StreamWriter file = File.CreateText("DQCharacterData.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, players);
            }
        }

        void LoadPlayers()
        {
            // deserialize JSON directly from a file
            if (File.Exists("DQCharacterData.json"))
            {
                using (StreamReader file = File.OpenText("DQCharacterData.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    players = (List<Character>)serializer.Deserialize(file, typeof(List<Character>));
                }
            }
        }

        void Attack(string message)
        {
            if (!input.Parameter.Equals(""))
            {
                int atkIndex = FindPlayer(input.User);
                int defIndex = FindPlayer(input.Parameter);

                if (atkIndex.IsFound())
                {
                    if (defIndex.IsFound())
                    {
                        if (players[atkIndex].Energy > 0)
                        {
                            int damage = (int)(random.Next((int)(players[atkIndex].Attack / 2), players[atkIndex].Attack) / 1 + players[defIndex].Defence);

                            players[atkIndex].Energy--;
                            players[atkIndex].AttacksMade++;
                            players[atkIndex].DamageDone += damage;
                            players[defIndex].Health -= damage;
                            players[defIndex].DamageTaken += damage;

                            Output($"{players[atkIndex].Name}'s level {players[atkIndex].Level} {players[atkIndex].Pet} deals {damage} damage to {players[defIndex].Name}'s {players[defIndex].Pet}");

                            if (players[defIndex].Health <= 0)
                            {
                                Output($"{players[defIndex].Name}'s {players[defIndex].Pet} has died.");
                                players.RemoveAt(defIndex);

                                players[atkIndex].Kills++;
                            }
                        }
                        else { Output($"Your {players[atkIndex].Pet} is too tired."); }
                    }
                    else { Output($"{input.Parameter} doesn't have a Discori."); }
                }
                else { Output($"You don't have a Discori."); }
            }
            else { Output($"Usage: Attack <Username>"); }
        }

        //DEBUG COMMANDS
        public const bool DEBUG = true;
        void Level(string message)
        {
            if (DEBUG)
                players[FindPlayer(message)].Level++;
        }

        void Debug(string message)
        {
            if (DEBUG && outputCallbackMethod != null)
                outputCallbackMethod($"DEBUG:{message}");
        }
    }

    class Character
    {
        //Player's Data
        public string Name { get; set; }
        public string Pet { get; set; }
        public int Level { get; set; }
        public int MaxHealth { get; set; }
        public int Health { get; set; }
        public int Attack { get; set; }
        public double Defence { get; set; }
        public int Energy { get; set; }

        //Player's Progress History
        public int AttacksMade { get; set; }
        public int DamageDone { get; set; }
        public int DamageTaken { get; set; }
        public int Kills { get; set; }

        public Character(string playerName)
        {
            Name = playerName;
            Pet = "Discorgi";
            Level = 1;
            MaxHealth = 100;
            Health = 10;
            Attack = 10;
            Defence = 0;
            Energy = 3;
            AttacksMade = 0;
            DamageDone = 0;
            DamageTaken = 0;
            Kills = 0;
        }
    }

    struct InputMessage
    {
        public string User { get; set; }
        public string Command { get; set; }
        public string Parameter { get; set; }
    }
}

namespace Extensions
{
    public static class DiscordQuestExtension
    {
        public static bool IsFound(this int i)
        {
            return i >= 0;
        }
    }
}