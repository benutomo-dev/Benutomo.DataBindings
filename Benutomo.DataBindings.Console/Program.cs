using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Benutomo;
using Benutomo.DataBindings;
using Benutomo.DataBindings.Console;

var x1 = new StructX();
var x2 = new StructX();


var ret1 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(x1), 1).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(x2), 1));

x1.obj = new object();

var ret2 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(x1), 1).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(x2), 1));

x2.obj = x1.obj;

var ret3 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(x1), 1).SequenceEqual(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(x2), 1));


var a = new TestA1();
var b = new TestB();

//DataBinding.MakeBinding(a, a => a[int.MinValue][2].A, b, b => b.B);

Console.WriteLine(a.A);
Console.WriteLine(b.B);
Console.WriteLine();

BindingScope(a, b);

GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
GC.WaitForPendingFinalizers();

a.A = 999;

Console.WriteLine(a.A);
Console.WriteLine(b.B);
Console.WriteLine();

b.B = "hogehoge";

Console.WriteLine(a.A);
Console.WriteLine(b.B);
Console.WriteLine();


static void BindingScope(TestA1 a, TestB b)
{
    using (var context = DataBinding.MakeBinding(a, a => a.A, b, b => b.B))
    {
        context.ForwardConverter = num => num.ToString();
        context.BackwardConverter = text => int.Parse(text!);

        Console.WriteLine(a.A);
        Console.WriteLine(b.B);
        Console.WriteLine();

        a.A = 99;

        Console.WriteLine(a.A);
        Console.WriteLine(b.B);
        Console.WriteLine();

        b.B = "33";

        Console.WriteLine(a.A);
        Console.WriteLine(b.B);
        Console.WriteLine();
    }
}



namespace Benutomo.DataBindings.Console
{

    public struct StructX
    {
        public int n;
        public object obj;
    }

    public partial class TestA1
    {
        [EnableNotificationSupport]
        [ChangedEvent]
        public int A { get => _A(); set => _A(value); }

        [EnableNotificationSupport]
        [ChangedEvent]
        public TestB B { get => _B(); set => _B(value); }

        public TestA2[] this[int i]
        {
            get => new TestA2[0];
        }

        public void X()
        {
        }

        public TestA1()
        {
            __b = new TestB();
        }
    }

    public class TestA2
    {
        public int A { get; set; }

        public void X()
        {
        }
    }

    public partial class TestB
    {
        [EnableNotificationSupport]
        [ChangedEvent]
        public string B { get => _B(); set => _B(value); }

        public TestB()
        {
            __b = "2";
        }
    }
}