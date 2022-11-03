using System.Linq.Expressions;
using System.Reflection;

namespace AnimeSearch.Core.Extensions;

public static class Expression
{
    public static MemberInfo GetPropertyMemberInfo<T>(this Expression<Func<T, object>> expression)
    {
        if (expression == null)
            return null;

        if (expression.Body is not MemberExpression memberExpression)
            memberExpression = ((UnaryExpression)expression.Body).Operand as MemberExpression;

        return memberExpression?.Member;
    }

    public static Type GetMemberUnderlyingType(this MemberInfo member)
    {
        return member.MemberType switch
        {
            MemberTypes.Field => ((FieldInfo)member).FieldType,
            MemberTypes.Property => ((PropertyInfo)member).PropertyType,
            MemberTypes.Event => ((EventInfo)member).EventHandlerType,
            _ => throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", nameof(member)),
        };
    }

    public static BinaryExpression CreateNullChecks(this System.Linq.Expressions.Expression expression, bool skipFinalMember = false)
    {
        var parents = new Stack<BinaryExpression>();

        BinaryExpression newExpression = null;

        if (expression is UnaryExpression unary)
        {
            expression = unary.Operand;
        }

        var temp = expression as MemberExpression;

        while (temp is MemberExpression member)
        {
            try
            {
                var nullCheck = System.Linq.Expressions.Expression.NotEqual(temp, System.Linq.Expressions.Expression.Constant(null));
                parents.Push(nullCheck);
            }
            catch (InvalidOperationException) { }

            temp = member.Expression as MemberExpression;
        }

        while (parents.Count > 0)
        {
            if (skipFinalMember && parents.Count == 1 && newExpression != null)
                break;

            newExpression = newExpression == null ? parents.Pop() : System.Linq.Expressions.Expression.AndAlso(newExpression, parents.Pop());
        }

        return newExpression ?? System.Linq.Expressions.Expression.Equal(System.Linq.Expressions.Expression.Constant(true), System.Linq.Expressions.Expression.Constant(true));
    }
}