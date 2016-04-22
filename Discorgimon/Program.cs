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
// Brad
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

        //In Seconds
        public const int REGEN_TIME = 10;
        public const int ENEMY_ACTION = 20;
        public const int ENEMY_RESPAWN = 180;

        //Public Datamembers
        public DiscorgimonOutputCallback outputCallbackMethod;
        public bool programRunning;

        //Private Datamembers
        List<Discorgi> players;
        InputMessage input;
        Random random;
        int threadCounter;

        //Constructor
        //callback: Method which outputs data to the text area
        public Discorgimon(DiscorgimonOutputCallback callback)
        {
            outputCallbackMethod = callback;
            programRunning = true;
            players = new List<Discorgi>();
            random = new Random();
            threadCounter = 0;
        }

        //Main Thread
        //If you leave this thread running for 68 years, you will encounter an overflow error
        public void Begin()
        {
            LoadPlayers();

            while (true)
            {
                Thread.Sleep(1000);
                threadCounter++; 

                if (threadCounter % REGEN_TIME == 0)
                {
                    Refresh();
                    SavePlayers();
                }
                if (threadCounter % ENEMY_ACTION == 0)
                {
                    ActivateEnemy();
                }

                if (threadCounter % ENEMY_RESPAWN == 0)
                {
                    CreateEnemy();
                }
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

            switch (input.Command.ToLower())
            {
                case "catch": CreateDiscorgi(); break;
                case "release": ReleaseDiscorgi();break;
                case "help": PrintHelp(); break;
                case "stats": StatusPlayer(); break;
                case "check": CheckPlayer(); break;
                case "attack": Attack(); break;
                case "heal": Heal(); break;
                case "showall": ShowAll(); break;
                //DEBUG
                case "testing": testing(); break;
                case "dlevel": DEBUGLevel(); break;
                case "dcreate": DEBUGCreate(); break;
                default: Output($"You failed to {input.Command}"); break;
            }
        }

        void testing()
        {
            Output("```testing");
        }

        //Private Methods:
        void Refresh()
        {
            foreach (Discorgi player in players)
            {
                player.Heal(1);
                player.Energize(1);
            }
        }

        int FindPlayer(string ownerName)
        {
            int playerIndex = 0;
            bool searching = true;

            //Search for a player with that name
            while (searching && playerIndex < players.Count)
                if (players[playerIndex].Owner.Equals(ownerName, StringComparison.OrdinalIgnoreCase))
                    searching = false;
                else
                    playerIndex++;
            //No Player Found, search for Enemy with associated Pet Name
            if (searching) 
            {
                playerIndex = 0;
                while (searching && playerIndex < players.Count)
                    if (players[playerIndex].PetName.Substring(players[playerIndex].PetName.IndexOf(' ') + 1).Equals(ownerName, StringComparison.OrdinalIgnoreCase))
                        searching = false;
                    else
                        playerIndex++;
                if (searching) //Nothing Found
                    playerIndex = -1;
            }

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
            else { Output($"You have to catch a Discorgi first."); }
        }

        void PrintHelp()
        {
            switch(input.Parameter.ToLower())
            {
                case "catch": Output("catch - Catches a Discorgi\ncatch < Name > -Catches a Discorgi and names them"); break;
                case "release": Output("release - Releases your existing Discorgi"); break;
                case "check": Output("check - Shows Info about your Discorgi\ncheck < Username > -Shows Info another users Discorgi"); break;
                case "stats": Output("stats - Shows Detailed Info about your Discorgi\nstats < Username > -Shows Detailed Info another users Discorgi"); break;
                case "attack": Output("attack <Username> - Performs an attack on another users Discorgi"); break;
                case "heal": Output("heal <Username> - Heals the specified users Discorgi"); break;
                default: Output("Commands: Catch, Release, Check, Stats, Attack, Heal"); break;
            }
        }

        void StatusPlayer()
        {
            int playerIndex = input.Parameter.Equals("") ? FindPlayer(input.User) : FindPlayer(input.Parameter);

            if (playerIndex.IsFound()) //Player Found
            {
                Output($"**【{players[playerIndex].PetName}】** is {players[playerIndex].GetStatus()}\n```Level: {players[playerIndex].Level}\nWearing: {players[playerIndex].Accessory}\nKills: {players[playerIndex].Kills}\nHealing Done: {players[playerIndex].HealingDone}\nDamage Dealt: {players[playerIndex].DamageDone}\nDamage Taken: {players[playerIndex].DamageTaken}```");
            }
            else
            {
                Output($"{(input.Parameter.Equals("") ? input.User : input.Parameter)} doesn't have a Discorgi.");
            }
        }

        void CheckPlayer()
        {
            int playerIndex = input.Parameter.Equals("") ? FindPlayer(input.User) : FindPlayer(input.Parameter);

            if (playerIndex.IsFound()) //Player Found
            {
                Output($"**【{players[playerIndex].PetName}】** is wearing {players[playerIndex].Accessory} and {players[playerIndex].GetStatus()}");
            }
            else
            {
                Output($"{(input.Parameter.Equals("") ? input.User : input.Parameter)} doesn't have a Discorgi.");
            }
        }

        void SavePlayers()
        {
            // serialize object to JSON
            using (StreamWriter file = File.CreateText("DiscorgimonData.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, players);
            }
        }

        void LoadPlayers()
        {
            // deserialize JSON directly from a file
            if (File.Exists("DiscorgimonData.json"))
            {
                using (StreamReader file = File.OpenText("DiscorgimonData.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    players = (List<Discorgi>)serializer.Deserialize(file, typeof(List<Discorgi>));
                }
            }
        }

        void Attack()
        {
            if (ValidTarget())
            {
                int atkIndex = FindPlayer(input.User);
                int defIndex = FindPlayer(input.Parameter);

                if (atkIndex != defIndex)
                {
                    if (players[atkIndex].Energy > 0)
                    {
                        int damage = (int)(random.Next((int)(players[atkIndex].Attack / 2), players[atkIndex].Attack) / 1 + players[defIndex].Defence);

                        players[atkIndex].Energy--;
                        players[atkIndex].AttacksMade++;
                        players[atkIndex].DamageDone += damage;
                        if(players[atkIndex].AddExp(2))
                            Output($"**【{players[atkIndex].PetName}】** has reached level {players[atkIndex].Level}!");

                        players[defIndex].Health -= damage;
                        players[defIndex].DamageTaken += damage;

                        Output($"**【{players[atkIndex].PetName}】** performs {Discorgi.RandomAttack()} dealing {damage} damage to **【{players[defIndex].PetName}】**");

                        if (players[defIndex].Health <= 0)
                        {
                            Output($"**【{ players[defIndex].PetName}】** has died.");

                            players[atkIndex].Kills++;
                            if(players[atkIndex].AddExp(13))
                                Output($"**【{players[atkIndex].PetName}】** has reached level {players[atkIndex].Level}!");

                            players.RemoveAt(defIndex);
                        }

                    }
                    else { Output($"**【{players[atkIndex].PetName}】** is too tired."); }
                }
                else { Output($"**【{players[atkIndex].PetName}】** chases his tail."); }
            }
        }

        void Heal()
        {
            if (ValidTarget())
            {
                int atkIndex = FindPlayer(input.User);
                int defIndex = FindPlayer(input.Parameter);

                if (players[atkIndex].Energy > 0)
                {
                    if (atkIndex != defIndex)
                    {
                        int healing = (int)(random.Next((int)(players[atkIndex].Attack / 2), players[atkIndex].Attack) * 1.5);

                        players[atkIndex].Energy--;
                        players[atkIndex].HealsMade++;
                        players[atkIndex].HealingDone += healing;
                        if(players[atkIndex].AddExp(1))
                            Output($"**【{players[atkIndex].PetName}】** has reached level {players[atkIndex].Level}!");

                        players[defIndex].Heal(healing);

                        Output($"**【{players[atkIndex].PetName}】** performs {Discorgi.RandomHeal()} healing **【{players[defIndex].PetName}】** for {healing}");
                    }
                    else
                    {
                        int healing = (int)(random.Next((int)(players[atkIndex].Attack / 2), players[atkIndex].Attack) * 0.7);

                        players[atkIndex].Energy--;
                        players[atkIndex].HealsMade++;
                        players[atkIndex].HealingDone += healing;

                        players[defIndex].Heal(healing);

                        Output($"**【{players[atkIndex].PetName}】** performs {Discorgi.RandomHeal()} healing themself for {healing}");
                    }


                }
                else { Output($"**【{players[atkIndex].PetName}】** is too tired."); }
            }
        }

        bool ValidTarget()
        {
            bool validity = false;

            if (!input.Parameter.Equals(""))
            {
                int atkIndex = FindPlayer(input.User);
                int defIndex = FindPlayer(input.Parameter);

                if (atkIndex.IsFound())
                {
                    if (defIndex.IsFound())
                    {
                        validity = true;
                    }
                    else { Output($"{input.Parameter} doesn't have a Discorgi."); }
                }
                else { Output($"You have to catch a Discorgi first."); }
            }
            else { Output($"Usage: {input.Command} <Username>"); }

            return validity;
        }

        void CreateEnemy()
        {
            //Create Enemy if one does not exist
            int playerIndex = FindPlayer("Enemy");

            if (!playerIndex.IsFound())
            {
                Discorgi player = new Discorgi("Enemy", Discorgi.RandomEnemyName());

                //LEVEL UP!
                int enemyLevel = random.Next(0, 5);
                for (int i = 0; i < enemyLevel; i++) { player.LevelUp(); }

                players.Add(player);

                Output($"A wild level {player.Level} **【{player.PetName}】** has appeared!");
            }
        }

        void ActivateEnemy()
        {
            int playerIndex = FindPlayer("Enemy");

            //If enemy exists, it attacks another random player
            if(playerIndex.IsFound() && players.Count > 1)
            {
                int atkIndex = FindPlayer("Enemy");
                int defIndex;
                do{defIndex = random.Next(players.Count);}
                while (defIndex == atkIndex);

                int damage = (int)(random.Next((int)(players[atkIndex].Attack / 2), players[atkIndex].Attack) / 1 + players[defIndex].Defence);

                players[atkIndex].AttacksMade++;
                players[atkIndex].DamageDone += damage;

                players[defIndex].Health -= damage;
                players[defIndex].DamageTaken += damage;

                Output($"**【{players[atkIndex].PetName}】** performs {Discorgi.RandomAttack()} dealing {damage} damage to **【{players[defIndex].PetName}】**");

                if (players[defIndex].Health <= 0)
                {
                    Output($"**【{ players[defIndex].PetName}】** has died.");

                    players[atkIndex].Kills++;

                    players.RemoveAt(defIndex);
                }
            }
        }

        void ShowAll()
        {
            bool firstpass = true;
            string output = string.Empty;
            foreach(Discorgi player in players)
            {
                if(firstpass)
                {
                    firstpass = false;
                    output += ($"```{player.Owner} - {player.PetName} ({player.Level})");
                }
                else if(!player.Owner.Equals("Enemy"))
                {
                    output+= ($"\n{player.Owner} - {player.PetName} ({player.Level})");
                }
            }
            Output(output + "```");
        }

        //DEBUG COMMANDS
        void DEBUGLevel()
        {
            int playerIndex = FindPlayer(input.Parameter);
            if (DEBUG && playerIndex.IsFound())
                players[playerIndex].LevelUp();
        }

        //Command: dcreate <owner name>
        //Description: Creates a discorgi named DEBUG with provided owner
        void DEBUGCreate()
        {
            if (DEBUG)
            {
                input.User = input.Parameter;
                input.Parameter = "DEBUG " + input.Parameter;
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
        public string Accessory { get; set; }

        public int Level { get; set; }
        public int ExpToLevel { get; set; }
        public int Exp { get; set; }

        public int MaxHealth { get; set; }
        public int Health { get; set; }

        public int MaxEnergy { get; set; }
        public int Energy { get; set; }

        public int Attack { get; set; }
        public double Defence { get; set; }
        

        //Player's Progress History
        public int AttacksMade { get; set; }
        public int DamageDone { get; set; }
        public int DamageTaken { get; set; }
        public int HealsMade { get; set; }
        public int HealingDone { get; set; }
        public int Kills { get; set; }

        public Discorgi(string owner, string pet)
        {
            Owner = owner;
            PetName = pet;
            Accessory = RandomAccesory();
            
            Level = 1;
            ExpToLevel = 20;
            Exp = 0;

            MaxHealth = 100;
            Health = MaxHealth;

            MaxEnergy = 3;
            Energy = MaxEnergy;

            Attack = 10;
            Defence = 0;

            AttacksMade = 0;
            DamageDone = 0;
            DamageTaken = 0;
            HealsMade = 0;
            HealingDone = 0;
            Kills = 0;
        }

        internal void Heal(int amount)
        {
            if (Health < MaxHealth)
            {
                Health += amount;
                if (Health > MaxHealth)
                    Health = MaxHealth;
            }
        }

        internal void Energize(int amount)
        {
            if (Energy < MaxEnergy)
            {
                Energy += amount;
                if (Energy > MaxEnergy)
                    Energy = MaxEnergy;
            }
        }

        internal bool AddExp(int exp)
        {
            bool causedLevelUp = false;
            Exp += exp;
            if(Exp >= ExpToLevel)
            {
                causedLevelUp = true;
                LevelUp();
            }
            return causedLevelUp;
        }

        internal string GetStatus()
        {
            //not dead yet
            //breathing heavy

            //<playername> is"
            string status = "doing awesome!";

            if (Health < MaxHealth * 0.2)
                status = "pretty fucked up...";
            else if (Health < MaxHealth * 0.6)
                status = "not doing great";
            else if (Health < MaxHealth * 0.8)
                status = "doing fine";

             return status;
        }

        internal void LevelUp()
        {
            Level++;
            MaxHealth += 10;
            Attack += 5;
            Defence += 0.1;
            Exp -= ExpToLevel;
            ExpToLevel += 20;
        }


        static String[] Names =
        {
            "Angel", "Bagel", "Bellatrix", "Biscuit", "Bobbafett", "Bubbles", "Candy", "Cimmanom", "Cleopatra", "Cupcake", "Doc", "Felix", "Jade", "Marshmellow", "Muggles", "Noodle", "Officer McSmiggles", "Paddington", "Pixie", "President Dwayne Elizondo Mountain Dew Herbert Camacho", "Robin Hood", "Ruby", "Shady Dave", "Tank", "Tonka", "Waddles", "Waffle", "Wilbur", "Yoshi", "Zelda",
        };

        internal static string RandomName()
        {
            Random random = new Random();
            return Names[random.Next(Names.Length)];
        }

        static String[] Accesories =
        {
            //wearing 
            "glasses", "a headband", "a chefs hat", "a thor costume", "nunchucks", "the cursed blade Muramasa", "earings", "a snuggie", "their childhood blanket", "a frying pan", "slippers", "a waffle iron", "glitter", "sunglasses", "a penguin costume", "a hairnet", "a thing", "a hoodie", "a houseplant", "nothing ;)", "a broken discoball", "a letters jacket", "a football helmet", "a moustache", "an octopus hat", "a graduation gown", "too much cologne", "a poncho", "a helmet", "a noose :(", "16 dog years of regret", "a no ragrets tattoo", "a newspaper hat", "way too many hats",
        };

        internal static string RandomAccesory()
        {
            Random random = new Random();
            return Accesories[random.Next(Accesories.Length)];
        }

        static String[] MovesPrefix =
        {
            //Does
            "a Rolling", "an Awkward", "a Shameful", "an Alpha", "an Unexplainable", "an Omega", "a Destined", "a Useless", "a Power", "a Doomsday", "a Consecutive", "a Corkscrew", "a Flying", "a Double", "an Illegal", "a Testicular", "an Ultimate", "a Devistating", "a Dragon", "a Paragon", "a Giga", "a Terra", "a Cyber", "a Glowing", "a Dominating", "a Crazed", "a Rocket", "a Swift", "a Super", "an Uber", "a Glorious", "Secret Move:", "an Uncoordinated", "a Graceless", "a Problematic", "a Dazzeling", "a Triumphant", "an Enjoyable", "a Wet Sounding", "a Girlie", "a Spastic", "a Mocking",
        };

        static String[] AttackSuffix =
        {
            "Backflip", "Tackle", "Punch", "Karate Chop", "Rocket Fist", "Fireball", "Dropkick", "Heel Drop", "Elbow Drop", "Skull Crucher", "Slide", "Bicycle Kick", "Pun", "Explotion", "Blast", "Drillbreak", "Wedgie", "Meteor", "Missile", "Smash", "Curse", "Slash", "Lance", "Blaster", "Pelvic Thrust", "Juggle", "Blizzard", "Lightning", "Kick", "Wiggle", "Technique", "Wet Willie", "Indian Burn", "Noogie", "Feint", "Slap",
        };

        static String[] HealSuffix =
        {
            "Praise","Compliment", "Pick-Me-Up", "Backrub", "Nuzzle", "Life Coaching", "Inside Joke", "Highfive", "Chant", "Incantation", "Song", "Serenade", "Encourage", "Cheer", "Gift", "Hug", "Smooch", "Piggy-Back Ride", "Tickle",
        };

        internal static string RandomAttack()
        {
            Random random = new Random();
            return MovesPrefix[random.Next(MovesPrefix.Length)] + " " + AttackSuffix[random.Next(AttackSuffix.Length)];
        }

        internal static string RandomHeal()
        {
            Random random = new Random();
            return MovesPrefix[random.Next(MovesPrefix.Length)] + " " + HealSuffix[random.Next(HealSuffix.Length)];
        }

        static String[] EnemySuffix =
        {
            "Vacuum", "Monkey", "Dinosaur", "Evangelist", "Sock Puppet", "Action Figure", "Barbie", "Colosus", "Angel", "Rhino", "Butler", "Gary", "Justin Bieber", "Dragon",
        };

        //TODO: Need Unique Boss Prefix

        internal static string RandomEnemyName()
        {
            Random random = new Random();
            return MovesPrefix[random.Next(MovesPrefix.Length)] + " " + EnemySuffix[random.Next(EnemySuffix.Length)];
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