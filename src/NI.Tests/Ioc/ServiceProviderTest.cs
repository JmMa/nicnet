using System;
using NI.Ioc;
using System.ComponentModel;
using System.Collections;

using NUnit.Framework;

namespace NI.Tests.Ioc
{
	/// <summary>
	/// </summary>
	[TestFixture]
	[NUnit.Framework.Category("NI.Ioc")]
	public class ServiceProviderTest
	{
		ComponentsConfig config;
		ServiceProvider serviceProvider;
		ApplicationContainer appContainer;

		public ServiceProviderTest()
		{
		}

		[SetUp]
		public void InitContainer() {
			config = createConfig();
			serviceProvider = new ServiceProvider(config);

			appContainer = new ApplicationContainer();
			appContainer.Add(serviceProvider);
		}
		
		/// <summary>
		/// Test:
		/// 1) ComponentsConfig parsing and validating
		/// 2) Components factory
		/// 3) Get service
		/// </summary>
		[Test]
		public void test_ServiceProvider() {
			
			// analyse: since only service provider itself is derived from Component, container contains only 1 instance
			Assert.AreEqual(1, appContainer.Components.Count, "Invalid component instances count");
			
			Component2 simple = serviceProvider.GetService("simple") as Component2;
			
			Assert.AreEqual( simple.Hehe.Length, 2, "Invalid initialization for 'simple.Hehe'");
			Assert.AreEqual( simple.Hehe[0], 1, "Invalid initialization for 'simple.Hehe[0]'");
			Assert.AreEqual( simple.Hehe[1], 2, "Invalid initialization for 'simple.Hehe[1]'");
			
			Component1 child = serviceProvider.GetService("child") as Component1;
			if (child.Dependency1==null || !(child.Dependency1 is Component2))
				throw new Exception("Invalid initialization for compontent 'child'");
			
			Component1 parent = serviceProvider.GetService("parent") as Component1;
			
			Assert.AreEqual( parent.Dependency1, child, "Invalid initialization for 'parent.Dependency1'");
			Assert.AreEqual( parent.PropInt, 6, "Invalid initialization for 'parent.PropInt'");
			Assert.AreEqual( parent.initCalled, true, "parent init not called");
			
			// get service
			simple = (serviceProvider as IServiceProvider).GetService( typeof(Component2) ) as Component2;
			if (simple==null)
				throw new Exception("Get Service fails");
			
		}

		[Test]
		public void test_NamedConstructorArgs() {
			var c3 = serviceProvider.GetService("testNamedConstructor") as Component3;
			Assert.AreEqual("John", c3.Name);
			Assert.AreEqual(5, c3.Age);
		}

		[Test]
		public void test_DelegateInjection() {
			var c4 = serviceProvider.GetService("testDelegateInjection") as Component4;
			Assert.AreEqual("1", c4.GetValStr());

			var c4suggested = serviceProvider.GetService("testDelegateSuggestedInjection") as Component4;
			Assert.AreEqual("1", c4suggested.GetValStr());

			c4suggested.InitValue();
			Assert.AreEqual(5, c4suggested.Val);
		}

		[Test]
		public void test_ConfigState() {
			// check
			int i = 0;
			foreach (IComponentInitInfo cInfo in config) {
				switch (cInfo.Name) {
					case "simple":
						if (cInfo.ConstructorArgs.Length != 2 ||
							cInfo.ComponentType != typeof(Component2) ||
							cInfo.Properties.Length != 0)
							throw new Exception("Invalid component info");
						break;
					case "child":
						if (cInfo.ConstructorArgs.Length != 1 ||
							cInfo.ComponentType != typeof(Component1) ||
							cInfo.Properties.Length != 0)
							throw new Exception("Invalid component info");
						break;
					case "parent":
						if (cInfo.ConstructorArgs.Length != 0 ||
							cInfo.ComponentType != typeof(Component1) ||
							cInfo.Properties.Length != 2)
							throw new Exception("Invalid component info");
						break;
				}

				i++;
			}
			//if (i!=3) throw new Exception("Invalid components number");

		}
		
