using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using NUnit.Framework;

using Python.Runtime;

using PyRuntime = Python.Runtime.Runtime;

namespace Python.EmbeddingTest
{
    public class TestConverter
    {
        static readonly Type[] _numTypes = new Type[]
        {
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong)
        };

        [OneTimeSetUp]
        public void SetUp()
        {
            PythonEngine.Initialize();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            PythonEngine.Shutdown();
        }

        [Test]
        public void TestConvertSingleToManaged(
            [Values(float.PositiveInfinity, float.NegativeInfinity, float.MinValue, float.MaxValue, float.NaN,
                float.Epsilon)] float testValue)
        {
            var pyFloat = new PyFloat(testValue);

            object convertedValue;
            var converted = Converter.ToManaged(pyFloat, typeof(float), out convertedValue, false);

            Assert.IsTrue(converted);
            Assert.IsTrue(((float) convertedValue).Equals(testValue));
        }

        [Test]
        public void TestConvertDoubleToManaged(
            [Values(double.PositiveInfinity, double.NegativeInfinity, double.MinValue, double.MaxValue, double.NaN,
                double.Epsilon)] double testValue)
        {
            var pyFloat = new PyFloat(testValue);

            object convertedValue;
            var converted = Converter.ToManaged(pyFloat, typeof(double), out convertedValue, false);

            Assert.IsTrue(converted);
            Assert.IsTrue(((double) convertedValue).Equals(testValue));
        }

        [Test]
        public void CovertTypeError()
        {
            Type[] floatTypes = new Type[]
            {
                typeof(float),
                typeof(double)
            };
            using (var s = new PyString("abc"))
            {
                foreach (var type in _numTypes.Union(floatTypes))
                {
                    object value;
                    try
                    {
                        bool res = Converter.ToManaged(s, type, out value, true);
                        Assert.IsFalse(res);
                        var bo = Exceptions.ExceptionMatches(Exceptions.TypeError);
                        Assert.IsTrue(Exceptions.ExceptionMatches(Exceptions.TypeError)
                            || Exceptions.ExceptionMatches(Exceptions.ValueError));
                    }
                    finally
                    {
                        Exceptions.Clear();
                    }
                }
            }
        }

        [Test]
        public void ConvertOverflow()
        {
            using (var num = new PyInt(ulong.MaxValue))
            {
                using var largeNum = PyRuntime.PyNumber_Add(num, num);
                try
                {
                    object value;
                    foreach (var type in _numTypes)
                    {
                        bool res = Converter.ToManaged(largeNum.BorrowOrThrow(), type, out value, true);
                        Assert.IsFalse(res);
                        Assert.IsTrue(Exceptions.ExceptionMatches(Exceptions.OverflowError));
                        Exceptions.Clear();
                    }
                }
                finally
                {
                    Exceptions.Clear();
                }
            }
        }

        [Test]
        public void NoImplicitConversionToBool()
        {
            var pyObj = new PyList(items: new[] { 1.ToPython(), 2.ToPython() }).ToPython();
            Assert.Throws<InvalidCastException>(() => pyObj.As<bool>());
        }

        [Test]
        public void ToNullable()
        {
            const int Const = 42;
            var i = new PyInt(Const);
            var ni = i.As<int?>();
            Assert.AreEqual(Const, ni);
        }

        [Test]
        public void BigIntExplicit()
        {
            BigInteger val = 42;
            var i = new PyInt(val);
            var ni = i.As<BigInteger>();
            Assert.AreEqual(val, ni);
            var nullable = i.As<BigInteger?>();
            Assert.AreEqual(val, nullable);
        }

        [Test]
        public void PyIntImplicit()
        {
            var i = new PyInt(1);
            var ni = (PyObject)i.As<object>();
            Assert.IsTrue(PythonReferenceComparer.Instance.Equals(i, ni));
        }

        [Test]
        public void ToPyList()
        {
            var list = new PyList();
            list.Append("hello".ToPython());
            list.Append("world".ToPython());
            var back = list.ToPython().As<PyList>();
            Assert.AreEqual(list.Length(), back.Length());
        }

        [Test]
        public void RawListProxy()
        {
            var list = new List<string> {"hello", "world"};
            var listProxy = PyObject.FromManagedObject(list);
            var clrObject = (CLRObject)ManagedType.GetManagedObject(listProxy);
            Assert.AreSame(list, clrObject.inst);
        }

        [Test]
        public void RawPyObjectProxy()
        {
            var pyObject = "hello world!".ToPython();
            var pyObjectProxy = PyObject.FromManagedObject(pyObject);
            var clrObject = (CLRObject)ManagedType.GetManagedObject(pyObjectProxy);
            Assert.AreSame(pyObject, clrObject.inst);

#pragma warning disable CS0612 // Type or member is obsolete
            const string handlePropertyName = nameof(PyObject.Handle);
#pragma warning restore CS0612 // Type or member is obsolete
            var proxiedHandle = pyObjectProxy.GetAttr(handlePropertyName).As<IntPtr>();
            Assert.AreEqual(pyObject.DangerousGetAddressOrNull(), proxiedHandle);
        }

