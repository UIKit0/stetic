using System;

namespace Stetic {

	public enum ObjectWrapperType {
		Object,
		Widget,
		Container,
		Window
	};

	public class ObjectWrapperAttribute : Attribute {
		string name, iconName;
		ObjectWrapperType type;

		public string Name {
			get { return name; }
			set { name = value; }
		}

		public string IconName {
			get { return iconName; }
			set { iconName = value; }
		}

		public ObjectWrapperType Type {
			get { return type; }
			set { type = value; }
		}

		public ObjectWrapperAttribute (string name, string iconName, ObjectWrapperType type)
		{
			this.name = name;
			this.iconName = iconName;
			this.type = type;
		}
	}

}
