using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Text;
using System.Threading;
using System.Drawing;

namespace cli_life
{
    public class Settings
    {
        public int width { get; set; }
        public int height { get; set; }
        public double liveDensity { get; set; }
    }

    public class Cell
    {
        public bool isAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool isAliveNext;

        public void determineNextLiveState()
        {
            int liveNeighbors = neighbors.Count(x => x.isAlive);
            if (isAlive)
                isAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                isAliveNext = liveNeighbors == 3;
        }

        public void advance()
        {
            isAlive = isAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] cells;
        public readonly int cellSize;

        public int columns => cells.GetLength(0);
        public int rows => cells.GetLength(1);
        public int width => columns * cellSize;
        public int height => rows * cellSize;

        static Dictionary<string, List<(int dx, int dy)>> patterns = new()
        {
            { "block", new List<(int, int)> { (0, 0), (1, 0), (0, 1), (1, 1) } },
            { "beehive", new List<(int, int)> { (1, 0), (2, 0), (0, 1), (3, 1), (1, 2), (2, 2) } },
            { "loaf", new List<(int, int)> { (1, 0), (2, 0), (0, 1), (3, 1), (1, 2), (3, 2), (2, 3) } },
            { "boat", new List<(int, int)> { (0, 0), (1, 0), (0, 1), (2, 1), (1, 2) } },
            { "tub", new List<(int, int)> { (1, 0), (0, 1), (2, 1), (1, 2) } }
        };

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            this.cellSize = cellSize;
            cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                cells[x, y] = new Cell();
            connectNeighbors();
            randomize(liveDensity);
        }

        readonly Random rand = new();

        public void randomize(double liveDensity)
        {
            foreach (var cell in cells)
                cell.isAlive = rand.NextDouble() < liveDensity;
        }

        public void advance()
        {
            foreach (var cell in cells)
                cell.determineNextLiveState();
            foreach (var cell in cells)
                cell.advance();
        }

