using FreelancePlatform.Context;
using FreelancePlatform.Models;

namespace FreelancePlatform.Services;

/// <summary>
/// Предоставляет методы управления балансом пользователей,
/// включая пополнение, заморозку, возврат, вывод средств и выплаты.
/// </summary>
public class BalanceService : IBalanceService
{
     private readonly AppDbContext _context;
     private const decimal commissionPercent = 0.1m;

     public BalanceService(AppDbContext context)
     {
          _context = context;
     }

     /// <summary>
     /// Возвращает баланс пользователя. Если запись отсутствует,
     /// создаёт её с нулевыми значениями.
     /// </summary>
     /// <param name="userId">Идентификатор пользователя.</param>
     /// <returns>Объект баланса пользователя.</returns>
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

     /// <summary>
     /// Пополняет баланс пользователя и сохраняет информацию о транзакции.
     /// </summary>
     /// <param name="userId">Идентификатор пользователя.</param>
     /// <param name="amount">Сумма пополнения.</param>
     /// <param name="paymentId">Идентификатор платежа.</param>
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
     
     /// <summary>
     /// Замораживает средства пользователя для оплаты заказа.
     /// </summary>
     /// <param name="userId">Идентификатор пользователя.</param>
     /// <param name="amount">Сумма заморозки.</param>
     /// <param name="orderId">Идентификатор заказа.</param>
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
     
     /// <summary>
     /// Замораживает средства пользователя для оплаты проекта.
     /// </summary>
     /// <param name="userId">Идентификатор пользователя.</param>
     /// <param name="amount">Сумма заморозки.</param>
     /// <param name="projectId">Идентификатор проекта.</param>
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
     
     /// <summary>
     /// Возвращает пользователю ранее замороженные средства по заказу.
     /// </summary>
     /// <param name="userId">Идентификатор пользователя.</param>
     /// <param name="amount">Возвращаемая сумма.</param>
     /// <param name="orderId">Идентификатор заказа.</param>
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
     
     /// <summary>
     /// Возвращает пользователю ранее замороженные средства по проекту.
     /// </summary>
     /// <param name="userId">Идентификатор пользователя.</param>
     /// <param name="amount">Возвращаемая сумма.</param>
     /// <param name="projectId">Идентификатор проекта.</param>
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

     /// <summary>
     /// Возвращает пользователю средства после отмены пополнения.
     /// </summary>
     /// <param name="userId">Идентификатор пользователя.</param>
     /// <param name="amount">Возвращаемая сумма.</param>
     /// <param name="paymentId">Идентификатор платежа.</param>
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
     
     /// <summary>
     /// Переводит замороженные средства клиента исполнителю по заказу,
     /// удерживая комиссию платформы.
     /// </summary>
     /// <param name="clientId">Идентификатор клиента.</param>
     /// <param name="freelancerId">Идентификатор исполнителя.</param>
     /// <param name="amount">Сумма выплаты.</param>
     /// <param name="orderId">Идентификатор заказа.</param>
     public async Task ReleaseForOrderAsync(string clientId, string freelancerId, decimal amount, int orderId)
     {
          var client = await GetAsync(clientId);
          var freelancer = await GetAsync(freelancerId);

          if (client.Frozen < amount)
          {
               throw new InvalidOperationException("Недостаточно замороженных средств");
          }

          var commission = amount * commissionPercent;
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
     
     /// <summary>
     /// Переводит замороженные средства клиента исполнителю по проекту,
     /// удерживая комиссию платформы.
     /// </summary>
     /// <param name="clientId">Идентификатор клиента.</param>
     /// <param name="freelancerId">Идентификатор исполнителя.</param>
     /// <param name="amount">Сумма выплаты.</param>
     /// <param name="projectId">Идентификатор проекта.</param>
     public async Task ReleaseForProjectAsync(string clientId, string freelancerId, decimal amount, int projectId)
     {
          var client = await GetAsync(clientId);
          var freelancer = await GetAsync(freelancerId);

          if (client.Frozen < amount)
          {
               throw new InvalidOperationException("Недостаточно замороженных средств");
          }

          var commission = amount * commissionPercent;
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

     /// <summary>
     /// Распределяет оплату между участниками командного проекта
     /// с удержанием комиссии платформы.
     /// </summary>
     /// <param name="clientId">Идентификатор клиента.</param>
     /// <param name="payouts">Список выплат участникам проекта.</param>
     /// <param name="projectId">Идентификатор проекта.</param>
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

          foreach (var (freelancerId, _, amount) in payouts)
          {
               var commission = Math.Round(amount * commissionPercent, 2);
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

     /// <summary>
     /// Выполняет вывод средств с баланса пользователя.
     /// </summary>
     /// <param name="userId">Идентификатор пользователя.</param>
     /// <param name="amount">Сумма вывода.</param>
     /// <param name="paymentId">Идентификатор платежа.</param>
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