        [Test]
        public void GenericToPython()
        {
            int i = 42;
            var pyObject = i.ToPythonAs<IConvertible>();
            var type = pyObject.GetPythonType();
            Assert.AreEqual("int", type.Name);
        }

        // regression for https://github.com/pythonnet/pythonnet/issues/451
        [Test]
        public void CanGetListFromDerivedClass()
        {
            using var scope = Py.CreateScope();
            scope.Import(typeof(GetListImpl).Namespace, asname: "test");
            scope.Exec(@"
class PyGetListImpl(test.GetListImpl):
    pass
    ");
            var pyImpl = scope.Get("PyGetListImpl");
            dynamic inst = pyImpl.Invoke();
            List<string> result = inst.GetList();
            CollectionAssert.AreEqual(new[] { "testing" }, result);
        }

        /// <summary>
        /// Test that when a method returns a concrete type implementing IDisposable,
        /// the object is wrapped as the concrete type (not IDisposable interface),
        /// preserving access to concrete type members and supporting 'with' statements.
        /// </summary>
        [Test]
        public void ConcreteTypeImplementingIDisposable_IsWrappedAsConcreteType()
        {
            using var scope = Py.CreateScope();
            scope.Import(typeof(ConcreteDisposableResource).Namespace, asname: "test");
            
            // Reset static state
            ConcreteDisposableResource.IsDisposed = false;
            ConcreteDisposableResource.InstanceCount = 0;

            // Test that a method returning IDisposable but actually returning a concrete type
            // wraps the object as the concrete type, not the interface
            scope.Exec(@"
import clr
clr.AddReference('Python.EmbeddingTest')
from Python.EmbeddingTest import ConcreteDisposableResource

# Get a resource through a method that declares IDisposable return type
resource = ConcreteDisposableResource.GetResource()

# Verify it's wrapped as the concrete type, not IDisposable
# The concrete type has a GetValue() method that IDisposable doesn't have
value = resource.GetValue()
assert value == 42, f'Expected 42, got {value}'

# Verify the concrete type name is accessible
type_name = resource.GetType().Name
assert type_name == 'ConcreteDisposableResource', f'Expected ConcreteDisposableResource, got {type_name}'

# Verify 'with' statement still works (IDisposable support)
with resource:
    inside_value = resource.GetValue()
    assert inside_value == 42
    assert ConcreteDisposableResource.IsDisposed == False

# After 'with' block, should be disposed
assert ConcreteDisposableResource.IsDisposed == True
");

            // Verify the resource was actually disposed
            Assert.IsTrue(ConcreteDisposableResource.IsDisposed, "Resource should be disposed after 'with' statement");
        }

        /// <summary>
        /// Test that Converter.ToPython wraps concrete types implementing interfaces
        /// as the concrete type, not the interface, when the declared type is an interface.
        /// </summary>
        [Test]
        public void Converter_ToPython_ConcreteTypeOverInterface()
        {
            using (Py.GIL())
            {
                // Create a concrete type that implements IDisposable
                var concreteResource = new ConcreteDisposableResource(100);
                
                // Convert using IDisposable as the declared type (simulating method return type)
                var pyObject = Converter.ToPython(concreteResource, typeof(IDisposable));
                
                // Verify it's wrapped as the concrete type, not IDisposable
                var wrappedObject = ManagedType.GetManagedObject(pyObject.BorrowOrThrow());
                Assert.IsInstanceOf<CLRObject>(wrappedObject);
                
                var clrObject = (CLRObject)wrappedObject;
                var wrappedType = clrObject.inst.GetType();
                
                // Should be the concrete type, not IDisposable
                Assert.AreEqual(typeof(ConcreteDisposableResource), wrappedType);
                Assert.AreNotEqual(typeof(IDisposable), wrappedType);
                
                // Verify we can access concrete type members from Python
                using var scope = Py.CreateScope();
                scope.Set("resource", pyObject.MoveToPyObject());
                var result = scope.Eval("resource.GetValue()");
                Assert.AreEqual(100, result.As<int>());
                
                pyObject.Dispose();
            }
        }
    }

    public interface IGetList
    {
        List<string> GetList();
    }

    public class GetListImpl : IGetList
    {
        public List<string> GetList() => new() { "testing" };
    }

    /// <summary>
    /// A concrete class implementing IDisposable with additional members.
    /// Used to test that methods returning IDisposable but actually returning
    /// concrete types are wrapped as the concrete type, not the interface.
    /// </summary>
    public class ConcreteDisposableResource : IDisposable
    {
        public static bool IsDisposed { get; set; }
        public static int InstanceCount { get; set; }

        private readonly int _value;

        public ConcreteDisposableResource(int value = 42)
        {
            _value = value;
            InstanceCount++;
            IsDisposed = false;
        }

        /// <summary>
        /// A method that exists only on the concrete type, not on IDisposable.
        /// This verifies that the object is wrapped as the concrete type.
        /// </summary>
        public int GetValue() => _value;

        public void Dispose()
        {
            IsDisposed = true;
        }

        /// <summary>
        /// A method that declares IDisposable return type but actually returns
        /// the concrete type. This is the scenario we're testing.
        /// </summary>
        public static IDisposable GetResource()
        {
            return new ConcreteDisposableResource();
        }
    }
}
