using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace GuessWhoServices.Infrastructure
{
    internal readonly record struct TryValueResult<T>(bool Found, T Value);

    internal static class ConcurrentDictionaryAccess
    {
        internal static TryValueResult<TValue> TryGetValue<TKey, TValue>(
            ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            return ConcurrentDictionaryInvoker<TKey, TValue>.TryGetValue(dictionary, key);
        }

        internal static TryValueResult<TValue> TryRemove<TKey, TValue>(
            ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            return ConcurrentDictionaryInvoker<TKey, TValue>.TryRemove(dictionary, key);
        }

        private static class ConcurrentDictionaryInvoker<TKey, TValue>
        {
            internal static readonly Func<ConcurrentDictionary<TKey, TValue>, TKey, TryValueResult<TValue>> TryGetValue =
                BuildTryGetValue();

            internal static readonly Func<ConcurrentDictionary<TKey, TValue>, TKey, TryValueResult<TValue>> TryRemove =
                BuildTryRemove();

            private static Func<ConcurrentDictionary<TKey, TValue>, TKey, TryValueResult<TValue>> BuildTryGetValue()
            {
                MethodInfo method = typeof(ConcurrentDictionary<TKey, TValue>).GetMethod(
                    nameof(ConcurrentDictionary<TKey, TValue>.TryGetValue),
                    BindingFlags.Public | BindingFlags.Instance);

                if (method == null)
                {
                    throw new MissingMethodException(typeof(ConcurrentDictionary<TKey, TValue>).FullName, "TryGetValue");
                }

                ParameterExpression dictParam = Expression.Parameter(typeof(ConcurrentDictionary<TKey, TValue>), "dictionary");
                ParameterExpression keyParam = Expression.Parameter(typeof(TKey), "key");
                ParameterExpression valueVar = Expression.Variable(typeof(TValue), "value");

                ConstructorInfo ctor = typeof(TryValueResult<TValue>).GetConstructor(new[] { typeof(bool), typeof(TValue) });
                if (ctor == null)
                {
                    throw new MissingMethodException(typeof(TryValueResult<TValue>).FullName, ".ctor");
                }

                MethodCallExpression call = Expression.Call(dictParam, method, keyParam, valueVar);

                NewExpression whenTrue = Expression.New(ctor, Expression.Constant(true), valueVar);
                NewExpression whenFalse = Expression.New(ctor, Expression.Constant(false), Expression.Default(typeof(TValue)));

                Expression body = Expression.Block(
                    new[] { valueVar },
                    Expression.Condition(call, whenTrue, whenFalse));

                return Expression.Lambda<Func<ConcurrentDictionary<TKey, TValue>, TKey, TryValueResult<TValue>>>(
                        body,
                        dictParam,
                        keyParam)
                    .Compile();
            }

            private static Func<ConcurrentDictionary<TKey, TValue>, TKey, TryValueResult<TValue>> BuildTryRemove()
            {
                MethodInfo method = typeof(ConcurrentDictionary<TKey, TValue>).GetMethod(
                    nameof(ConcurrentDictionary<TKey, TValue>.TryRemove),
                    BindingFlags.Public | BindingFlags.Instance,
                    binder: null,
                    types: new[] { typeof(TKey), typeof(TValue).MakeByRefType() },
                    modifiers: null);

                if (method == null)
                {
                    throw new MissingMethodException(typeof(ConcurrentDictionary<TKey, TValue>).FullName, "TryRemove");
                }

                ParameterExpression dictParam = Expression.Parameter(typeof(ConcurrentDictionary<TKey, TValue>), "dictionary");
                ParameterExpression keyParam = Expression.Parameter(typeof(TKey), "key");
                ParameterExpression valueVar = Expression.Variable(typeof(TValue), "value");

                ConstructorInfo ctor = typeof(TryValueResult<TValue>).GetConstructor(new[] { typeof(bool), typeof(TValue) });
                if (ctor == null)
                {
                    throw new MissingMethodException(typeof(TryValueResult<TValue>).FullName, ".ctor");
                }

                MethodCallExpression call = Expression.Call(dictParam, method, keyParam, valueVar);

                NewExpression whenTrue = Expression.New(ctor, Expression.Constant(true), valueVar);
                NewExpression whenFalse = Expression.New(ctor, Expression.Constant(false), Expression.Default(typeof(TValue)));

                Expression body = Expression.Block(
                    new[] { valueVar },
                    Expression.Condition(call, whenTrue, whenFalse));

                return Expression.Lambda<Func<ConcurrentDictionary<TKey, TValue>, TKey, TryValueResult<TValue>>>(
                        body,
                        dictParam,
                        keyParam)
                    .Compile();
            }
        }
    }
}
