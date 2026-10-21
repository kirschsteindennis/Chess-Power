using ChessLogic.Enums;
using ChessLogic.Model;
using ChessLogic.Model.Game;
using ChessLogic.Moves;
using Chess.Test.Shared.Support;
using ChessLogic.ViewModel;

namespace ChessLogic.Tests.ViewModel;

[TestFixture]
public class GameStateTests
{
    #region Start

    [Test]
    public void NewGame_StartsWithTheGivenPlayerAndNoResult()
    {
        // Arrange
        Board board = Board.Initial();

        // Act
        GameState game = new(Player.White, board);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.CurrentPlayer, Is.EqualTo(Player.White));
            Assert.That(game.Result, Is.Null);
            Assert.That(game.IsGameOver(), Is.False);
            Assert.That(game.Board, Is.SameAs(board));
        }
    }

    #endregion

    #region Legal Moves

    [Test]
    public void AllLegalMovesFor_InitialPosition_Has20Moves()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act
        int count = game.AllLegalMovesFor(Player.White).Count();

        // Assert
        Assert.That(count, Is.EqualTo(20));
    }

    [Test]
    public void LegalMovesForPiece_OnAnEmptySquare_ReturnsNothing()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act
        IEnumerable<Move> moves = game.LegalMovesForPiece(Sq.At("e4"));

        // Assert
        Assert.That(moves, Is.Empty);
    }

    [Test]
    public void LegalMovesForPiece_OfTheOpponent_ReturnsNothing()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act
        IEnumerable<Move> moves = game.LegalMovesForPiece(Sq.At("e7"));

        // Assert
        Assert.That(moves, Is.Empty);
    }

    [Test]
    public void LegalMovesForPiece_AStartingPawn_HasTwoMoves()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act
        List<Move> moves = [.. game.LegalMovesForPiece(Sq.At("e2"))];

        // Assert
        Assert.That(moves.Select(m => Sq.Name(m.ToPos)), Is.EquivalentTo(["e3", "e4"]));
    }

    [Test]
    public void LegalMovesForPiece_APinnedPiece_CannotLeaveTheLine()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .White(PieceType.Rook, "e2")
            .Black(PieceType.Rook, "e8")
            .Black(PieceType.King, "a8")
            .Build();
        GameState game = new(Player.White, board);

        // Act
        List<string> destinations = [.. game.LegalMovesForPiece(Sq.At("e2")).Select(m => Sq.Name(m.ToPos))];

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["e3", "e4", "e5", "e6", "e7", "e8"]));
    }

    [Test]
    public void LegalMovesForPiece_TheKing_CannotStepIntoCheck()
    {
        // Arrange: the black rook controls the whole d-file
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .Black(PieceType.Rook, "d8")
            .Black(PieceType.King, "h8")
            .Build();
        GameState game = new(Player.White, board);

        // Act
        List<string> destinations = [.. game.LegalMovesForPiece(Sq.At("e1")).Select(m => Sq.Name(m.ToPos))];

        // Assert
        Assert.That(destinations, Is.EquivalentTo(["e2", "f1", "f2"]));
    }

    #endregion

    #region Making Moves

    [Test]
    public void MakeMove_UpdatesTheBoardAndSwitchesThePlayer()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act
        MoveHelper.Play(game, "e2", "e4");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Board[Sq.At("e4")].Type, Is.EqualTo(PieceType.Pawn));
            Assert.That(game.Board[Sq.At("e2")], Is.Null);
            Assert.That(game.CurrentPlayer, Is.EqualTo(Player.Black));
        }
    }

    [Test]
    public void MakeMove_EnPassant_CapturesThePawnThatJumpedTwoSquares()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());
        MoveHelper.PlayAll(game, "e2e4", "a7a6", "e4e5", "d7d5");

        // Act
        MoveHelper.Play(game, "e5", "d6");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Board[Sq.At("d6")].Color, Is.EqualTo(Player.White));
            Assert.That(game.Board[Sq.At("d5")], Is.Null);
        }
    }

    [Test]
    public void MakeMove_EnPassant_IsOnlyPossibleDirectlyAfterTheDoubleStep()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());
        MoveHelper.PlayAll(game, "e2e4", "a7a6", "e4e5", "d7d5", "h2h3", "a6a5");

        // Act
        List<Move> moves = [.. game.LegalMovesForPiece(Sq.At("e5"))];

        // Assert
        Assert.That(moves.Any(m => m.Type == MoveType.EnPassant), Is.False);
    }

    [Test]
    public void MakeMove_Castling_MovesKingAndRook()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());
        MoveHelper.PlayAll(game, "e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "f8c5");

        // Act
        MoveHelper.Play(game, "e1", "g1");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Board[Sq.At("g1")].Type, Is.EqualTo(PieceType.King));
            Assert.That(game.Board[Sq.At("f1")].Type, Is.EqualTo(PieceType.Rook));
        }
    }

    [Test]
    public void MakeMove_Promotion_ReplacesThePawn()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "e1")
            .Black(PieceType.King, "e8")
            .White(PieceType.Pawn, "a7")
            .Build();
        GameState game = new(Player.White, board);

        // Act
        game.MakeMove(new PawnPromotion(Sq.At("a7"), Sq.At("a8"), PieceType.Knight));

        // Assert
        Assert.That(game.Board[Sq.At("a8")].Type, Is.EqualTo(PieceType.Knight));
    }

    #endregion

    #region Game Over

    [Test]
    public void MakeMove_FoolsMate_EndsWithCheckmateForBlack()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act
        MoveHelper.PlayAll(game, "f2f3", "e7e5", "g2g4", "d8h4");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.IsGameOver(), Is.True);
            Assert.That(game.Result.Winner, Is.EqualTo(Player.Black));
            Assert.That(game.Result.Reason, Is.EqualTo(EndReason.Checkmate));
        }
    }

    [Test]
    public void MakeMove_ScholarsMate_EndsWithCheckmateForWhite()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act
        MoveHelper.PlayAll(game, "e2e4", "e7e5", "d1h5", "b8c6", "f1c4", "g8f6", "h5f7");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Result.Winner, Is.EqualTo(Player.White));
            Assert.That(game.Result.Reason, Is.EqualTo(EndReason.Checkmate));
        }
    }

    [Test]
    public void MakeMove_ANormalMove_DoesNotEndTheGame()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act
        MoveHelper.Play(game, "e2", "e4");

        // Assert
        Assert.That(game.IsGameOver(), Is.False);
    }

    public void MakeMove_WhenTheOpponentCannotMoveAndIsNotInCheck_IsAStalemate()
    {
        // Arrange: after Qb6 the black king on a8 has no square and is not attacked
        Board board = new BoardBuilder()
            .Black(PieceType.King, "a8")
            .White(PieceType.King, "c7")
            .White(PieceType.Queen, "d6")
            .Build();
        GameState game = new(Player.White, board);

        // Act
        MoveHelper.Play(game, "d6", "b6");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Result.Reason, Is.EqualTo(EndReason.Stalemate));
            Assert.That(game.Result.Winner, Is.EqualTo(Player.None));
        }
    }

    [Test]
    public void MakeMove_CapturingTheLastPiece_IsADrawByInsufficientMaterial()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "a1")
            .Black(PieceType.Knight, "b2")
            .Black(PieceType.King, "h8")
            .Build();
        GameState game = new(Player.White, board);

        // Act
        MoveHelper.Play(game, "a1", "b2");

        // Assert
        Assert.That(game.Result.Reason, Is.EqualTo(EndReason.InsufficientMaterial));
    }

    [Test]
    public void MakeMove_The100thQuietMove_IsADrawByTheFiftyMoveRule()
    {
        // Arrange: 98 half-moves without capture or pawn move are already played
        Board board = new BoardBuilder()
            .White(PieceType.King, "a1")
            .White(PieceType.Rook, "b1")
            .Black(PieceType.King, "h8")
            .Black(PieceType.Rook, "g8")
            .Build();
        GameState game = new(Player.White, board, halfMoveClock: 98);

        // Act
        MoveHelper.Play(game, "b1", "b2");
        bool overAfterFirstMove = game.IsGameOver();
        MoveHelper.Play(game, "g8", "g7");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(overAfterFirstMove, Is.False);
            Assert.That(game.Result.Reason, Is.EqualTo(EndReason.FiftyMoveRule));
        }
    }

    [Test]
    public void MakeMove_APawnMove_ResetsTheFiftyMoveCounter()
    {
        // Arrange
        Board board = new BoardBuilder()
            .White(PieceType.King, "a1")
            .White(PieceType.Pawn, "e2")
            .Black(PieceType.King, "h8")
            .Black(PieceType.Rook, "g8")
            .Build();
        GameState game = new(Player.White, board, halfMoveClock: 98);

        // Act
        MoveHelper.Play(game, "e2", "e4");
        MoveHelper.Play(game, "g8", "g7");

        // Assert
        Assert.That(game.IsGameOver(), Is.False);
    }

    [Test]
    public void MakeMove_TheSamePositionForTheThirdTime_IsADrawByRepetition()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act: both sides move a knight out and back, twice
        MoveHelper.PlayAll(game, "g1f3", "g8f6", "f3g1", "f6g8");
        bool overAfterOneRound = game.IsGameOver();
        MoveHelper.PlayAll(game, "g1f3", "g8f6", "f3g1", "f6g8");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(overAfterOneRound, Is.False);
            Assert.That(game.Result.Reason, Is.EqualTo(EndReason.ThreefoldRepetition));
        }
    }

    #endregion

    #region Resign

    [TestCase(Player.White, Player.Black)]
    [TestCase(Player.Black, Player.White)]
    public void Resign_GivesTheWinToTheOpponent(Player resigning, Player expectedWinner)
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());

        // Act
        game.Resign(resigning);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.IsGameOver(), Is.True);
            Assert.That(game.Result.Winner, Is.EqualTo(expectedWinner));
            Assert.That(game.Result.Reason, Is.EqualTo(EndReason.Resignation));
        }
    }

    [Test]
    public void Resign_ByThePlayerWhoIsNotOnTurn_IsPossible()
    {
        // Arrange: it is white's turn
        GameState game = new(Player.White, Board.Initial());

        // Act
        game.Resign(Player.Black);

        // Assert
        Assert.That(game.Result.Winner, Is.EqualTo(Player.White));
    }

    [Test]
    public void Resign_AfterTheGameIsOver_DoesNotChangeTheResult()
    {
        // Arrange
        GameState game = new(Player.White, Board.Initial());
        MoveHelper.PlayAll(game, "f2f3", "e7e5", "g2g4", "d8h4");
        Result before = game.Result;

        // Act
        game.Resign(Player.Black);

        // Assert
        Assert.That(game.Result, Is.SameAs(before));
    }

    #endregion
}
