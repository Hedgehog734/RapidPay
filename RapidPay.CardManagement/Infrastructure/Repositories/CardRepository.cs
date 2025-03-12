using Microsoft.EntityFrameworkCore;
using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Persistence;
using RapidPay.Shared.Helpers;

namespace RapidPay.CardManagement.Infrastructure.Repositories;

public class CardRepository(CardDbContext context) : ICardRepository
{
    public async Task<Card?> GetByNumberAsync(string cardNumber)
    {
        return await context.Cards.FirstOrDefaultAsync(x => x.CardNumber == cardNumber);
    }

    public async Task<Card> AddAsync(Card card)
    {
        await context.Cards.AddAsync(card);
        await context.SaveChangesAsync();
        return card;
    }

    public async Task UpdateAsync(Card card)
    {
        context.Cards.Update(card);
        await context.SaveChangesAsync();
    }

    public async Task<bool> WithdrawAsync(string cardNumber, decimal amount)
    {
        var card = await GetByNumberAsync(cardNumber);

        if (card == null || !FundsHelper.HasSufficientFunds(card.Balance, card.CreditLimit,
                card.UsedCredit, amount))
        {
            return false;
        }

        if (card.Balance >= amount)
        {
            card.Balance -= amount;
        }
        else
        {
            var remainingAmount = amount - card.Balance;
            card.Balance = 0;
            card.UsedCredit += remainingAmount;
        }

        context.Cards.Update(card);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DepositAsync(string cardNumber, decimal amount, decimal fee = 0)
    {
        amount -= fee; // just subtract the commission as per requirements

        var card = await GetByNumberAsync(cardNumber);

        if (card == null)
        {
            return false;
        }

        if (card.UsedCredit > 0)
        {
            var repaymentAmount = Math.Min(amount, card.UsedCredit ?? 0);
            card.UsedCredit -= repaymentAmount;
            amount -= repaymentAmount;
        }

        if (amount > 0)
        {
            card.Balance += amount;
        }

        context.Cards.Update(card);
        await context.SaveChangesAsync();
        return true;
    }
}