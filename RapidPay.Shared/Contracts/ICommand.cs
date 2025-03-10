using MediatR;

namespace RapidPay.Shared.Contracts;

public interface ICommand<out TResponse> : IRequest<TResponse>;