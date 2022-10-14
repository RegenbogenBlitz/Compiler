namespace JackCompiler
{
    using System;
    using System.Collections.Generic;

    internal static class VmWriterHelper
    {
        private static Dictionary<string, int> labelUniqueIdentifiers;

        internal static string WritePush(VmSegmentType segmentType, int index)
        {
            if (index < 0)
            {
                throw new ArgumentException("index must be larger than 0");
            }

            var segmentText = GetSegmentText(segmentType);

            return "push " + segmentText + " " + index + Environment.NewLine;
        }

        internal static string WritePop(VmSegmentType segmentType, int index)
        {
            if (index < 0)
            {
                throw new ArgumentException("index must be larger than 0");
            } 
            
            var segmentText = GetSegmentText(segmentType);

            return "pop " + segmentText + " " + index + Environment.NewLine;
        }

        private static string GetSegmentText(VmSegmentType segmentType)
        {
            switch (segmentType)
            {
                case VmSegmentType.ArgumentType:
                    return "argument";

                case VmSegmentType.LocalType:
                    return "local";

                case VmSegmentType.StaticType:
                    return "static";

                case VmSegmentType.ConstantType:
                    return "constant";

                case VmSegmentType.ThisType:
                    return "this";

                case VmSegmentType.ThatType:
                    return "that";

                case VmSegmentType.PointerType:
                    return "pointer";

                case VmSegmentType.TempType:
                    return "temp";

                default:
                    throw new InvalidOperationException("Enum value not defined");
            }
        }

        internal static string WriteArithmetic(VmArithmeticType arithmeticType)
        {
            switch (arithmeticType)
            {
                case VmArithmeticType.AddType:
                    return "add" + Environment.NewLine;
                    
                case VmArithmeticType.SubtractType:
                    return "sub" + Environment.NewLine;

                case VmArithmeticType.NegateType:
                    return "neg" + Environment.NewLine;

                case VmArithmeticType.EqualityType:
                    return "eq" + Environment.NewLine;

                case VmArithmeticType.GreaterThanType:
                    return "gt" + Environment.NewLine;

                case VmArithmeticType.LessThanType:
                    return "lt" + Environment.NewLine;

                case VmArithmeticType.AndType:
                    return "and" + Environment.NewLine;

                case VmArithmeticType.OrType:
                    return "or" + Environment.NewLine;

                case VmArithmeticType.NotType:
                    return "not" + Environment.NewLine;

                default:
                    throw new InvalidOperationException("Enum value not defined");
            }
        }

        internal static string WriteLabel(string labelName, out string uniqueLabel)
        {
            if (labelUniqueIdentifiers == null)
            {
                throw new InvalidOperationException("This can only be called in a function");
            }

            if (!labelUniqueIdentifiers.ContainsKey(labelName))
            {
                labelUniqueIdentifiers.Add(labelName, 0);
            }

            uniqueLabel = labelName + labelUniqueIdentifiers[labelName];
            labelUniqueIdentifiers[labelName]++;

            return "label " + uniqueLabel + Environment.NewLine;
        }

        public static string WriteGoto(string uniqueLabel)
        {
            return "goto " + uniqueLabel + Environment.NewLine;
        }

        internal static string WriteIf(string uniqueLabel)
        {
            return "if-goto " + uniqueLabel + Environment.NewLine;
        }

        internal static string WriteCall(string ownerClassName, string subRoutineName, int numOfParameters)
        {
            if (numOfParameters < 0)
            {
                throw new ArgumentException("numOfParameters must be larger than 0");
            } 

            return "call " + ownerClassName + "." + subRoutineName + " " + numOfParameters + Environment.NewLine;
        }

        internal static string WriteFunction(string ownerClassName, string subRoutineName, int numOfLocals)
        {
            labelUniqueIdentifiers = new Dictionary<string, int>();

            if (numOfLocals < 0)
            {
                throw new ArgumentException("numOfLocals must be larger than 0");
            } 
            
            return "function " + ownerClassName + "." + subRoutineName + " " + numOfLocals + Environment.NewLine;
        }

        internal static string WriteReturn()
        {
            return "return" + Environment.NewLine;
        }
    }
}
