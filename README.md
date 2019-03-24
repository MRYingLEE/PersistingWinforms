# Introduction
It is easy to persist a component if it supports serializing. But unfortunly not all components do so. In such a situation, if you want to persist a component, you must write a lot of ugly code to save/restore its properties.

A lot of people tried to provide a general solution but failed for traditional serialization methods are not powerful to deal with complicated components.

Here, I will provide a general and robust way to persist a component by serializing it to C# code via System.ComponentModel instead of a data stream. But in order to generate C# code, the component has to be in design mode, so there could be some limits.

# Background

Initially, my idea was inspired by http://www.cnblogs.com/VisualStudioDesigner/archive/2010/08/20/1804438.html . This article reminded me that a component in design mode can generate C# code via System.ComponentModel (https://msdn.microsoft.com/en-us/library/system.componentmodel(v=vs.110).aspx ).

# Using the code
The coding with System.ComponentModel is not easy.  After days coding and debuging, I developed a class to simply the procedure to persist a Winforms component to C# code.  The core source code is as the following:

```csharp
using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace SmartData.Persistent
{
    public partial class ComponentDesigner
    {
        private IServiceProvider _serviceProvider;

        private IDesignerHost DesignerHost
        {
            get
            {
                return this._serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
            }
        }

        DesignSurfaceManager surfaceManager;
        DesignSurface surface;

        private IComponent componentInDesign = null;
        private Type originalComponentType = null;

        public IComponent ComponentInDesign
        {
            get
            { return componentInDesign; }
        }

        public static string InitializeComponentMethodName = "InitializeComponent";
        public static string InvokeInitMethod = InitializeComponentMethodName; //"InitializeComponent4Persistent"; // Other name instead of "InitializeComponent" has no magic to form the code.

        public static string DefaultOutputFolder;
        public ComponentDesigner(Type componentType)
        {
            originalComponentType = componentType;

            surfaceManager = new DesignSurfaceManager();
            surface = surfaceManager.CreateDesignSurface();

            surface.BeginLoad(componentType);

            this._serviceProvider = surface;
            componentInDesign = DesignerHost.RootComponent;

            NameCreationService _nameCreationService = new NameCreationService();
            if (_nameCreationService != null)
            {
                this.DesignerHost.RemoveService(typeof(INameCreationService), false);
                this.DesignerHost.AddService(typeof(INameCreationService), _nameCreationService);
            }

            try
            {
                MethodInfo m = componentInDesign.GetType().GetMethod(InvokeInitMethod);

                if (m != null)
                    m.Invoke(componentInDesign, new object[] { });

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        // I tried to load a component directly, but no findings, maybe not possible for in design surface, a component is always in design mode.
        // I need ILSpy to disassembly System.Design at first 2015.3.19;

        public static string postfix = "_LiYing";

        //将DesignerHost中的Component转成CodeType
        private CodeTypeDeclaration ConvertComponentToCodeType()
        {
            //componentInDesign = this.LoadComponent(componentInDesign);
            DesignerSerializationManager manager = new DesignerSerializationManager(this._serviceProvider);
            //这句Code是必须的，必须要有一个session，DesignerSerializationManager才能工作
            IDisposable session = manager.CreateSession();

            TypeCodeDomSerializer serializer = manager.GetSerializer(componentInDesign.GetType(), typeof(TypeCodeDomSerializer)) as TypeCodeDomSerializer;
            List<object> list = new List<object>();
            foreach (IComponent item in this.DesignerHost.Container.Components)
            {
                //PropertyInfo vName = item.GetType().GetProperty("Name");

                //if (vName != null)
                //{
                //    string realName = vName.GetValue(item) as string;

                //    if (realName == "")
                //    {
                //        vName.SetValue(item, "RENAMED");
                //    }
                //}

                list.Add(item);
            }
            CodeTypeDeclaration declaration = serializer.Serialize(manager, componentInDesign, list);
            session.Dispose();
            return declaration;
        }

        public string OriginalCode()
        {
            CodeTypeDeclaration componentType = this.ConvertComponentToCodeType();
            componentType.Name = this.originalComponentType.Name + postfix;

            StringBuilder bulder = new StringBuilder();
            StringWriter writer = new StringWriter(bulder, CultureInfo.InvariantCulture);
            CodeGeneratorOptions option = new CodeGeneratorOptions();
            option.BracingStyle = "C";
            option.BlankLinesBetweenMembers = false;

            CSharpCodeProvider codeDomProvider = new CSharpCodeProvider();

            codeDomProvider.GenerateCodeFromType(componentType, writer, option);

            string originalCode = bulder.ToString();

            return originalCode;
        }

        public bool IsUsedField(string code, string fieldName)
        {
            string extFieldName = "this." + fieldName;

            if ((code.Contains(" " + extFieldName + ".")) || code.Contains(" " + extFieldName + ";"))
                return true;

            int pos = 0;
            int newPos = 0;

            while (pos >= 0)
            {
                newPos = code.IndexOf(extFieldName, pos);

                if (newPos < 0)
                    return false;

                if (!(char.IsLetterOrDigit(code[newPos - 1])) && !(char.IsLetterOrDigit(code[newPos + (extFieldName).Length])))
                    return true;
                else
                    newPos = newPos + extFieldName.Length;

                pos = newPos;
            }

            return true;
        }

        public Type UserComponentType
        {
            get
            {
                Type realType = originalComponentType;

                while (realType.FullName.EndsWith(ComponentDesigner.postfix))  //In case nested define.
                {
                    realType = realType.BaseType;
                }

                return realType;
            }
        }

    }
}
```

