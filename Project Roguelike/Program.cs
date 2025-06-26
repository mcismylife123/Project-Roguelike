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
                    int dmg = random.Next(1, 6);
                    player.Health -= dmg;
                    logger.Add($"You encountered an enemy! -{dmg}Hp");
                }
                else if (tile == 'E')
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

                    level++;
                    logger.Add("You rested at the campfire. Stamina restored!");
                    logger.Add("Proceeding to next level...");

                    map = new Map(player, logger);
                    return;
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
    }

    class Map
    {
        private char[,] grid;
        private Player player;
        private Logger logger;
        private Random random = new Random();
        private const int VisionRadius = 2;
        private bool[,] revealed;

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

                Console.WriteLine();
            }

            Console.WriteLine("WASD: move\nEsc: options\n");
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
}