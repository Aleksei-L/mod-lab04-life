using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Threading;

namespace cli_life {
	public class Settings {
		public int width { get; set; }
		public int height { get; set; }
		public double liveDensity { get; set; }
	}

	public class Cell {
		public bool isAlive;
		public readonly List<Cell> neighbors = new List<Cell>();
		private bool isAliveNext;

		public void determineNextLiveState() {
			int liveNeighbors = neighbors.Count(x => x.isAlive);
			if (isAlive)
				isAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
			else
				isAliveNext = liveNeighbors == 3;
		}

		public void advance() {
			isAlive = isAliveNext;
		}
	}

	public class Board {
		public readonly Cell[,] cells;
		public readonly int cellSize;

		public int columns => cells.GetLength(0);
		public int rows => cells.GetLength(1);
		public int width => columns * cellSize;
		public int height => rows * cellSize;

		static Dictionary<string, List<(int dx, int dy)>> patterns = new() {
			{ "block", new List<(int, int)> { (0, 0), (1, 0), (0, 1), (1, 1) } },
			{ "beehive", new List<(int, int)> { (1, 0), (2, 0), (0, 1), (3, 1), (1, 2), (2, 2) } },
			{ "loaf", new List<(int, int)> { (1, 0), (2, 0), (0, 1), (3, 1), (1, 2), (3, 2), (2, 3) } },
			{ "boat", new List<(int, int)> { (0, 0), (1, 0), (0, 1), (2, 1), (1, 2) } },
			{ "tub", new List<(int, int)> { (1, 0), (0, 1), (2, 1), (1, 2) } }
		};

		public Board(int width, int height, int cellSize, double liveDensity = .1) {
			this.cellSize = cellSize;
			cells = new Cell[width / cellSize, height / cellSize];
			for (int x = 0; x < columns; x++) {
				for (int y = 0; y < rows; y++) {
					cells[x, y] = new Cell();
				}
			}

			connectNeighbors();
			randomize(liveDensity);
		}

		readonly Random rand = new();

		public void randomize(double liveDensity) {
			foreach (var cell in cells)
				cell.isAlive = rand.NextDouble() < liveDensity;
		}

		public void advance() {
			foreach (var cell in cells)
				cell.determineNextLiveState();
			foreach (var cell in cells)
				cell.advance();
		}

		private void connectNeighbors() {
			for (int x = 0; x < columns; x++) {
				for (int y = 0; y < rows; y++) {
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

		public void saveState(string filename) {
			using var writer = new StreamWriter(filename);
			for (int y = 0; y < rows; y++) {
				for (int x = 0; x < columns; x++)
					writer.Write(cells[x, y].isAlive ? '1' : '0');
				writer.WriteLine();
			}
		}

		public static Board loadState(string filename) {
			var lines = File.ReadAllLines(filename);
			int rows = lines.Length;
			int columns = lines[0].Length;
			var board = new Board(columns, rows, 1, 0);
			for (int y = 0; y < rows; y++)
			for (int x = 0; x < columns; x++)
				board.cells[x, y].isAlive = lines[y][x] == '1';
			return board;
		}

		public int countLiveCells() {
			int counter = 0;
			foreach (var cell in cells) {
				if (cell.isAlive)
					counter++;
			}

			return counter;
		}

		public int countCombinations() {
			var visited = new HashSet<Cell>();
			int combos = 0;
			foreach (Cell cell in cells) {
				if (cell.isAlive && !visited.Contains(cell)) {
					combos++;
					var stack = new Stack<Cell>();
					stack.Push(cell);
					while (stack.Count > 0) {
						var c = stack.Pop();
						if (!visited.Add(c))
							continue;
						foreach (Cell n in c.neighbors) {
							if (n.isAlive && !visited.Contains(n))
								stack.Push(n);
						}
					}
				}
			}

			return combos;
		}

		public Dictionary<string, int> countPatterns() {
			var counts = patterns.Keys.ToDictionary(k => k, k => 0);
			foreach (var kv in patterns) {
				var name = kv.Key;
				var shape = kv.Value;
				for (int x = 0; x < columns; x++) {
					for (int y = 0; y < rows; y++) {
						bool ok = true;
						foreach (var d in shape) {
							int xx = (x + d.dx + columns) % columns;
							int yy = (y + d.dy + rows) % rows;
							if (!cells[xx, yy].isAlive) {
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
	}

	class Program {
		static Board board;
		static Settings settings;

		static void loadSettings() {
			var json = File.ReadAllText("../../../settings.json");
			settings = JsonSerializer.Deserialize<Settings>(json);
		}

		static void reset() {
			board = new Board(
				width: settings.width,
				height: settings.height,
				cellSize: 1,
				liveDensity: settings.liveDensity
			);
		}

		static void render() {
			for (int row = 0; row < board.rows; row++) {
				for (int col = 0; col < board.columns; col++) {
					Console.Write(board.cells[col, row].isAlive ? '*' : '.');
				}

				Console.Write('\n');
			}
		}

		static void Main(string[] args) {
			loadSettings();

			if (args.Length > 0 && File.Exists($"../../../{args[0]}"))
				board = Board.loadState($"../../../{args[0]}");
			else
				reset();

			while (true) {
				Console.Clear();
				render();

				if (Console.KeyAvailable) {
					var key = Console.ReadKey(true).Key;
					if (key == ConsoleKey.P) {
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
						while (true) {
							var choice = Console.ReadKey(true).Key;
							if (choice == ConsoleKey.Y)
								break;
							if (choice == ConsoleKey.N)
								return;
						}
					}
				}

				board.advance();
				Thread.Sleep(1000);
			}
		}
	}
}
