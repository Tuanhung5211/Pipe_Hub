using System;
using System.Collections.Generic;

namespace Pipe_Hub.DAL.Entities;

public class Wallet : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal Balance { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
