using System;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Entities;

public class Transaction : BaseEntity
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? Description { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? WalletId { get; set; }
    public Wallet? Wallet { get; set; }
}
