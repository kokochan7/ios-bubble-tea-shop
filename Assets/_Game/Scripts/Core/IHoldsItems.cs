namespace BubbleTea.Core
{
    /// <summary>
    /// Something a character carries in their hands (a tray of drinks, a crate, drinks they bought).
    /// <see cref="WalkBob"/> reaches the character's arms forward while it's true.
    /// </summary>
    public interface IHoldsItems
    {
        bool IsHolding { get; }
    }
}
