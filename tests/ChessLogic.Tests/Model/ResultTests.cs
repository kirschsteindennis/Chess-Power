using ChessLogic.Enums;
using ChessLogic.Model.Game;

namespace ChessLogic.Tests.Model;

[TestFixture]
public class ResultTests
{
    [Test]
    public void Win_IsACheckmateForTheWinner()
    {
        // Arrange
        Player winner = Player.Black;

        // Act
        Result result = Result.Win(winner);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Winner, Is.EqualTo(Player.Black));
            Assert.That(result.Reason, Is.EqualTo(EndReason.Checkmate));
        }
    }

    [TestCase(EndReason.Stalemate)]
    [TestCase(EndReason.FiftyMoveRule)]
    [TestCase(EndReason.InsufficientMaterial)]
    [TestCase(EndReason.ThreefoldRepetition)]
    public void Draw_HasNoWinner(EndReason reason)
    {
        // Arrange: the reason comes from the TestCase attributes

        // Act
        Result result = Result.Draw(reason);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Winner, Is.EqualTo(Player.None));
            Assert.That(result.Reason, Is.EqualTo(reason));
        }
    }

    [Test]
    public void Resignation_NamesTheWinnerAndTheReason()
    {
        // Arrange
        Player winner = Player.White;

        // Act
        Result result = Result.Resignation(winner);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Winner, Is.EqualTo(Player.White));
            Assert.That(result.Reason, Is.EqualTo(EndReason.Resignation));
        }
    }
}
