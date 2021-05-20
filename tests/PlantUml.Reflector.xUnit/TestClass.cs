using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming
#pragma warning disable 414

#nullable enable
namespace ArchMastery.Structurizer.Reflector.xUnit
{
    public abstract class TestBase<TValue>
        where TValue : struct
    {
    }

    public record MyEntity(string Message);

    public sealed class TestClass<TValue> : TestBase<TValue>, INotifyPropertyChanged
        where TValue : struct
    {
#pragma warning disable 649
#pragma warning disable 8618
        private static readonly InnerClass<TValue>[] _innerClass;
#pragma warning restore 8618
#pragma warning restore 649
        private static readonly IEnumerable<InnerClass<DateTime>> _datesField = new List<InnerClass<DateTime>>();

        public TestClass(string value)
        {
            Property = value;
        }

        private InnerClass<TValue>[] InnerProperty => _innerClass;
        internal IEnumerable<InnerClass<DateTime>> DatesProperty => _datesField;

        private string Property { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void ResetProperty()
        {
            Property = string.Empty;
        }

        public TValue Convert<TFrom>(TFrom from)
            where TFrom : struct
        {
            return (TValue) (object) from;
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static InnerClass<TValue> CreateInnerClass()
        {
            return _innerClass.FirstOrDefault() ?? new InnerClass<TValue>();
        }

        public class InnerClass<T>
            where T : struct
        {
            private static T _value = default;
        }
    }

    public static class Extensions
    {
        public static string GetName<TValue, T>(this TestClass<TValue>.InnerClass<T> innerClass)
            where TValue : struct
            where T : struct
        {
            return innerClass.GetType().FullName ?? innerClass.GetType().Name;
        }
    }
}
