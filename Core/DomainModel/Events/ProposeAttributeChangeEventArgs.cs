using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FIVES
{
    public class ProposeAttributeChangeEventArgs : EventArgs
    {

        public ProposeAttributeChangeEventArgs(Entity entity, string componentName, string attributeName, object value, Guid suggester)
        {
            this.Entity = entity;
            this.ComponentName = componentName;
            this.AttributeName = attributeName;
            this.Value = value;
            this.Suggester = suggester;
        }

        public Entity Entity { get; private set; }
        public string ComponentName { get; private set; }
        public string AttributeName { get; private set; }
        public object Value { get; private set; }
        public Guid Suggester { get; private set; }
    }
}
