// This is SLIGHTLY MODIFIED simple sample from MS

using System;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;

namespace SmartData.Persistent
{
    public class NameCreationService : System.ComponentModel.Design.Serialization.INameCreationService
    {
        public NameCreationService()
        {
        }

        public static int globalID = 0;

        // Creates an identifier for a particular data type that does not conflict  
        // with the identifiers of any components in the specified collection. 
        public string CreateName(System.ComponentModel.IContainer container, System.Type dataType)
        {
            // Create a basic type name string. 
            string baseName = dataType.Name;


            if (container == null)  // It is strange sometimes container is null           
            {
                globalID++;
                return baseName + globalID + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");  // Just in case
            }

            int uniqueID = 1;

            bool unique = false;
            // Continue to increment uniqueID numeral until a  
            // unique ID is located. 

            while (!unique)
            {
                unique = true;
                // Check each component in the container for a matching  
                // base type name and unique ID. 
                for (int i = 0; i < container.Components.Count; i++)
                {
                    // Check component name for match with unique ID string. 
                    if (container.Components[i].Site.Name.StartsWith(baseName + uniqueID.ToString()))
                    {
                        // If a match is encountered, set flag to recycle  
                        // collection, increment ID numeral, and restart.
                        unique = false;
                        uniqueID++;
                        break;
                    }
                }

                #region I added for avoid name conflicts with existing fields/ Properties of the component

                // Check component name for match with unique ID string. 
                if ((container.GetType().GetField(baseName + uniqueID.ToString()) != null) || (container.GetType().GetProperty(baseName + uniqueID.ToString()) != null))
                {
                    // If a match is encountered, set flag to recycle  
                    // collection, increment ID numeral, and restart.
                    unique = false;
                    uniqueID++;
                    break;
                }

                #endregion

            }// end of while
            return baseName + uniqueID.ToString();
        }


        // Returns whether the specified name contains  
        // all valid character types. 
        public bool IsValidName(string name)
        {
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                UnicodeCategory uc = Char.GetUnicodeCategory(ch);
                switch (uc)
                {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        // Throws an exception if the specified name does not contain  
        // all valid character types. 
        public void ValidateName(string name)
        {
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                UnicodeCategory uc = Char.GetUnicodeCategory(ch);
                switch (uc)
                {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:
                    case UnicodeCategory.ConnectorPunctuation:
                        break;
                    default:
                        throw new Exception("The name '" + name + "' is not a valid identifier.");
                }
            }
        }
    }
}