		ComponentsConfig createConfig() {

			string xml_config = @"
				<components>
					<component name='simple_template_template'>
						<constructor-arg index='1'>
							<map>
								<entry key='IUserAccount'><value>accounts</value></entry>
								<entry key='ICmsPage'><value>pages</value></entry>
							</map>
						</constructor-arg>
					</component>
				
					<component name='simple_template' parent='simple_template_template'/>
				
					<component name='simple'
						parent='simple_template'
						type='NI.Tests.Ioc.Component2,NI.Tests' singleton='true'>
						<constructor-arg index='0'>
							<list>
								<entry><value>1</value></entry>
								<entry><value>2</value></entry>
							</list>
						</constructor-arg>
					</component>
				
					<component name='child' type='NI.Tests.Ioc.Component1,NI.Tests'  singleton='true'>
						<constructor-arg index='0'>
							<ref name='simple'/>
						</constructor-arg>
					</component>
					
					<component name='parent_template'
						type='NI.Tests.Ioc.Component1,NI.Tests' 
						init-method='init' singleton='false'>
						<property name='PropInt'>
							<value>6</value>
						</property>
					</component>
					
					<component
						name='parent'
						parent='parent_template'
						singleton='true'>
					
						<property name='Dependency1'>
							  <ref name='child'/>
						</property>
						
						
					</component>
					
					<component name='testNamedConstructor'
						type='NI.Tests.Ioc.Component3,NI.Tests'>
						<constructor-arg name='age'>
							<value>5</value>
						</constructor-arg>
						<constructor-arg name='name'>
							<value>John</value>
						</constructor-arg>
					</component>

					<component name='testDelegateInjection'
						type='NI.Tests.Ioc.Component4,NI.Tests'>
						<property name='Val'><value>1</value></property>
						<property name='FormatVal'>
							<component type='NI.Ioc.DelegateFactory' singleton='false'>
								<property name='TargetObject'><ref name='testNamedConstructor'/></property>
								<property name='TargetMethod'><value>FormatIntVal</value></property>
								<property name='DelegateType'><type>System.Func`2[[System.Int32,mscorlib],[System.String,mscorlib]],mscorlib</type></property>
							</component>
						</property>
					</component>

					<component name='testDelegateSuggestedInjection'
						type='NI.Tests.Ioc.Component4,NI.Tests'>
						<property name='Val'><value>1</value></property>
						<property name='FormatVal'>
							<component type='NI.Ioc.DelegateFactory' singleton='false'>
								<property name='TargetObject'><ref name='testNamedConstructor'/></property>
								<property name='TargetMethod'><value>FormatIntVal</value></property>
							</component>
						</property>

						<property name='InitVal'>
							<component type='NI.Ioc.DelegateFactory' singleton='false'>
								<property name='TargetObject'><ref name='testNamedConstructor'/></property>
								<property name='TargetMethod'><value>SetAgeToValue</value></property>
							</component>
						</property>
					</component>

				</components>
			";
			
			XmlComponentsConfig config = new XmlComponentsConfig(xml_config);

			return config;
		}
		
		
		
		
		
	}

	public class BaseComponent {

	}

	public class Component2 : BaseComponent {
		
		public int[] Hehe;
		public IDictionary Map;
		
		public Component2(int[] hehe, IDictionary map) {
			Hehe = hehe;
			Map = map;
		}
	}

	public class Component1 : BaseComponent {
		BaseComponent _Dependency1;
		int _PropInt;
		bool _initCalled = false;

		public BaseComponent Dependency1 {
			get { return _Dependency1; }
			set { _Dependency1 = value; }
		}
			
		public int PropInt {
			get { return _PropInt; }
			set { _PropInt = value; } 
		}
		
		public bool initCalled {
			get {
				return _initCalled;
			}
		}

		public Component1() { }
			
		public void init() {
			_initCalled = true;
		}

		public Component1(BaseComponent dependency1) {
			Dependency1 = dependency1;
		}
	}

	public class Component3 {
		public int Age;
		public string Name;

		public Component3(string name, int age) {
			Name = name;
			Age = age;
		}

		public string FormatIntVal(int i) {
			return i.ToString();
		}

		public void SetAgeToValue(Component4 c) {
			c.Val = Age;
		}
	}

	public class Component4 {
		public Func<int,string> FormatVal { get; set; }
		public int Val { get; set; }

		public Action<Component4> InitVal { get; set; }

		public string GetValStr() {
			return FormatVal(Val);
		}

		public void InitValue() {
			InitVal(this);
		}

	}
				
	
	
	
}
