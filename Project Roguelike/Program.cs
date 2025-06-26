using System;
using System.Linq;

namespace Roguelike
{
    class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game();
            game.Run();
        }
    }

    class Game
    {
        private Map map;
        private Player player;
        private Logger logger;
        private int level = 1;
        private Random random = new Random();

        public void Run()
        {
            Init();

            while (true)
            {
                Console.Clear();
                map.Render(player, logger, level);

                if (player.Health == 0 || player.Stamina == 0)
                {
                    HandleGameOver();
                    continue;
                }

                PlayerAction();
            }
        }

        private void Init()
        {
            logger = new Logger();
            player = new Player();
            map = new Map(player, logger);
        }

        private void HandleGameOver()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(player.Health == 0 ? "You are dead!" : "You are out of stamina!");
            Console.ResetColor();
            Console.WriteLine("1: Restart\n2: Exit");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
                {
                    level = 1;
                    Init();
                    break;
                }
                else if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
                {
                    Environment.Exit(0);
                }
            }
        }

        private void PlayerAction()
        {
            int dx = 0, dy = 0;

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.W) { dx = -1; break; }
                else if (key.Key == ConsoleKey.S) { dx = 1; break; }
                else if (key.Key == ConsoleKey.A) { dy = -1; break; }
                else if (key.Key == ConsoleKey.D) { dy = 1; break; }
                else if (key.Key == ConsoleKey.I)
                {
                    player.ShowInventory(logger);
                    return;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    ShowOptions();
                    return;
                }
            }

            int newX = player.X + dx;
            int newY = player.Y + dy;

            if (map.IsValidMove(newX, newY))
            {
                char tile = map.GetTile(newX, newY);

                if (tile == '?')
                {
                    MysteryEvent e = MysteryPool.GetRandomEvent();
                    e.Trigger(player, logger);
                }
                else if (tile == 'E')
                {
                    bool goNextLevel;
                    map.Campfire.Interact(player, logger, ref level, out goNextLevel);

                    if (goNextLevel)
                    {
                        map = new Map(player, logger);
                        return;
                    }
                }

                map.MovePlayer(newX, newY);
                player.Stamina--;
            }
        }

        private void ShowOptions()
        {
            Console.Clear();
            Console.WriteLine("1: Restart\n2: Exit\n3: Continue");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
                {
                    level = 1;
                    Init();
                    break;
                }
                else if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
                {
                    Environment.Exit(0);
                }
                else if (key.Key == ConsoleKey.D3 || key.Key == ConsoleKey.NumPad3)
                {
                    break;
                }
            }
        }
    }

    class Player
    {
        public int X { get; set; }
        public int Y { get; set; }
        private int health = 30;
        private int stamina = 30;

        public List<Item> Inventory { get; } = new List<Item>();
        public Weapon EquippedWeapon { get; private set; }

        public int Health
        {
            get => health;
            set => health = Math.Clamp(value, 0, 30);
        }

        public int Stamina
        {
            get => stamina;
            set => stamina = Math.Clamp(value, 0, 30);
        }

        public void AddItem(Item item, Logger logger)
        {
            if (Inventory.Count < 9)
            {
                Inventory.Add(item);
            }
            else
            {
                Console.Clear();
                Console.WriteLine($"Inventory full! New item: {item.Name}");
                Console.WriteLine("\nCurrent Inventory:");
                for (int i = 0; i < Inventory.Count; i++)
                {
                    Console.WriteLine($"{i + 1}: {Inventory[i].Name}");
                }

                Console.WriteLine("\n1: Replace an existing item");
                Console.WriteLine("2: Drop new item");

                while (true)
                {
                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
                    {
                        Console.Clear();
                        Console.WriteLine($"Replace which item with: {item.Name}");
                        for (int i = 0; i < Inventory.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}: {Inventory[i].Name}");
                        }
                        Console.WriteLine("\nEsc: Back");

                        while (true)
                        {
                            var replaceKey = Console.ReadKey(true);

                            if (replaceKey.Key == ConsoleKey.Escape)
                            {
                                Console.Clear();
                                Console.WriteLine($"Inventory full! New item: {item.Name}");
                                Console.WriteLine("\nCurrent Inventory:");
                                for (int i = 0; i < Inventory.Count; i++)
                                {
                                    Console.WriteLine($"{i + 1}: {Inventory[i].Name}");
                                }
                                Console.WriteLine("\n1: Replace an existing item");
                                Console.WriteLine("2: Drop new item");
                                break;
                            }

                            int index = replaceKey.KeyChar - '1';

                            if (index >= 0 && index < Inventory.Count)
                            {
                                logger.Add($"\nReplaced '{Inventory[index].Name}' with '{item.Name}'");
                                Inventory[index] = item;
                                return;
                            }
                        }

                    }
                    else if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
                    {
                        Console.Write($"\nAre you sure you want to drop '{item.Name}'?");
                        Console.WriteLine("Press Y to confirm, any other key to cancel.");

                        var confirm = Console.ReadKey(true);
                        if (confirm.Key == ConsoleKey.Y)
                        {
                            logger.Add($"\nDrop: {item.Name}");
                            System.Threading.Thread.Sleep(1000);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("\nCancel drop.");
                            System.Threading.Thread.Sleep(500);
                            Console.Clear();
                            Console.WriteLine($"Inventory full! New item: {item.Name}");
                            Console.WriteLine("\nCurrent Inventory:");
                            for (int i = 0; i < Inventory.Count; i++)
                            {
                                Console.WriteLine($"{i + 1}: {Inventory[i].Name}");
                            }
                            Console.WriteLine("\n1: Replace an existing item");
                            Console.WriteLine("2: Discard new item");
                        }
                    }
                }
            }
        }



        public void ShowInventory(Logger logger)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Inventory:");

                if (Inventory.Count == 0)
                {
                    Console.WriteLine(" - Empty -");
                    Console.WriteLine("\nPress any key to return...");
                    Console.ReadKey(true);
                    return;
                }

                for (int i = 0; i < Inventory.Count; i++)
                {
                    Console.WriteLine($"{i + 1}: {Inventory[i].Name}");
                }

                Console.WriteLine("\n1: use item");
                Console.WriteLine("2: drop item");
                Console.WriteLine("Esc: back");

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
                {
                    UseItem(logger);
                    return;
                }
                else if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
                {
                    DiscardItem(logger);
                    return;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    return;
                }
            }
        }

        private void UseItem(Logger logger)
        {
            Console.WriteLine("\n[1-9]: select item to use");
            Console.WriteLine("Esc: back");
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) return;

                int index = key.KeyChar - '1';
                if (index >= 0 && index < Inventory.Count)
                {
                    Inventory[index].Use(this, logger);
                    Inventory.RemoveAt(index);
                    return;
                }
            }
        }

        private void DiscardItem(Logger logger)
        {
            Console.WriteLine("\n[1-9]: select item to drop");
            Console.WriteLine("Esc: back");
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) return;

                int index = key.KeyChar - '1';
                if (index >= 0 && index < Inventory.Count)
                {
                    var item = Inventory[index];
                    Console.WriteLine($"\nAre you sure you want to drop '{item.Name}'?\n");
                    Console.WriteLine("Press Y to confirm, any other key to cancel.");
                    var confirm = Console.ReadKey(true);
                    if (confirm.Key == ConsoleKey.Y)
                    {
                        Inventory.RemoveAt(index);
                        logger.Add($"Drop: {item.Name}");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Canceled.");
                        System.Threading.Thread.Sleep(1000);
                        return;
                    }
                }
            }
        }

        public void EquipWeapon(Weapon weapon)
        {
            if (EquippedWeapon != null)
                Inventory.Add(EquippedWeapon);

            EquippedWeapon = weapon;
        }

        public int GetAttackDamage()
        {
            return EquippedWeapon?.Damage ?? 1;
        }
    }

    class Map
    {
        private char[,] grid;
        private Player player;
        private Logger logger;
        private Random random = new Random();

        private const int VisionRadius = 3;
        private bool[,] revealed;

        private Campfire campfire;
        public Campfire Campfire => campfire;


        public Map(Player p, Logger l)
        {
            player = p;
            logger = l;
            GenerateMap();
        }

        public void GenerateMap()
        {
            grid = new char[10, 20];
            revealed = new bool[10, 20];

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    if (i == 0) grid[i, j] = '▄';
                    else if (i == 9) grid[i, j] = '▀';
                    else if (j == 0 || j == 19) grid[i, j] = '█';
                    else grid[i, j] = ' ';
                }
            }

            player.X = random.Next(1, 9);
            player.Y = random.Next(1, 19);
            grid[player.X, player.Y] = '@';

            int exitX, exitY;
            do
            {
                exitX = random.Next(1, 9);
                exitY = random.Next(1, 19);
            } while (exitX == player.X && exitY == player.Y);

            grid[exitX, exitY] = 'E';
            campfire = new Campfire(exitX, exitY);

            for (int i = 0; i < 4; i++)
            {
                int x, y;
                do
                {
                    x = random.Next(1, 9);
                    y = random.Next(1, 19);
                } while (grid[x, y] != ' ');

                grid[x, y] = '?';
            }
        }

        public bool IsValidMove(int x, int y)
        {
            return x >= 0 && x < 10 && y >= 0 && y < 20 && grid[x, y] != '█' && grid[x, y] != '▀' && grid[x, y] != '▄';
        }

        public char GetTile(int x, int y)
        {
            return grid[x, y];
        }

        public void MovePlayer(int newX, int newY)
        {
            grid[player.X, player.Y] = ' ';
            player.X = newX;
            player.Y = newY;
            grid[player.X, player.Y] = '@';
        }

        public void Render(Player player, Logger logger, int level)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    char tile = grid[i, j];
                    bool isWall = (tile == '█' || tile == '▀' || tile == '▄');

                    int dx = i - player.X;
                    int dy = j - player.Y;
                    bool inVision = dx * dx + dy * dy <= VisionRadius * VisionRadius;

                    if (!isWall && inVision)
                        revealed[i, j] = true;

                    if (isWall || revealed[i, j])
                    {
                        // Hiện đúng tile có màu
                        if (tile == '@') Console.ForegroundColor = ConsoleColor.Cyan;
                        else if (tile == '?') Console.ForegroundColor = ConsoleColor.Yellow;
                        else if (tile == 'E') Console.ForegroundColor = ConsoleColor.Magenta;

                        Console.Write(tile);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write('▒');
                        Console.ResetColor();
                    }
                }

                if (i == 1)
                {
                    Console.Write("  Health:  ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(new string('▌', player.Health));
                    Console.ResetColor();
                }
                else if (i == 3)
                {
                    Console.Write("  Stamina: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(new string('▌', player.Stamina));
                    Console.ResetColor();
                }
                else if (i == 5)
                {
                    Console.Write("  Level: ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(level);
                    Console.ResetColor();
                }
                else if (i == 7)
                {
                    Console.Write("  Weapon: ");
                    if (player.EquippedWeapon != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"{player.EquippedWeapon.Name} (+{player.EquippedWeapon.Damage})");
                    }
                    else
                    {
                        Console.Write("None");
                    }
                    Console.ResetColor();
                }


                Console.WriteLine();
            }

            Console.WriteLine("WASD: move\nI: inventory\nEsc: options\n");
            logger.Print();
        }
    }

    class Logger
    {
        private string[] logs = new string[5];

        public void Add(string message)
        {
            for (int i = 0; i < logs.Length - 1; i++)
            {
                logs[i] = logs[i + 1];
            }
            logs[logs.Length - 1] = message;
        }

        public void Print()
        {
            Console.WriteLine("Log:");
            foreach (var log in logs)
            {
                Console.WriteLine(log);
            }
        }
    }

    class Campfire
    {
        public int X { get; }
        public int Y { get; }

        public Campfire(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Interact(Player player, Logger logger, ref int level, out bool proceedToNextLevel)
        {
            Console.Clear();
            Console.WriteLine("You found a campfire!");
            Console.Write("You rested and regained your ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("stamina");
            Console.ResetColor();

            player.Stamina = 30;
            System.Threading.Thread.Sleep(2000);
            Console.ReadKey(true);

            logger.Add("You rested at the campfire. Stamina restored!");
            logger.Add("Proceeding to next level...");
            level++;

            proceedToNextLevel = true;
        }
    }

    abstract class Item
    {
        public string Name { get; }
        public Item(string name) => Name = name;
        public abstract void Use(Player player, Logger logger);
    }

    class Weapon : Item
    {
        public int Damage { get; }

        public Weapon(string name, int dmg) : base(name)
        {
            Damage = dmg;
        }

        public override void Use(Player player, Logger logger)
        {
            player.EquipWeapon(this);
            logger.Add($"Equipped weapon: {Name} (+{Damage} dmg)");
        }
    }

    class Consumable : Item
    {
        public enum EffectType { Health, Stamina }
        private EffectType Effect;
        private int Amount;

        public Consumable(string name, EffectType effect, int amount) : base(name)
        {
            Effect = effect;
            Amount = amount;
        }

        public override void Use(Player player, Logger logger)
        {
            if (Effect == EffectType.Health)
            {
                player.Health += Amount;
                logger.Add($"Healed {Amount} HP with {Name}");
            }
            else
            {
                player.Stamina += Amount;
                logger.Add($"Restored {Amount} Stamina with {Name}");
            }
        }
    }

    abstract class MysteryEvent
    {
        public abstract void Trigger(Player player, Logger logger);
    }

    class HealEvent : MysteryEvent
    {
        private int amount;
        private string message;

        public HealEvent(int amount, string message)
        {
            this.amount = amount;
            this.message = message;
        }

        public override void Trigger(Player player, Logger logger)
        {
            player.Health += amount;
            logger.Add($"{message} (+{amount} HP)");
        }
    }

    class StaminaEvent : MysteryEvent
    {
        private int amount;
        private string message;

        public StaminaEvent(int amount, string message)
        {
            this.amount = amount;
            this.message = message;
        }

        public override void Trigger(Player player, Logger logger)
        {
            player.Stamina += amount;
            logger.Add($"{message} (+{amount} Stamina)");
        }
    }

    class ItemEvent : MysteryEvent
    {
        private Item item;

        public ItemEvent(Item item)
        {
            this.item = item;
        }

        public override void Trigger(Player player, Logger logger)
        {
            player.AddItem(item, logger);
            logger.Add($"You found an item: {item.Name}");
        }
    }

    static class MysteryPool
    {
        private static Random rand = new Random();

        public static MysteryEvent GetRandomEvent()
        {
            var pool = new List<MysteryEvent>
        {
            new HealEvent(4, "You sip water from a cracked bottle."),
            new HealEvent(8, "A healing fairy blesses you."),
            new HealEvent(6, "You find a red herb and eat it."),
            new HealEvent(10, "You discover a hidden stash of food."),

            new StaminaEvent(10, "You find an energy drink."),
            new StaminaEvent(30, "A mysterious force restores your stamina."),
            new StaminaEvent(15, "You breathe deeply and feel refreshed."),
            new StaminaEvent(20, "You find a magical fruit that boosts your energy."),

            new ItemEvent(new Weapon("Rusty Sword", 3)),
            new ItemEvent(new Weapon("Wooden Club", 2)),
            new ItemEvent(new Weapon("Iron Dagger", 4)),
            new ItemEvent(new Weapon("Steel Axe", 5)),

            new ItemEvent(new Consumable("Apple", Consumable.EffectType.Health, 5)),
            new ItemEvent(new Consumable("Energy Pill", Consumable.EffectType.Stamina, 10)),
            new ItemEvent(new Consumable("Healing Potion", Consumable.EffectType.Health, 10)),
            new ItemEvent(new Consumable("Stamina Drink", Consumable.EffectType.Stamina, 15))
        };

            return pool[rand.Next(pool.Count)];
        }
    }

}