        private void connectNeighbors()
        {
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : columns - 1;
                    int xR = (x < columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : rows - 1;
                    int yB = (y < rows - 1) ? y + 1 : 0;
                    cells[x, y].neighbors.Add(cells[xL, yT]);
                    cells[x, y].neighbors.Add(cells[x, yT]);
                    cells[x, y].neighbors.Add(cells[xR, yT]);
                    cells[x, y].neighbors.Add(cells[xL, y]);
                    cells[x, y].neighbors.Add(cells[xR, y]);
                    cells[x, y].neighbors.Add(cells[xL, yB]);
                    cells[x, y].neighbors.Add(cells[x, yB]);
                    cells[x, y].neighbors.Add(cells[xR, yB]);
                }
            }
        }

        public void saveState(string filename)
        {
            using var writer = new StreamWriter(filename);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                    writer.Write(cells[x, y].isAlive ? '1' : '0');
                writer.WriteLine();
            }
        }

        public static Board loadState(string filename)
        {
            var lines = File.ReadAllLines(filename);
            int rows = lines.Length;
            int columns = lines[0].Length;
            var board = new Board(columns, rows, 1, 0);
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                board.cells[x, y].isAlive = lines[y][x] == '1';
            return board;
        }

        public int countLiveCells()
        {
            int cnt = 0;
            foreach (var cell in cells)
                if (cell.isAlive)
                    cnt++;
            return cnt;
        }

        public int countCombinations()
        {
            var visited = new HashSet<Cell>();
            int combos = 0;
            foreach (var cell in cells)
            {
                if (cell.isAlive && !visited.Contains(cell))
                {
                    combos++;
                    var stack = new Stack<Cell>();
                    stack.Push(cell);
                    while (stack.Count > 0)
                    {
                        var c = stack.Pop();
                        if (!visited.Add(c)) continue;
                        foreach (var n in c.neighbors)
                            if (n.isAlive && !visited.Contains(n))
                                stack.Push(n);
                    }
                }
            }

            return combos;
        }

        public Dictionary<string, int> countPatterns()
        {
            var counts = patterns.Keys.ToDictionary(k => k, k => 0);
            foreach (var kv in patterns)
            {
                var name = kv.Key;
                var shape = kv.Value;
                for (int x = 0; x < columns; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        bool ok = true;
                        foreach (var d in shape)
                        {
                            int xx = (x + d.dx + columns) % columns;
                            int yy = (y + d.dy + rows) % rows;
                            if (!cells[xx, yy].isAlive)
                            {
                                ok = false;
                                break;
                            }
                        }

                        if (ok)
                            counts[name]++;
                    }
                }
            }

            return counts;
        }

        // запускает серию экспериментов, сохраняет data.txt и plot.png
        public static void runExperiments(int width, int height, int generations)
        {
            var densities = new double[] { 0.3, 0.5, 0.8 };
            var results = new Dictionary<double, int[]>();

            // готовим доску и считаем
            foreach (var d in densities)
            {
                var board = new Board(width, height, 1, d);
                var counts = new int[generations];
                for (int g = 0; g < generations; g++)
                {
                    counts[g] = board.countLiveCells();
                    board.advance();
                }

                results[d] =
                    counts;
            }

            var basePath = "../../../";
            var dataFile = Path.Combine(basePath, "data.txt");
            // сохраняем данные
            using (var w = new StreamWriter(dataFile))
            {
                w.Write("gen");
                foreach (var d in densities) w.Write($";{d}");
                w.WriteLine();
                for (int g = 0; g < generations; g++)
                {
                    w.Write(g);
                    foreach (var d in densities) w.Write($";{results[d][g]}");
                    w.WriteLine();
                }
            }

            // строим график
            int wPx = 800, hPx = 600, margin = 50;
            var bmp = new Bitmap(wPx, hPx);
            using var gph = Graphics.FromImage(bmp);
            gph.Clear(Color.White);

            // оси
            gph.DrawLine(Pens.Black, margin, hPx - margin, wPx - margin, hPx - margin);
            gph.DrawLine(Pens.Black, margin, margin, margin, hPx - margin);

            // масштаб
            int maxCount = densities.Select(d => results[d].Max()).Max();
            float xScale = (wPx - 2 * margin) / (float)(generations - 1);
            float yScale = (hPx - 2 * margin) / (float)maxCount;

            var pens = new[] { Pens.Orange, Pens.Red, Pens.Purple };
            for (int i = 0; i < densities.Length; i++)
            {
                var d = densities[i];
                for (int g = 1; g < generations; g++)
                {
                    float x1 = margin + (g - 1) * xScale;
                    float y1 = hPx - margin - results[d][g - 1] * yScale;
                    float x2 = margin + g * xScale;
                    float y2 = hPx - margin - results[d][g] * yScale;
                    gph.DrawLine(pens[i], x1, y1, x2, y2);
                }
            }

            // легенда
            for (int i = 0; i < densities.Length; i++)
            {
                gph.DrawRectangle(pens[i], wPx - margin - 120, margin + i * 20, 10, 10);
                gph.DrawString($"ρ={densities[i]}", SystemFonts.DefaultFont, Brushes.Black,
                    wPx - margin - 100, margin + i * 20 - 2);
            }

            bmp.Save(Path.Combine(basePath, "plot.png"));
        }
    }

    class Program
    {
        static Board board;
        static Settings settings;

        static void loadSettings()
        {
            var json = File.ReadAllText("../../../settings.json");
            settings = JsonSerializer.Deserialize<Settings>(json);
        }

        static void reset()
        {
            board = new Board(
                width: settings.width,
                height: settings.height,
                cellSize: 1,
                liveDensity: settings.liveDensity
            );
        }

        static void render()
        {
            for (int row = 0; row < board.rows; row++)
            {
                for (int col = 0; col < board.columns; col++)
                    Console.Write(board.cells[col, row].isAlive ? '*' : '.');
                Console.Write('\n');
            }
        }

        static void Main(string[] args)
        {
            loadSettings();
            if (args.Length > 0 && File.Exists($"../../../{args[0]}"))
                board = Board.loadState($"../../../{args[0]}");
            else
                reset();

            while (true)
            {
                Console.Clear();
                render();

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.P)
                    {
                        board.saveState("../../../pause_state.txt");
                        Console.WriteLine("Поле сохранено в pause_state.txt");

                        int live = board.countLiveCells();
                        int combos = board.countCombinations();
                        Console.WriteLine($"Живых клеток: {live}, комбинаций: {combos}");

                        var patterns = board.countPatterns();
                        Console.WriteLine("Статические фигуры:");
                        foreach (var kv in patterns)
                            Console.WriteLine($"{kv.Key}: {kv.Value}");

                        Console.Write("Продолжить? (Y/N): ");
                        while (true)
                        {
                            var choice = Console.ReadKey(true).Key;
                            if (choice == ConsoleKey.Y) break;
                            if (choice == ConsoleKey.N)
                            {
                                Board.runExperiments(
                                    settings.width,
                                    settings.height,
                                    100 // число поколений
                                );
                                return;
                            }
                        }
                    }
                }

                board.advance();
                Thread.Sleep(1000);
            }
        }
    }
}