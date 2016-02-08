using System.Collections;
using System.ComponentModel;
using System.Linq;

namespace TableTweaker
{
    public class ResultObject
    {
        private readonly object _o;
        private readonly PropertyDescriptor _property;
        private bool _initialized;
        private string _header;
        private IEnumerable _children;

        public ResultObject(object o, PropertyDescriptor property = null)
        {
            _o = o;
            _property = property;
        }

        public string Header
        {
            get
            {
                Initialize();
                return _header;
            }
        }

        public IEnumerable Children
        {
            get
            {
                Initialize();
                return _children;
            }
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_o == null)
            {
                _header = "<null>";
                return;
            }

            if (_property != null)
            {
                var value = _property.GetValue(_o);
                _header = _property.Name + " = " + value;
                _children = new[] { value };
                return;
            }

            var e = _o as IEnumerable;
            if (e != null)
            {
                var enumerableChildren = e.Cast<object>().Select(x => new ResultObject(x)).ToArray();
                _children = enumerableChildren;
                _header = $"<enumerable count={enumerableChildren.Length}>";
                return;
            }

            var properties = TypeDescriptor.GetProperties(_o).Cast<PropertyDescriptor>()
                .Select(p => new ResultObject(_o, p)).ToArray();
            _header = _o.ToString();
            if (properties.Length > 0)
            {
                _children = properties;
            }
        }
    }
}