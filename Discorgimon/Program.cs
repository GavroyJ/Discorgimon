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

namespace Discorgimon
{
    public delegate void DiscorgimonOutputCallback(string message);

    class Program
    {
        static void Main(string[] args)
        {
            DiscorgimonOutputCallback discorgimonOutput = new DiscorgimonOutputCallback(Output);
            Discorgimon discorgimon = new Discorgimon(discorgimonOutput);
            Thread discorgimonThread = new Thread(new ThreadStart(discorgimon.Begin));
            discorgimonThread.Start();

            do
            {
                discorgimon.Input("Josh", Console.ReadLine());
            } while (discorgimon.programRunning);

        }

        static void Output(string message)
        {
            Console.WriteLine($"{message}");
        }

    }

    public class Discorgimon
    {
        //Allows Debug Commands and Debug Output
        public const bool DEBUG = true;

        //Public Datamembers
        public DiscorgimonOutputCallback outputCallbackMethod;
        public bool programRunning;

        //Private Datamembers
        List<Discorgi> players;
        InputMessage input;
        Random random;

        //Constructor
        //callback: Method which outputs data to the text area
        public Discorgimon(DiscorgimonOutputCallback callback)
        {
            outputCallbackMethod = callback;
            programRunning = true;
            players = new List<Discorgi>();
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
            input.User = user;
            if (message.Contains(" "))
            {
                input.Command = message.Substring(0, message.IndexOf(' ')).ToLower();
                input.Parameter = message.IndexOf(' ') > 0 ? message.Substring(message.IndexOf(' ') + 1) : "";
            }
            else
            {
                input.Command = message;
                input.Parameter = "";
            }

            switch (input.Command)
            {
                case "catch": CreateDiscorgi(); break;
                case "release": ReleaseDiscorgi();break;
                case "help": PrintHelp(); break;
                case "show": ShowPlayer(); break;
                case "attack": Attack(); break;
                case "exit": programRunning = false; break;
                    //DEBUG
                case "dlevel": DEBUGLevel(); break;
                case "dcreate": DEBUGCreate(); break;
                default: Output($"Unknown Command: {input.Command}"); break;
            }
        }

        //Private Methods:
        void Refresh()
        {
            foreach (Discorgi player in players)
            {
                if (player.Energy < player.MaxEnergy)
                    player.Energy++;
                if (player.Health < player.MaxHealth)
                    player.Health++;
            }
        }

        int FindPlayer(string ownerName)
        {
            int playerIndex = 0;
            bool searching = true;

            while (searching && playerIndex < players.Count)
                if (players[playerIndex].Owner.Equals(ownerName,StringComparison.OrdinalIgnoreCase))
                    searching = false;
                else
                    playerIndex++;
            if (searching)
                playerIndex = -1;

            return playerIndex;
        }

        void Output(string message)
        {
            if (outputCallbackMethod != null)
                outputCallbackMethod(message);
        }

        void CreateDiscorgi()
        {
            int playerIndex = FindPlayer(input.User);

            if (!playerIndex.IsFound())
            {
                Discorgi player = new Discorgi(input.User, input.Parameter.Equals("") ? Discorgi.RandomName() : input.Parameter);

                players.Add(player);

                Output($"{input.User} captured a Discorgi wearing **{player.Accessory}** named **【{player.PetName}】**!");
            }
            else { Output($"You already have **【{players[playerIndex].PetName}】**"); }
        }

        void ReleaseDiscorgi()
        {
            int playerIndex = FindPlayer(input.User);

            if (playerIndex.IsFound())
            {
                Output($"{input.User} punts **【{players[playerIndex].PetName}】** back into the wild!");

                players.RemoveAt(playerIndex);
            }
            else { Output($"You don't have a Discrogi."); }
        }

        void PrintHelp()
        {
            Output("Type, \"Catch\" to get your own Discorgi.");
        }

        void ShowPlayer()
        {
            if (!input.Parameter.Equals(""))
            {
                int playerIndex = FindPlayer(input.Parameter);

                if (playerIndex.IsFound()) //Player Found
                {
                    Output($"**【{players[playerIndex].PetName}】** is {players[playerIndex].GetStatus()}");
                    Output($"Level: {players[playerIndex].Level}");
                    Output($"Wearing: {players[playerIndex].Accessory}");
                    Output($"Kills: {players[playerIndex].Kills}");
                    Output($"Damage Dealt: {players[playerIndex].DamageDone}");
                    Output($"Damage Taken: {players[playerIndex].DamageTaken}");
                }
                else
                {
                    Output($"{input.Parameter} doesn't have a Discori.");
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
                    players = (List<Discorgi>)serializer.Deserialize(file, typeof(List<Discorgi>));
                }
            }
        }

