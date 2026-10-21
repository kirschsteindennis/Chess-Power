using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Moves;
using ChessLogic.Pieces;
using ChessLogic.ViewModel;
using ChessUI.Enum;
using ChessUI.Models;
using ChessUI.UiElements;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChessUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Image[,] pieceImages = new Image[8, 8];
    private readonly Rectangle[,] highlights = new Rectangle[8, 8];
    private readonly Dictionary<Position, Move> moveCache = [];

    private GameState gameState;
    private Position selectedPos = null;

    public MainWindow()
    {
        InitializeComponent();
        InitializeBoard();
    }

    public void ShowMainMenu()
    {
        MainMenu mainMenu = new();
        MenuContainer.Content = mainMenu;

        mainMenu.OptionSelected += mode =>
        {
            MenuContainer.Content = null;
            switch (mode)
            {
                case GameMode.Local:
                    StartLocalGame();
                    break;
                case GameMode.VsComputer:
                    ShowDifficultyMenu();
                    break;
                case GameMode.Online:
                    break;
                default:
                    return;
            }
        };
    }

    private void StartLocalGame()
    {
        selectedPos = null;
        HideHighlights();
        moveCache.Clear();
        gameState = new(Player.White, Board.Initial());
        DrawBoard(gameState.Board);
        SetCursor(gameState.CurrentPlayer);
    }

    private void StartAIGame(AIDifficulty difficulty)
    {
        selectedPos = null;
        HideHighlights();
        moveCache.Clear();

        gameState = new AIGameState(Player.White, Board.Initial(), difficulty);
        DrawBoard(gameState.Board);
        SetCursor(gameState.CurrentPlayer);
    }

    private void ShowDifficultyMenu()
    {
        DifficultyMenu difficultyMenu = new();
        MenuContainer.Content = difficultyMenu;

        difficultyMenu.DifficultySelected += difficulty =>
        {
            MenuContainer.Content = null;
            StartAIGame(difficulty);
        };

        difficultyMenu.BackToMainMenu += () => ShowMainMenu();
    }

    private void InitializeBoard()
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Image image = new();
                pieceImages[r, c] = image;
                PieceGrid.Children.Add(image);

                Rectangle highlight = new();
                highlights[r, c] = highlight;
                HighlightGrid.Children.Add(highlight);
            }
        }
    }

    private void DrawBoard(Board board)
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Piece piece = board[r, c];
                pieceImages[r, c].Source = Images.GetImage(piece);
            }
        }
    }

    private void BoardGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (IsMenuOnScreen())
            return;

        Point point = e.GetPosition(BoardGrid);
        Position pos = ToSquarePosition(point);

        if (selectedPos == null)
            OnFromPositionsSelected(pos);
        else
            OnToPositionSelected(pos);
    }

    private Position ToSquarePosition(Point point)
    {
        double squareSize = BoardGrid.ActualWidth / 8;
        int row = (int)(point.Y / squareSize);
        int col = (int)(point.X / squareSize);
        return new Position(row, col);
    }

    private void OnFromPositionsSelected(Position pos)
    {
        IEnumerable<Move> moves = gameState.LegalMovesForPiece(pos);

        if (moves.Any())
        {
            selectedPos = pos;
            CacheMoves(moves);
            ShowHighlights();
        }
    }

    private void OnToPositionSelected(Position pos)
    {
        selectedPos = null;
        HideHighlights();

        if (moveCache.TryGetValue(pos, out Move move))
        {
            if (move.Type == MoveType.PawnPromotion)
                HandlePromotion(move.FromPos, move.ToPos);
            else
                HandleMove(move);

            if (gameState is AIGameState aiGameState &&
                !aiGameState.IsGameOver() &&
                aiGameState.CurrentPlayer == Player.Black)
            {
                aiGameState.MakeAIMove();
                DrawBoard(aiGameState.Board);
                SetCursor(aiGameState.CurrentPlayer);

                if (aiGameState.IsGameOver())
                    ShowGameOver();
            }
        }
    }

    private void HandlePromotion(Position from, Position to)
    {
        pieceImages[to.Row, to.Column].Source = Images.GetImage(gameState.CurrentPlayer, PieceType.Pawn);
        pieceImages[from.Row, from.Column].Source = null;

        PromotionMenu promMenu = new(gameState.CurrentPlayer);
        MenuContainer.Content = promMenu;

        promMenu.PieceSelected += type =>
        {
            MenuContainer.Content = null;
            Move promMove = new PawnPromotion(from, to, type);
            HandleMove(promMove);
        };
    }

    private void HandleMove(Move move)
    {
        gameState.MakeMove(move);
        DrawBoard(gameState.Board);
        SetCursor(gameState.CurrentPlayer);

        if (gameState.IsGameOver())
            ShowGameOver();
    }

    private void CacheMoves(IEnumerable<Move> moves)
    {
        moveCache.Clear();

        foreach (Move move in moves)
            moveCache[move.ToPos] = move;
    }

    private void ShowHighlights()
    {
        Color color = Color.FromArgb(150, 125, 255, 125);

        foreach (Position to in moveCache.Keys)
            highlights[to.Row, to.Column].Fill = new SolidColorBrush(color);
    }

    private void HideHighlights()
    {
        foreach (Position to in moveCache.Keys)
            highlights[to.Row, to.Column].Fill = Brushes.Transparent;
    }

    private void SetCursor(Player player)
    {
        if (player == Player.White)
            Cursor = ChessCursors.WhiteCursor;
        else
            Cursor = ChessCursors.BlackCursor;
    }

    private bool IsMenuOnScreen()
    {
        return MenuContainer.Content != null;
    }

    private void ShowGameOver()
    {
        GameOverMenu gameOverMenu = new(gameState);
        MenuContainer.Content = gameOverMenu;

        gameOverMenu.OptionSelected += option =>
        {
            if (option == Option.Restart)
            {
                MenuContainer.Content = null;
                RestartGame();
            }
            else
                Application.Current.Shutdown();
        };
    }

    private void RestartGame()
    {
        ShowMainMenu();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (!IsMenuOnScreen() && e.Key == Key.Escape)
            ShowPauseMenu();
    }

    private void ShowPauseMenu()
    {
        PauseMenu pauseMenu = new();
        MenuContainer.Content = pauseMenu;

        pauseMenu.OptionSelected += option =>
        {
            MenuContainer.Content = null;

            if (option == Option.Restart)
                RestartGame();
        };
    }
}