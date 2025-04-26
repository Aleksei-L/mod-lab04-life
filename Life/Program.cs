using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Text;
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

		public Board(int width, int height, int cellSize, double liveDensity = .1) {
			this.cellSize = cellSize;
			cells = new Cell[width / cellSize, height / cellSize];
			for (int x = 0; x < columns; x++)
			for (int y = 0; y < rows; y++)
				cells[x, y] = new Cell();

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
