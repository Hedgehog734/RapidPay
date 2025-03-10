using MediatR;

namespace RapidPay.Shared.Contracts;

public interface IQuery<out TResponse> : IRequest<TResponse>;