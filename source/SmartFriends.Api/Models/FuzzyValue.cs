using System;

namespace SmartFriends.Api.Models
{
    public class FuzzyValue
    {
        private readonly Type _valueType;
        public object Value { get; }
        public bool IsHsv => typeof(HsvValue) == _valueType;
        public bool ShouldSerialize => IsHsv || typeof(DateTime) == _valueType;

        public FuzzyValue(object value)
        {
            _valueType = value?.GetType() ?? typeof(string);
            Value = value;
        }
    }
}
