﻿using KIARA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("ServiceBus")]

namespace FIVES
{
    public class Attribute
    {
        public Attribute(ReadOnlyAttributeDefinition definition, Component parentComponent)
        {
            ParentComponent = parentComponent;
            Definition = definition;
            Value = definition.DefaultValue;
        }

        public Component ParentComponent { get; private set; }

        public object Value
        {
            get
            {
                return CurrentValue;
            }
            
            internal set 
            {
                Set(value);
            }
        }

        public Type Type
        {
            get { return Definition.Type; }
        }

        public void Suggest(object newValue, Guid suggester)
        {
            if(newValue == null && !CanBeAssignedNull(this.Type))
                    throw(new AttributeAssignmentException("Attribute of type " + this.Type
                    + " can not be assigned from provided value" + newValue + " of type " + newValue.GetType()) );
            try
            {
                var convertedValue = convertValueToAttributeType(newValue);
                if (ParentComponent.ContainingEntity.Owner.Equals(World.Instance.ID)
                    && World.Instance.ContainsEntity(ParentComponent.ContainingEntity.Guid))
                {
                    var proposedChange =
                        new ProposeAttributeChangeEventArgs(ParentComponent.ContainingEntity,
                                                            ParentComponent.Name,
                                                            Definition.Name,
                                                            convertedValue,
                                                            suggester);

                    ParentComponent.ContainingEntity.PublishAttributeChangeSuggestion(proposedChange);
                }
                else
                {
                    Set(convertedValue, suggester);
                }
            }
            catch
            {
                throw new AttributeAssignmentException("Attribute of type " + this.Type
                    + " can not be assigned from provided value" + newValue + " of type " + newValue.GetType());
            }
        }

        public void Suggest(object newValue)
        {
            Suggest(newValue, World.Instance.ID);
        }

        private object convertValueToAttributeType(object value)
        {
            if (value == null || value.GetType() == this.Type)
                return value;

            if (World.Instance.Ktd != null && World.Instance.Ktd.ContainsType(this.Type.Name))
            {
                KtdType typeAsKtdType = World.Instance.Ktd.GetKtdType(this.Type.Name);
                return typeAsKtdType.AssignValuesToNativeType(value, this.Type);
            }

            return Convert.ChangeType(value, this.Type);
        }

        public T As<T>()
        {
            if (!CurrentValue.GetType().IsAssignableFrom(typeof(T)) && World.Instance.Ktd.ContainsType(Type.Name))
            {
                KtdType typeAsKtdType = World.Instance.Ktd.GetKtdType(Type.Name);
                Type t = typeof(T);
                return (T)typeAsKtdType.AssignValuesToNativeType(Value, t);
            }
            else
            {
                return (T)Value;
            }
        }

        internal void Set(object value)
        {
            Set(value, World.Instance.ID);
        }

        internal void Set(object value, Guid setBy)
        {
            var oldValue = CurrentValue;
            CurrentValue = value;
            lastChangedBy = setBy;
            ParentComponent.raiseChangeEvent(Definition.Name, oldValue, CurrentValue, lastChangedBy);
        }

        private static bool CanBeAssignedNull(Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }

        private ReadOnlyAttributeDefinition Definition;
        private object CurrentValue;
        private Guid lastChangedBy;
    }
}