A demo is attached to show how to use this class:

![DEMO](https://github.com/MRYingLEE/PersistingWinforms/blob/master/Component2Code.JPG "DEMO")


1. You have to create the component in design mode according to the component type

```csharp
private System.Windows.Forms.Button buttonInDESIGNmode;

private static ComponentDesigner designer;

private void Form1_Load(object sender, EventArgs e)
{
    designer = new ComponentDesigner(typeof(Button));

    buttonInDESIGNmode = designer.ComponentInDesign as System.Windows.Forms.Button;
}
``` 

2. You may change the properties of the component in design mode at your will

```csharp
private void button1_Click(object sender, EventArgs e)
 {
     times++;
     buttonInDESIGNmode.Text = "Change Text! You have tried " + times.ToString() + " times.";

     button1.Text = buttonInDESIGNmode.Text;

     richTextBox1.Text = designer.OriginalCode();

 }

 private void button2_Click(object sender, EventArgs e)
 {
     buttonInDESIGNmode.Left++;

     button1.Left = buttonInDESIGNmode.Left;

     richTextBox1.Text = designer.OriginalCode();
 }
```
 
And you may use System.Windows.Forms.PropertyGrid to change the properties of the component.

3. At ANY time, you may get the code to generate the component

```csharp
richTextBox1.Text = designer.OriginalCode();
``` 

An example of generated C# code is as the following:

```csharp

public class Button_LiYing : System.Windows.Forms.Button
{
    private Button_LiYing()
    {
        this.InitializeComponent();
    }
    private void InitializeComponent()
    {
        this.SuspendLayout();
        // 
        // 
        // 
        this.AllowDrop = true;
        this.ForeColor = System.Drawing.Color.Red;
        this.Location = new System.Drawing.Point(1, 0);
        this.Text = "Change Text! You have tried 1 times.";
        this.ResumeLayout(false);
    }
}
```

# Points of Interest
Of course, we may go further. In a big software I am working, SmartData: Excel for Enterprise Data, (http://smartdatahk.blogspot.hk/), I do the following:

1. To modify the generated code to suitable for my special need.

2. To compile the source code to assembly (DLL) at runtime.

3. To load the generated assembly and restore the original component when SmartData re-entries.

So that my core code can assume that some components are persistent even after rebooting.

# History

This is the first version. I tested in .Net 4.5. I think it works in other .Net versions also, but I didn't test.


# License
This article, along with any associated source code and files, is licensed under The Code Project Open License (CPOL)

