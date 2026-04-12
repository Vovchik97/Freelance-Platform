using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Stripe.V2;

namespace FreelancePlatform.Services;

public class BalanceService : IBalanceService
{
     private readonly AppDbContext _context;

     public BalanceService(AppDbContext context)
     {
          _context = context;
     }

     public async Task<UserBalance> GetAsync(string userId)
     {
          var balance = await _context.UserBalances.FindAsync(userId);
          if (balance == null)
          {
               balance = new UserBalance
               {
                    UserId = userId,
                    Balance = 0,
                    Frozen = 0
               };
               _context.UserBalances.Add(balance);
               await _context.SaveChangesAsync();
          }

          return balance;
     }

     public async Task DepositAsync(string userId, decimal amount, int paymentId)
     {
          var balance = await GetAsync(userId);

          balance.Balance += amount;

          _context.BalanceTransactions.Add(new BalanceTransaction
          {
               UserId = userId,
               Amount = amount,
               PaymentId = paymentId,
               Type = BalanceTransactionType.Deposit
          });

          await _context.SaveChangesAsync();
     }
     
     public async Task FreezeForOrderAsync(string userId, decimal amount, int orderId)
     {
          var balance = await GetAsync(userId);

          if (balance.Balance < amount)
          {
               throw new InvalidOperationException("Недостаточно средств");
          }

          balance.Balance -= amount;
          balance.Frozen += amount;

          _context.BalanceTransactions.Add(new BalanceTransaction
          {
               UserId = userId,
               Amount = amount,
               OrderId = orderId,
               Type = BalanceTransactionType.Freeze
          });

          await _context.SaveChangesAsync();
     }
     
     public async Task FreezeForProjectAsync(string userId, decimal amount, int projectId)
     {
          var balance = await GetAsync(userId);

          if (balance.Balance < amount)
          {
               throw new InvalidOperationException("Недостаточно средств");
          }

          balance.Balance -= amount;
          balance.Frozen += amount;

          _context.BalanceTransactions.Add(new BalanceTransaction
          {
               UserId = userId,
               Amount = amount,
               ProjectId = projectId,
               Type = BalanceTransactionType.Freeze
          });

          await _context.SaveChangesAsync();
     }
     
     public async Task RefundForOrderAsync(string userId, decimal amount, int orderId)
     {
          var balance = await GetAsync(userId);

          if (balance.Frozen < amount)
          {
               throw new InvalidOperationException(
                    $"Невозможно вернуть {amount}: замороженных средств только {balance.Frozen}");
          }

          balance.Frozen -= amount;
          balance.Balance += amount;

          _context.BalanceTransactions.Add(new BalanceTransaction
          {
               UserId = userId,
               Amount = amount,
               OrderId = orderId,
               Type = BalanceTransactionType.Refund
          });

          await _context.SaveChangesAsync();
     }
     
     public async Task RefundForProjectAsync(string userId, decimal amount, int projectId)
     {
          var balance = await GetAsync(userId);
          
          if (balance.Frozen < amount)
          {
               throw new InvalidOperationException(
                    $"Невозможно вернуть {amount}: замороженных средств только {balance.Frozen}");
          }

          balance.Frozen -= amount;
          balance.Balance += amount;

          _context.BalanceTransactions.Add(new BalanceTransaction
          {
               UserId = userId,
               Amount = amount,
               ProjectId = projectId,
               Type = BalanceTransactionType.Refund
          });

          await _context.SaveChangesAsync();
     }

     public async Task RefundDepositAsync(string userId, decimal amount, int paymentId)
     {
          var balance = await GetAsync(userId);
          
          if (balance.Balance < amount)
          {
               throw new InvalidOperationException(
                    $"Невозможно вернуть {amount}: на балансе только {balance.Balance}");
          }

          balance.Balance -= amount;

          _context.BalanceTransactions.Add(new BalanceTransaction
          {
               UserId = userId,
               Amount = amount,
               PaymentId = paymentId,
               Type = BalanceTransactionType.Refund
          });

          await _context.SaveChangesAsync();
     }
     
