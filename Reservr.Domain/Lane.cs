namespace Reservr.Domain;

/// <summary>A bowling lane in the venue.</summary>
public sealed class Lane
{
    public Lane(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);

        Id = Guid.CreateVersion7();
        Number = number;
    }

    public Guid Id { get; private set; }

    /// <summary>The lane number shown to customers. Always positive.</summary>
    public int Number { get; private set; }
}
