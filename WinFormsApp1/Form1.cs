namespace WinFormsApp1;

public partial class Form1 : Form
{
    private const int Rows = 20;
    private const int Columns = 10;
    private const int CellSize = 30;
    private bool _isPaused = false;
    private bool _isGameOver = false;


    private readonly System.Windows.Forms.Timer _timer;
    private int[,] _gameField;
    private Point[] _currentPiece;
    private Point _piecePosition;
    private static readonly Random Random = new();
    private int _score;

    public Form1()
    {
        InitializeComponent();
        this.DoubleBuffered = true;
        this.Width = 600;
        this.Height = 680;
        this.Text = "Тетрис";
        this.StartPosition = FormStartPosition.CenterScreen;
        _gameField = new int[Rows, Columns];
        GenerateNewPiece();

        _timer = new System.Windows.Forms.Timer { Interval = 500 };
        _timer.Tick += UpdateGame;
        _timer.Start();

        KeyDown += OnKeyDown;
        Paint += OnPaint;

        LoadHighScores();
    }

    private int _currentPieceId;

    private List<HighScore> _highScores = [];

    private void LoadHighScores()
    {
        if (File.Exists("highscores.txt"))
        {
            var lines = File.ReadAllLines("highscores.txt");
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length == 2)
                {
                    string playerName = parts[0];
                    int score = int.Parse(parts[1]);
                    _highScores.Add(new HighScore(playerName, score));
                }
            }
        }

        _highScores = _highScores.OrderByDescending(h => h.Score).Take(5).ToList();
    }

    private string _playerName;

    private void CheckAndSaveNewHighScore()
    {
        if (_highScores.Count < 5 || _score > _highScores.Last().Score)
        {
            string playerName = Prompt.ShowDialog("Введите имя:", "Новый рекорд");
            _playerName = playerName;

            var existingScore = _highScores.FirstOrDefault(h => h.PlayerName == _playerName);

            if (existingScore != null)
            {
                existingScore.Score = Math.Max(existingScore.Score, _score);
            }
            else
            {
                _highScores.Add(new HighScore(_playerName, _score));
            }

            _highScores = _highScores.OrderByDescending(h => h.Score).Take(5).ToList();

            SaveHighScores();
        }
    }


    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Width = 200, Text = text };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 200 };
            Button confirmation = new Button()
                { Text = "ОК", Left = 100, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }

    private void DisplayHighScores(Graphics graphics)
    {
        var font = new Font("Arial", 14, FontStyle.Bold);
        var font1 = new Font("Arial", 13, FontStyle.Regular);

        var yPosition = 0;
        graphics.DrawString("Таблица рекордов:", font, Brushes.Black, 320, yPosition);

        for (int i = 0; i < _highScores.Count; i++)
        {
            var highScore = _highScores[i];
            yPosition += 30;
            graphics.DrawString($"{i + 1}. {highScore.PlayerName} - {highScore.Score}", font1, Brushes.Black, 320,
                yPosition);
        }
    }


    private void SaveHighScores()
    {
        var lines = _highScores.Select(h => $"{h.PlayerName},{h.Score}").ToArray();
        File.WriteAllLines("highscores.txt", lines);
    }


    public class HighScore
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }

        public HighScore(string playerName, int score)
        {
            PlayerName = playerName;
            Score = score;
        }
    }


    private void GenerateNewPiece()
    {
        var pieces = new[]
        {
            new[] { new Point(0, 0), new Point(0, 1), new Point(1, 0), new Point(1, 1) }, // квадрат
            new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(3, 1) }, // полоса
            new[] { new Point(0, 1), new Point(1, 0), new Point(1, 1), new Point(1, 2) }, // т
            new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(1, 2) }, // тудасюда
            new[] { new Point(0, 1), new Point(0, 2), new Point(1, 0), new Point(1, 1) } // тудасюда другая
        };

        var random = Random.Next(pieces.Length);
        _currentPiece = pieces[random];
        _currentPieceId = random;

        _piecePosition = new Point(0, Columns / 2 - 1);

        var newPosition = _currentPiece.Select(p => new Point(p.X + _piecePosition.X, p.Y + _piecePosition.Y))
            .ToArray();
        if (!IsValidPosition(newPosition))
        {
            _isGameOver = true;
            _timer.Stop();
            this.Invalidate();
            CheckAndSaveNewHighScore();
        }
    }


    private void UpdateGame(object sender, EventArgs e)
    {
        MovePiece(1, 0);
    }

    private void MovePiece(int rowOffset, int colOffset)
    {
        if (_isPaused || _isGameOver) return;

        var newPosition = _currentPiece
            .Select(p => new Point(p.X + _piecePosition.X + rowOffset, p.Y + _piecePosition.Y + colOffset))
            .ToArray();

        if (IsValidPosition(newPosition))
        {
            _piecePosition.Offset(rowOffset, colOffset);
        }
        else if (rowOffset == 1 && colOffset == 0)
        {
            LockPiece();
            GenerateNewPiece();
        }

        Invalidate();
    }


    private void LockPiece()
    {
        foreach (var point in _currentPiece)
        {
            var x = point.X + _piecePosition.X;
            var y = point.Y + _piecePosition.Y;
            _gameField[x, y] = 1;
        }

        ClearFullRows();
    }


    private void ClearFullRows()
    {
        for (int row = Rows - 1; row >= 0; row--)
        {
            if (Enumerable.Range(0, Columns).All(col => _gameField[row, col] == 1))
            {
                for (int r = row; r > 0; r--)
                {
                    for (int col = 0; col < Columns; col++)
                    {
                        _gameField[r, col] = _gameField[r - 1, col];
                    }
                }

                for (int col = 0; col < Columns; col++)
                {
                    _gameField[0, col] = 0;
                }

                _score += 100;
                row++;
            }
        }
    }

    private bool IsValidPosition(Point[] piece)
    {
        return piece.All(p => p.X >= 0 && p.X < Rows && p.Y >= 0 && p.Y < Columns && _gameField[p.X, p.Y] == 0);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_isGameOver)
        {
            if (e.KeyCode == Keys.Enter)
            {
                RestartGame();
            }

            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Left:
                MovePiece(0, -1);
                break;
            case Keys.Right:
                MovePiece(0, 1);
                break;
            case Keys.Down:
                MovePiece(1, 0);
                break;
            case Keys.Up:
                RotatePiece();
                break;
            case Keys.Space:
                DropPiece();
                break;
            case Keys.Escape:
                TogglePause();
                break;
        }
    }

    private void RestartGame()
    {
        _gameField = new int[Rows, Columns];
        _score = 0;
        _isGameOver = false;
        _timer.Start();
        GenerateNewPiece();

        this.Invalidate();
    }


    private void TogglePause()
    {
        if (_isPaused)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }

        _isPaused = !_isPaused;
        Invalidate();
    }


    private void DropPiece()
    {
        if (_isPaused || _isGameOver) return;

        while (IsValidPosition(_currentPiece.Select(p => new Point(p.X + _piecePosition.X + 1, p.Y + _piecePosition.Y))
                   .ToArray()))
        {
            _piecePosition.Offset(1, 0);
        }

        LockPiece();
        GenerateNewPiece();
        Invalidate();
    }


    private void RotatePiece()
    {
        if (_isPaused || _isGameOver) return;

        var centerOffsets = new Dictionary<int, Point>
        {
            { 0, new Point(0, 0) },
            { 1, new Point(1, 1) },
            { 2, new Point(1, 1) },
            { 3, new Point(1, 1) },
            { 4, new Point(1, 1) }
        };

        if (_currentPieceId == 0)
        {
            return;
        }

        if (!centerOffsets.TryGetValue(_currentPieceId, out var center))
        {
            return;
        }

        var rotatedPiece = _currentPiece.Select(p =>
        {
            int relativeX = p.X - center.X;
            int relativeY = p.Y - center.Y;

            return new Point(center.X - relativeY, center.Y + relativeX);
        }).ToArray();

        var newPosition = rotatedPiece.Select(p => new Point(p.X + _piecePosition.X, p.Y + _piecePosition.Y)).ToArray();
        if (IsValidPosition(newPosition))
        {
            _currentPiece = rotatedPiece;
        }

        Invalidate();
    }


    private void OnPaint(object sender, PaintEventArgs e)
    {
        var graphics = e.Graphics;

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                if (_gameField[row, col] == 1)
                {
                    graphics.FillRectangle(Brushes.Blue, col * CellSize, row * CellSize, CellSize, CellSize);
                    graphics.DrawRectangle(Pens.Black, col * CellSize, row * CellSize, CellSize, CellSize);
                }
            }
        }

        foreach (var point in _currentPiece)
        {
            var x = (point.Y + _piecePosition.Y) * CellSize;
            var y = (point.X + _piecePosition.X) * CellSize;
            graphics.FillRectangle(Brushes.Red, x, y, CellSize, CellSize);
            graphics.DrawRectangle(Pens.Black, x, y, CellSize, CellSize);
        }

        for (int row = 0; row <= Rows; row++)
        {
            graphics.DrawLine(Pens.Black, 0, row * CellSize, Columns * CellSize, row * CellSize);
        }

        for (int col = 0; col <= Columns; col++)
        {
            graphics.DrawLine(Pens.Black, col * CellSize, 0, col * CellSize, Rows * CellSize);
        }


        graphics.DrawString($"Счет: {_score}", new Font("Arial", 16), Brushes.Black, 10, Rows * CellSize + 10);

        if (_isPaused)
        {
            var pauseMessage = "Пауза";
            var font = new Font("Arial", 24, FontStyle.Bold);
            var x = 100;
            var y = 280;
            graphics.DrawString(pauseMessage, font, Brushes.Red, x, y);
        }

        if (_isGameOver)
        {
            var gameOverMessage = $"Игра закончена\nСчет: {_score}\nНажмите Enter";
            var font = new Font("Arial", 24, FontStyle.Bold);
            graphics.DrawString(gameOverMessage, font, Brushes.Red, 320, 246);
        }

        DisplayHighScores(graphics);
        
        graphics.DrawString($"Чесноков Кирилл Игоревич\nО738Б", new Font("Arial", 12), Brushes.Black, 320, Rows * CellSize - 4);

    }
}