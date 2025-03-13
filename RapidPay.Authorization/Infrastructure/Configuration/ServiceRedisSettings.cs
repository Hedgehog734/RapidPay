using RapidPay.Shared.Configuration;

namespace RapidPay.Authorization.Infrastructure.Configuration;

public class ServiceRedisSettings : RedisSettings
{
    public int LockPeriodSeconds { get; set; }
    public int FraudPeriodSeconds { get; set; }
}