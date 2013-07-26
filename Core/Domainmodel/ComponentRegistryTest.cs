
using NUnit.Framework;
using System;

namespace FIVES
{
    [TestFixture()]
    public class ComponentRegistryTest
    {
        ComponentLayout layout = new ComponentLayout();
        ComponentRegistry registry;
        string name = "myComponent";

        public ComponentRegistryTest() {
            layout["i"] = AttributeType.INT;
            layout["f"] = AttributeType.FLOAT;
            layout["s"] = AttributeType.STRING;
            layout["b"] = AttributeType.BOOL;
        }

        [SetUp()]
        public void init() {
            registry = new ComponentRegistry();
        }

        [Test()]
        public void shouldDefineComponentsWithoutException()
        {
            registry.defineComponent(name, Guid.NewGuid(), layout);
        }

        [Test()]
        public void shouldNotThrowExceptionWhenRedefiningSameComponent()
        {
            Guid owner = Guid.NewGuid();
            registry.defineComponent(name, owner, layout);
            registry.defineComponent(name, owner, layout);
        }

        [Test()]
        public void shouldThrowExceptionWhenRedefiningComponentWithDifferentLayout()
        {
            Guid owner = Guid.NewGuid();

            ComponentLayout otherLayout = new ComponentLayout();
            otherLayout["a"] = AttributeType.BOOL;

            registry.defineComponent(name, owner, layout);
            Assert.Throws<ComponentAlreadyDefinedException>(
                delegate() { registry.defineComponent(name, owner, otherLayout); } );
        }

        [Test()]
        public void shouldThrowExceptionWhenRedefiningComponentWithDifferentOwner()
        {
            registry.defineComponent(name, Guid.NewGuid(), layout);
            Assert.Throws<ComponentAlreadyDefinedException>(
                delegate() { registry.defineComponent(name, Guid.NewGuid(), layout); } );
            
        }

        [Test()]
        public void shouldFailToConstructUndefinedComponent() {
            Assert.Throws<ComponentIsNotDefinedException>(
                delegate() { registry.createComponent("foobar"); } );
        }

        [Test()]
        public void shouldCreateDefinedLayoutAttributesInNewComponents() 
        {
            registry.defineComponent(name, Guid.NewGuid(), layout);
            Component c = registry.createComponent(name);
            Assert.Null(c.getIntAttribute("i"));
            Assert.Null(c.getFloatAttribute("f"));
            Assert.Null(c.getStringAttribute("s"));
            Assert.Null(c.getBoolAttribute("b"));
        }

        [Test()]
        public void shouldNotCreateUndefinedAttributesInNewComponents() 
        {
            registry.defineComponent(name, Guid.NewGuid(), layout);
            Component c = registry.createComponent(name);
            Assert.Throws<AttributeIsNotDefinedException>(
                delegate() { c.getIntAttribute("foobar"); } );
        }
    }
}
