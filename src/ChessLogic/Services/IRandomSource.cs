namespace ChessLogic.Services;

/// <summary>
/// Source of Random Numbers. Exists so that tests can replace the randomness.
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Generates a random Number from 0 up to, but not includung a maximum number
    /// </summary>
    /// <param name="maxExclusive">Up to this, but not including number</param>
    /// <returns>Returns a number from 0 up to, but not including maxExclusive</returns>
    int Next(int maxExclusive);
}
