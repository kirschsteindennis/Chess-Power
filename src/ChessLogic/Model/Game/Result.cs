using ChessLogic.Enums;

namespace ChessLogic.Model.Game;

public class Result(Player winner, EndReason reason)
{
    public Player Winner { get; } = winner;
    public EndReason Reason { get; } = reason;

    public static Result Win(Player winner)
    {
        return new Result(winner, EndReason.Checkmate);
    }

    public static Result Draw(EndReason reason)
    {
        return new Result(Player.None, reason);
    }

    public static Result Resignation(Player winner)
    {
        return new Result(winner, EndReason.Resignation);
    }
}
