using System.Runtime.CompilerServices;

namespace Benutomo.DataBindings
{
    internal sealed class ClassMemberValue<ClassT, MemberT> : ClassMemberValueBase<ClassT, MemberT, IMemberAccessStrategy<ClassT, MemberT>, IMemberValueChagedObserveStrategy<ClassT>>, IValueAccesser<MemberT>
        where ClassT : class
    {
        public void SetValue(MemberT? value, bool ignoreIfSameObject)
        {
            var currentObject = FetchCurrentObject();

            if (currentObject is not null)
            {
                if (ignoreIfSameObject && MemberAccessStrategy.TryGetMemberValue(currentObject, out var currentValue))
                {
                    if (typeof(MemberT).IsValueType)
                    {
                        unsafe
                        {
                            // 値型の場合、もしEqualsが不適切にオーバーライドされていたとしても、currentValueとvalueのバイト列が完全一致ならば両者は区別不能なので、同じである

                            if (new ReadOnlySpan<byte>(Unsafe.AsPointer(ref currentValue), Unsafe.SizeOf<MemberT>()).SequenceEqual(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref value), Unsafe.SizeOf<MemberT>())))
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (ReferenceEquals(currentValue, value))
                        {
                            return;
                        }
                    }
                }

                MemberAccessStrategy.TrySetMemberValue(currentObject, value);
            }
        }


        public ClassMemberValue(IReadOnlyValueAccessor<ClassT> objectAccessor, IMemberAccessStrategy<ClassT, MemberT> memberAccessStrategy, IMemberValueChagedObserveStrategy<ClassT> memberObserveStrategy) : base(objectAccessor, memberAccessStrategy, memberObserveStrategy)
        {

        }
    }
}