        void Attack()
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

                            Output($"**【{players[atkIndex].PetName}】** deals {damage} damage to **【{players[defIndex].PetName}】**");

                            if (players[defIndex].Health <= 0)
                            {
                                Output($"{players[defIndex].PetName} has died.");

                                players[atkIndex].Kills++;

                                players.RemoveAt(defIndex);
                            }
                        }
                        else { Output($"**【{players[atkIndex].PetName} is too tired."); }
                    }
                    else { Output($"{input.Parameter} doesn't have a Discori."); }
                }
                else { Output($"You don't have a Discori."); }
            }
            else { Output($"Usage: Attack <Username>"); }
        }

        //DEBUG COMMANDS
        void DEBUGLevel()
        {
            if (DEBUG)
                players[FindPlayer(input.Parameter)].LevelUp();
        }

        //Command: dcreate <owner name>
        //Description: Creates a discorgi named DEBUG with provided owner
        void DEBUGCreate()
        {
            if (DEBUG)
            {
                input.User = input.Parameter;
                input.Parameter = "DEBUG";
                CreateDiscorgi();
            }
        }
        void Debug(string message)
        {
            if (DEBUG && outputCallbackMethod != null)
                outputCallbackMethod($"DEBUG:{message}");
        }
    }

    class Discorgi
    {
        //Player's Data
        public string Owner { get; set; }
        public string PetName { get; set; }
        public int Level { get; set; }
        public int MaxHealth { get; set; }
        public int Health { get; set; }
        public int MaxEnergy { get; set; }
        public int Energy { get; set; }
        public int Attack { get; set; }
        public double Defence { get; set; }
        public string Accessory { get; set; }

        //Player's Progress History
        public int AttacksMade { get; set; }
        public int DamageDone { get; set; }
        public int DamageTaken { get; set; }
        public int Kills { get; set; }

        public Discorgi(string owner, string pet)
        {
            Owner = owner;
            PetName = pet;
            Level = 1;
            MaxHealth = 100;
            Health = MaxHealth;
            MaxEnergy = 3;
            Energy = MaxEnergy;
            Attack = 10;
            Defence = 0;
            Accessory = RandomAccesory();

            AttacksMade = 0;
            DamageDone = 0;
            DamageTaken = 0;
            Kills = 0;
        }

        internal string GetStatus()
        {
            //not dead yet
            //breathing heavy

            //<playername> is"
            string status = "doing AWESOME!";

            if (Health < MaxHealth * 0.2)
                status = "pretty fucked up...";
            else if (Health < MaxHealth * 0.6)
                status = "not great";
            else if (Health < MaxHealth * 0.8)
                status = "fine";

             return status;
        }

        internal void LevelUp()
        {
            Level++;
            MaxHealth += 10;
            Attack += 5;
            Defence += 0.1;
        }


        static String[] Names =
        {
            "Angel",
            "Bagel",
            "Bellatrix",
            "Biscuit",
            "Bobbafett",
            "Bubbles",
            "Candy",
            "Cimmanom",
            "Cleopatra",
            "Cupcake",
            "Doc",
            "Felix",
            "Jade",
            "Marshmellow",
            "Muggles",
            "Noodle",
            "Officer McSmiggles",
            "Paddington",
            "Pixie",
            "President Dwayne Elizondo Mountain Dew Herbert Camacho",
            "Robin Hood",
            "Ruby",
            "Shady Dave",
            "Tank",
            "Tonka",
            "Waddles",
            "Waffle",
            "Wilbur",
            "Yoshi",
            "Zelda",
        };

        internal static string RandomName()
        {
            Random random = new Random();

            return Names[random.Next(Names.Length)];
        }

        static String[] Accesories =
        {
            //wearing 
            "glasses",
            "a headband",
            "a chefs hat",
            "a thor costume",
            "nunchucks",
            "the cursed blade Muramasa",
            "earings",
            "a snuggie",
            "their childhood blanket",
            "a frying pan",
            "slippers",
            "a waffle iron",
            "glitter",
            "sunglasses",
            "a penguin costume",
            "a hairnet",
            "a thing",
            "a hoodie",
            "a houseplant",
            "nothing ;)",
            "a broken discoball",
            "a letters jacket",
            "a football helmet",
            "a moustache",
            "an octopus hat",
            "a graduation gown",
            "too much cologne",
            "a poncho",
            "a helmet",
            "a noose :(",
            "16 dog years of regret",
            "a no ragrets tattoo",
        };

        internal static string RandomAccesory()
        {
            Random random = new Random();

            return Accesories[random.Next(Accesories.Length)];
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
    public static class DiscorgimonExtension
    {
        public static bool IsFound(this int i)
        {
            return i >= 0;
        }
    }
}