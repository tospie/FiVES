﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FIVES
{
    public class ProposeAttributeChangeEventArgs : EventArgs
    {

        public ProposeAttributeChangeEventArgs(Entity entity, string componentName, string attributeName, object value, Guid suggester)
        {
            this.entity = entity;
            this.componentName = componentName;
            this.attributeName = attributeName;
            this.value = value;
            this.Suggester = suggester;
        }

        public Entity Entity
        {
            get { return entity;  }
        }

        public string ComponentName
        {
            get { return componentName; }
        }

        public string AttributeName
        {
            get { return attributeName; }
        }

        public object Value
        {
            get
            {
                return value;
            }
        }

        Entity entity;
        string componentName;
        string attributeName;
        object value;
        public Guid Suggester { get; private set; }
    }
}
