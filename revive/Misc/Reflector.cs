using System;
using System.Reflection;

namespace lethalCompanyRevive.Misc
{
    public class Reflector
    {
        const BindingFlags privateOrInternal = BindingFlags.NonPublic | BindingFlags.Instance;
        const BindingFlags internalStatic = BindingFlags.NonPublic | BindingFlags.Static;
        const BindingFlags internalField = privateOrInternal | BindingFlags.GetField;
        const BindingFlags internalStaticField = internalStatic | BindingFlags.GetField;
        const BindingFlags internalProperty = privateOrInternal | BindingFlags.GetProperty;
        const BindingFlags internalMethod = privateOrInternal | BindingFlags.InvokeMethod;
        const BindingFlags internalStaticMethod = internalStatic | BindingFlags.InvokeMethod;
        object Obj { get; }
        Type ObjType { get; }

        Reflector(object obj)
        {
            Obj = obj;
            ObjType = obj.GetType();
        }

        T GetField<T>(string variableName, BindingFlags flags)
        {
            try
            {
                return (T)ObjType.GetField(variableName, flags).GetValue(Obj);
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        Reflector GetProperty(string propertyName, BindingFlags flags)
        {
            try
            {
                return new(ObjType.GetProperty(propertyName, flags).GetValue(Obj, null));
            }
            catch
            {
                return null;
            }
        }

        Reflector SetField(string variableName, object value, BindingFlags flags)
        {
            try
            {
                ObjType.GetField(variableName, flags).SetValue(Obj, value);
                return this;
            }
            catch
            {
                return null;
            }
        }

        Reflector SetProperty(string propertyName, object value, BindingFlags flags)
        {
            try
            {
                ObjType.GetProperty(propertyName, flags).SetValue(Obj, value, null);
                return this;
            }
            catch
            {
                return null;
            }
        }

        T InvokeMethod<T>(string methodName, BindingFlags flags, params object[] args)
        {
            try
            {
                return (T)ObjType.GetMethod(methodName, flags).Invoke(Obj, args);
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        public T GetInternalField<T>(string variableName) => GetField<T>(variableName, internalField);
        public T GetInternalStaticField<T>(string variableName) => GetField<T>(variableName, internalStaticField);

        public Reflector GetInternalField(string variableName)
        {
            object o = GetInternalField<object>(variableName);
            return o is null ? null : new(o);
        }

        public Reflector GetInternalStaticField(string variableName)
        {
            object o = GetInternalStaticField<object>(variableName);
            return o is null ? null : new(o);
        }

        public Reflector SetInternalField(string variableName, object value)
            => SetField(variableName, value, internalField);

        public Reflector SetInternalStaticField(string variableName, object value)
            => SetField(variableName, value, internalStaticField);

        public Reflector GetInternalProperty(string propertyName)
            => GetProperty(propertyName, internalProperty);

        public Reflector SetInternalProperty(string propertyName, object value)
            => SetProperty(propertyName, value, internalProperty);

        public T InvokeInternalMethod<T>(string methodName, params object[] args)
            => InvokeMethod<T>(methodName, internalMethod, args);

        public Reflector InvokeInternalMethod(string methodName, params object[] args)
        {
            object obj = InvokeInternalMethod<object>(methodName, args);
            return obj is null ? null : new Reflector(obj);
        }

        public static Reflector Target(object obj) => new(obj);
    }
}
