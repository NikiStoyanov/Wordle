namespace Wordle.Server.Core.Entities;

public class MultiplayerMatch
{
    public int Id { get; set; }

    public string RoomCode { get; set; } = string.Empty;

    public int HostId { get; set; }

    public User Host { get; set; } = null!;

    public int? GuestId { get; set; }

    public User? Guest { get; set; }

    public int WordId { get; set; }

    public Word Word { get; set; } = null!;

    public int? WinnerId { get; set; }

    public string Status { get; set; } = "Waiting";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
