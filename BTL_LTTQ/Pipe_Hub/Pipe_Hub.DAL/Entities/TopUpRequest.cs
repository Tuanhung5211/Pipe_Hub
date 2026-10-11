using System;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Entities;

public class TopUpRequest : BaseEntity
{
    public decimal Amount { get; set; }
    public TopUpStatus Status { get; set; } = TopUpStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
