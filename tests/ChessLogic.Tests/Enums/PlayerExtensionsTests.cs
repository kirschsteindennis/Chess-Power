using ChessLogic.Enums;

namespace ChessLogic.Tests.Enums;

[TestFixture]
public class PlayerExtensionsTests
{
    [TestCase(Player.White, Player.Black)]
    [TestCase(Player.Black, Player.White)]
    [TestCase(Player.None, Player.None)]
    public void Opponent_ReturnsTheOtherPlayer(Player player, Player expected)
    {
        // Arrange: Nothing to do, input comes from test cases
        // Act
        Player opponent = player.Opponent();

        // Assert
        Assert.That(opponent, Is.EqualTo(expected));
    }
}
