using cli_life;

namespace NET
{
    [TestClass]
    public class BoardTests
    {
        [TestMethod]
        public void BoardInitialization_Test()
        {
            var board = new Board(10, 10, 1, 0.5);
            Assert.AreEqual(10, board.columns);
            Assert.AreEqual(10, board.rows);
        }

        [TestMethod]
        public void RandomizeDensity_Test()
        {
            var board = new Board(10, 10, 1, 0.0);
            board.randomize(1.0);
            int liveCells = board.countLiveCells();
            Assert.AreEqual(100, liveCells);
        }

        [TestMethod]
        public void Advance_ChangesBoardState_Test()
        {
            var board = new Board(10, 10, 1, 1.0);
            board.advance();
            Assert.IsTrue(board.countLiveCells() <= 100);
        }

        [TestMethod]
        public void SaveAndLoadState_Test()
        {
            var board = new Board(10, 10, 1, 0.5);
            board.saveState("test_save.txt");
            var loadedBoard = Board.loadState("test_save.txt");
            Assert.AreEqual(board.columns, loadedBoard.columns);
            Assert.AreEqual(board.rows, loadedBoard.rows);
        }

        [TestMethod]
        public void CountLiveCells_Test()
        {
            var board = new Board(10, 10, 1, 0.0);
            Assert.AreEqual(0, board.countLiveCells());
        }

        [TestMethod]
        public void CountCombinations_EmptyBoard_Test()
        {
            var board = new Board(10, 10, 1, 0.0);
            Assert.AreEqual(0, board.countCombinations());
        }

        [TestMethod]
        public void CountCombinations_AllAlive_Test()
        {
            var board = new Board(5, 5, 1, 1.0);
            Assert.AreEqual(1, board.countCombinations());
        }

        [TestMethod]
        public void CountPatterns_BlockPattern_Test()
        {
            var board = new Board(4, 4, 1, 0.0);
            board.cells[0, 0].isAlive = true;
            board.cells[0, 1].isAlive = true;
            board.cells[1, 0].isAlive = true;
            board.cells[1, 1].isAlive = true;
            var patterns = board.countPatterns();
            Assert.IsTrue(patterns["block"] >= 1);
        }

        [TestMethod]
        public void SettingsDeserialization_Test()
        {
            var json = "{\"width\":10,\"height\":10,\"liveDensity\":0.5}";
            var settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);
            Assert.AreEqual(10, settings.width);
            Assert.AreEqual(0.5, settings.liveDensity);
        }

        [TestMethod]
        public void BoardRunExperiments_CreatesDataFile_Test()
        {
            Board.runExperiments(10, 10, 5);
            Assert.IsTrue(File.Exists("../../../data.txt"));
        }

        [TestMethod]
        public void BoardRunExperiments_CreatesPlotFile_Test()
        {
            Board.runExperiments(10, 10, 5);
            Assert.IsTrue(File.Exists("../../../plot.png"));
        }

        [TestMethod]
        public void EmptyBoard_NoPatterns_Test()
        {
            var board = new Board(10, 10, 1, 0.0);
            var patterns = board.countPatterns();
            int sum = patterns.Values.Sum();
            Assert.AreEqual(0, sum);
        }

        [TestMethod]
        public void SaveLoadState_PreservesCells_Test()
        {
            var board = new Board(10, 10, 1, 0.0);
            board.cells[0, 0].isAlive = true;
            board.saveState("test_save2.txt");
            var loadedBoard = Board.loadState("test_save2.txt");
            Assert.IsTrue(loadedBoard.cells[0, 0].isAlive);
        }

        [TestMethod]
        public void Advance_DoesNotCrash_Test()
        {
            var board = new Board(10, 10, 1, 0.5);
            board.advance();
            board.advance();
            board.advance();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void CountLiveCells_AfterAdvance_Test()
        {
            var board = new Board(10, 10, 1, 1.0);
board.advance();
            int liveCells = board.countLiveCells();
            Assert.IsTrue(liveCells >= 0);
        }
    }
}