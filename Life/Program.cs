using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace cli_life {
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
	}

	class Program {
		static Board board;

		static private void reset() {
			board = new Board(
				width: 50,
				height: 20,
				cellSize: 1,
				liveDensity: 0.5);
		}

		static void render() {
			for (int row = 0; row < board.rows; row++) {
				for (int col = 0; col < board.columns; col++) {
					var cell = board.cells[col, row];
					if (cell.isAlive) {
						Console.Write('*');
					} else {
						Console.Write(' ');
					}
				}

				Console.Write('\n');
			}
		}

		static void Main(string[] args) {
			reset();
			while (true) {
				Console.Clear();
				render();
				board.advance();
				Thread.Sleep(1000);
			}
		}
	}
}
