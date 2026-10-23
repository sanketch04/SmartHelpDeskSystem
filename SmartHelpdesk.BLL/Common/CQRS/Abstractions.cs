using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHelpdesk.BLL.Common.CQRS;
public interface ICommand<TResult>{}
public interface IQuery<TResult>{ }

public interface ICommandHandler<TCommand,TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQueryhandler<TQuery,TResult> where TQuery:IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
