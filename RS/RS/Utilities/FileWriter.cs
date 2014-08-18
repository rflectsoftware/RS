using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RS.Utilities
{
    public static class FileWriter
    {
        [AttributeUsage(AttributeTargets.Property)]
        public class HeaderTextAttribute : Attribute
        {
            public string HeaderText { get; set; }

            public HeaderTextAttribute(string HeaderText)
            {
                this.HeaderText = HeaderText;
            }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class IgnoredFieldAttribute : Attribute { }

        [AttributeUsage(AttributeTargets.Property)]
        public class ToStringFormatAttribute : Attribute
        {
            public string ToStringFormat { get; set; }

            public ToStringFormatAttribute(string ToStringFormat)
            {
                this.ToStringFormat = ToStringFormat;
            }
        }

        public static void WriteFile<T>(IEnumerable<T> OutputList, string OutputFileName = null, bool IncludeHeader = true, string ColumnDelimiter = "\t", string LineDelimiter = "\r\n", string TextQualifier = null)
        {
            if (OutputList.Count() < 1)
            {
                throw new Exception("Can't output an empty list!");
            }

            //If they didn't pass in an output file prompt them for that now and make sure it's valid before proceeding
            if (string.IsNullOrWhiteSpace(OutputFileName))
            {
                System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog();

                fileDialog.Title = "Output File Name Required";
                fileDialog.AddExtension = true;
                fileDialog.DefaultExt = "txt";
                fileDialog.OverwritePrompt = true;

                if (fileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                OutputFileName = fileDialog.FileName;
            }

            //Prepare a streamwriter now that we know we're really outputting something...
            string tempFileName = Path.GetTempFileName();

            StringBuilder outputBuilder = new StringBuilder(5000000);

            using (StreamWriter SW = new StreamWriter(tempFileName))
            {
                int lineCounter = 0;

                //Build the official list of output properties
                List<PropertyInfo> validProperties = typeof(T).GetProperties().ToList();

                //Remove ignored properties
                validProperties.RemoveAll(x => x.GetFirstAttribute<IgnoredFieldAttribute>() != null);

                PropertyInfo[] properties = validProperties.ToArray();

                foreach (T outputItem in OutputList)
                {
                    lineCounter++;
                    string[] OutputValues = new string[properties.Length];

                    //If it's the first item, and they want a header in the output, build it now...
                    if (lineCounter == 1 && IncludeHeader)
                    {
                        if (IncludeHeader)
                        {
                            for (int ColumnIndex = 0; ColumnIndex < properties.Length; ColumnIndex++)
                            {
                                OutputValues[ColumnIndex] = properties[ColumnIndex].Name;

                                //See if this property has a custom header text defined...
                                HeaderTextAttribute headerText = properties[ColumnIndex].GetFirstAttribute<HeaderTextAttribute>();

                                if (headerText != null)
                                {
                                    OutputValues[ColumnIndex] = headerText.HeaderText;
                                }

                                if (TextQualifier != null) OutputValues[ColumnIndex] = TextQualifier + OutputValues[ColumnIndex] + TextQualifier;
                            }

                            outputBuilder.Append(string.Join(ColumnDelimiter, OutputValues) + LineDelimiter);
                        }
                    }

                    //Now loop through the properties for the current object being output
                    for (int columnIndex = 0; columnIndex < properties.Length; columnIndex++)
                    {
                        //See if this property has ToStringFormat attribute that needs applied to the value
                        ToStringFormatAttribute ToStringFormat = properties[columnIndex].GetFirstAttribute<ToStringFormatAttribute>();

                        object BaseOutputValue = properties[columnIndex].GetValue(outputItem, null);

                        if (BaseOutputValue == null)
                        {
                            OutputValues[columnIndex] = null;
                        }
                        else
                        {
                            if (ToStringFormat == null)
                            {
                                OutputValues[columnIndex] = BaseOutputValue.ToString();
                            }
                            else
                            {
                                MethodInfo ToStringCall = properties[columnIndex].PropertyType.GetMethod("ToString", new Type[] { typeof(string) });

                                OutputValues[columnIndex] = ToStringCall.Invoke(BaseOutputValue, new object[] { ToStringFormat.ToStringFormat }).ToString();
                            }
                        }

                        if (TextQualifier != null) OutputValues[columnIndex] = TextQualifier + OutputValues[columnIndex] + TextQualifier;

                    }

                    outputBuilder.Append(string.Join(ColumnDelimiter, OutputValues) + LineDelimiter);

                    //Use a max of 50 MB of memory before writing to the stream and clearing out
                    if (outputBuilder.Length > 50000000)
                    {
                        SW.Write(outputBuilder);

                        //Assume if the file was 50 MB already, there might be 50 MB more to go
                        outputBuilder = new StringBuilder(50000000);
                    }
                }

                //Write the last block
                SW.Write(outputBuilder);
            }

            //At this point the temp file is built and populated with all the data
            if (File.Exists(OutputFileName))
            {
                File.Delete(OutputFileName);
            }

            File.Move(tempFileName, OutputFileName);
        }

        public static void WriteFile(System.Data.DataTable DT, string OutputFileName = null, bool IncludeHeader = true, string ColumnDelimiter = "\t", string LineDelimiter = "\r\n", string TextQualifier = null)
        {
            if (DT.Rows.Count < 1)
            {
                throw new Exception("Can't output an empty table!");
            }

            //If they didn't pass in an output file prompt them for that now and make sure it's valid before proceeding
            if (string.IsNullOrWhiteSpace(OutputFileName))
            {
                System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog();
                fileDialog.Title = "Output File Name Required";
                fileDialog.AddExtension = true;
                fileDialog.DefaultExt = "txt";
                fileDialog.OverwritePrompt = true;

                if (fileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                OutputFileName = fileDialog.FileName;
            }

            //Prepare a streamwriter now that we know we're really outputting something...
            string tempFileName = Path.GetTempFileName();

            StringBuilder outputBuilder = new StringBuilder(5000000);

            using (StreamWriter SW = new StreamWriter(tempFileName))
            {
                int lineCounter = 0;

                foreach (System.Data.DataRow OutputItem in DT.Rows)
                {
                    lineCounter++;

                    string[] outputValues = new string[DT.Columns.Count];

                    //If it's the first item, and they want a header in the output, build it now...
                    if (lineCounter == 1 && IncludeHeader)
                    {
                        if (IncludeHeader)
                        {
                            for (int ColumnIndex = 0; ColumnIndex < DT.Columns.Count; ColumnIndex++)
                            {
                                outputValues[ColumnIndex] = DT.Columns[ColumnIndex].ColumnName;

                                if (TextQualifier != null)
                                {
                                    outputValues[ColumnIndex] = TextQualifier + outputValues[ColumnIndex] + TextQualifier;
                                }
                            }

                            outputBuilder.Append(string.Join(ColumnDelimiter, outputValues) + LineDelimiter);
                        }
                    }

                    //Now loop through the properties for the current object being output
                    for (int columnIndex = 0; columnIndex < DT.Columns.Count; columnIndex++)
                    {
                        object BaseOutputValue = OutputItem[columnIndex];

                        if (BaseOutputValue == null || BaseOutputValue == DBNull.Value)
                        {
                            outputValues[columnIndex] = null;
                        }
                        else
                        {
                            outputValues[columnIndex] = BaseOutputValue.ToString();
                        }

                        if (TextQualifier != null) outputValues[columnIndex] = TextQualifier + outputValues[columnIndex] + TextQualifier;
                    }

                    outputBuilder.Append(string.Join(ColumnDelimiter, outputValues) + LineDelimiter);

                    //Use a max of 50 MB of memory before writing to the stream and clearing out
                    if (outputBuilder.Length > 50000000)
                    {
                        SW.Write(outputBuilder);

                        //Assume if the file was 50 MB already, there might be 50 MB more to go
                        outputBuilder = new StringBuilder(50000000);
                    }
                }

                //Write the last block
                SW.Write(outputBuilder);
            }

            //At this point the temp file is built and populated with all the data
            if (File.Exists(OutputFileName))
            {
                File.Delete(OutputFileName);
            }

            File.Move(tempFileName, OutputFileName);
        }
    }
}
