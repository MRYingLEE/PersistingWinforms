
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

