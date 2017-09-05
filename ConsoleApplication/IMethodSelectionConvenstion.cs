using Castle.DynamicProxy;

namespace ConsoleApplication
{
    public interface IMethodSelectionConvenstion
    {
        bool HasSupport(IInvocation invocation);
    }

    public class DefaultMethodSelectionConvenstion : IMethodSelectionConvenstion
    {
        public bool HasSupport(IInvocation invocation)
        {
            return true;
        }
    }
}