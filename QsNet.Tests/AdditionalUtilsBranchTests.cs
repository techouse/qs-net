using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using QsNet.Internal;
using QsNet.Models;
using Xunit;

namespace QsNet.Tests;

public class AdditionalUtilsBranchTests
{
    private sealed class NonDisposableEmptyEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator() => new NonDisposableEmptyEnumerator();

        private sealed class NonDisposableEmptyEnumerator : IEnumerator
        {
            public bool MoveNext() => false; // empty
            public void Reset() { }
            public object Current => null!;
        }
    }

    private sealed class NonDisposableSingleEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator() => new NonDisposableSingleEnumerator();

        private sealed class NonDisposableSingleEnumerator : IEnumerator
        {
            private int _state;
            public bool MoveNext()
            {
                if (_state != 0) return false;
                _state = 1;
                return true; // one item
            }
            public void Reset() => _state = 0;
            public object Current => 123; // any value
        }
    }

    [Fact]
    public void IsEmpty_ReturnsTrueForNullAndUndefined_AndHandlesStrings()
    {
        Utils.IsEmpty(null).Should().BeTrue();
        Utils.IsEmpty(Undefined.Instance).Should().BeTrue();
        Utils.IsEmpty(string.Empty).Should().BeTrue();
        Utils.IsEmpty("x").Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_EnumerableWithNonDisposableEnumerator_CoversHasAnyBothOutcomes()
    {
        // Enumerator does not implement IDisposable: covers the HasAny branch where cast to IDisposable is null
        Utils.IsEmpty(new NonDisposableEmptyEnumerable()).Should().BeTrue();
        Utils.IsEmpty(new NonDisposableSingleEnumerable()).Should().BeFalse();

        // Regular enumerable (List) enumerator implements IDisposable: covers the other HasAny branch
        Utils.IsEmpty(new List<int>()).Should().BeTrue();
        Utils.IsEmpty(new List<int> { 1 }).Should().BeFalse();
    }

    [Fact]
    public void Apply_DefaultBranch_WhenTypeMismatch_ReturnsOriginalValue()
    {
        // T is int, value is string => should hit the default branch and return the original string
        object input = "abc";
        var result = Utils.Apply<int>(input, x => x * 2);
        result.Should().BeSameAs(input);
    }

    private sealed class CustomType { }

    [Fact]
    public void IsNonNullishPrimitive_DefaultBranch_ReturnsTrueForCustomType()
    {
        // Not string/number/bool/enum/DateTime/Uri/IEnumerable/IDictionary/Undefined/null
        // Should match the default case and return true
        Utils.IsNonNullishPrimitive(new CustomType()).Should().BeTrue();
    }
}