     public async Task ReleaseForOrderAsync(string clientId, string freelancerId, decimal amount, int orderId)
     {
          var client = await GetAsync(clientId);
          var freelancer = await GetAsync(freelancerId);

          if (client.Frozen < amount)
          {
               throw new InvalidOperationException("Недостаточно замороженных средств");
          }

          const decimal commissonPercent = 0.1m;
          var commission = amount * commissonPercent;
          var payout = amount - commission;
          
          client.Frozen -= amount;
          freelancer.Balance += payout;
          
          _context.BalanceTransactions.AddRange(
               new BalanceTransaction
               {
                    UserId = freelancerId,
                    Amount = amount,
                    OrderId = orderId,
                    Type = BalanceTransactionType.Payout
               },
               new BalanceTransaction
               {
                    UserId = "PLATFORM",
                    Amount = commission,
                    OrderId = orderId,
                    Type = BalanceTransactionType.Commission
               }
          );
          
          await _context.SaveChangesAsync();
     }
     
     public async Task ReleaseForProjectAsync(string clientId, string freelancerId, decimal amount, int projectId)
     {
          var client = await GetAsync(clientId);
          var freelancer = await GetAsync(freelancerId);

          if (client.Frozen < amount)
          {
               throw new InvalidOperationException("Недостаточно замороженных средств");
          }

          const decimal commissonPercent = 0.1m;
          var commission = amount * commissonPercent;
          var payout = amount - commission;
          
          client.Frozen -= amount;
          freelancer.Balance += payout;
          
          _context.BalanceTransactions.AddRange(
               new BalanceTransaction
               {
                    UserId = freelancerId,
                    Amount = amount,
                    ProjectId = projectId,
                    Type = BalanceTransactionType.Payout
               },
               new BalanceTransaction
               {
                    UserId = "PLATFORM",
                    Amount = commission,
                    ProjectId = projectId,
                    Type = BalanceTransactionType.Commission
               }
          );
          
          await _context.SaveChangesAsync();
     }

     public async Task ReleaseForTeamProjectAsync(string clientId,
          List<(string UserId, string UserName, decimal Amount)> payouts, int projectId)
     {
          var client = await GetAsync(clientId);
          var totalAmount = payouts.Sum(p => p.Amount);

          if (client.Frozen < totalAmount)
          {
               throw new InvalidOperationException("Недостаточно замороженных средств.");
          }

          client.Frozen -= totalAmount;
          
          const decimal commissonPercent = 0.1m;

          foreach (var (freelancerId, userName, amount) in payouts)
          {
               var commission = Math.Round(amount * commissonPercent, 2);
               var payout = amount - commission;

               var freelancer = await GetAsync(freelancerId);
               freelancer.Balance += payout;
               
               _context.BalanceTransactions.AddRange(
                    new BalanceTransaction
                    {
                         UserId = freelancerId,
                         Amount = amount,
                         ProjectId = projectId,
                         Type = BalanceTransactionType.Payout
                    },
                    new BalanceTransaction
                    {
                         UserId = "PLATFORM",
                         Amount = commission,
                         ProjectId = projectId,
                         Type = BalanceTransactionType.Commission
                    }
               );
          }

          await _context.SaveChangesAsync();
     }

     public async Task WithdrawAsync(string userId, decimal amount, int paymentId)
     {
          var balance = await GetAsync(userId);

          if (balance.Balance < amount)
          {
               throw new InvalidOperationException("Недостаточно средств");
          }
          
          balance.Balance -= amount;

          _context.BalanceTransactions.Add(new BalanceTransaction
          {
               UserId = userId,
               Amount = amount,
               PaymentId = paymentId,
               Type = BalanceTransactionType.Withdraw
          });
          
          await _context.SaveChangesAsync();
     }
}