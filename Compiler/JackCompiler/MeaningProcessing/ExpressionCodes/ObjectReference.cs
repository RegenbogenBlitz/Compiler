namespace JackCompiler.MeaningProcessing.ExpressionCodes
{
    internal class ObjectReference
    {
        private enum ObjectRelation
        {
            ThisObject,
            ThatObject
        }

        private readonly ObjectRelation relation;
        private readonly VariableCode variableCode;

        private ObjectReference(ObjectRelation relation, VariableCode variableCode)
        {
            this.relation = relation;
            this.variableCode = variableCode;
        }

        internal static ObjectReference This()
        {
            return new ObjectReference(ObjectRelation.ThisObject, null);
        }

        internal static ObjectReference That(VariableCode variableCode)
        {
            return new ObjectReference(ObjectRelation.ThatObject, variableCode);
        }
        
        public string GetDescription()
        {
            if (this.relation == ObjectRelation.ThisObject)
            {
                return "this";
            }
            else
            {
                return this.variableCode.GetDescription();
            }
        }

        public string Compile()
        {
            if (this.relation == ObjectRelation.ThisObject)
            {
                return VmWriterHelper.WritePush(VmSegmentType.PointerType, 0);
            }
            else
            {
                return this.variableCode.Compile();
            }
        }
    }
}
