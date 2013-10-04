using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoCore.AST;
using ProtoCore.AST.AssociativeAST;
using Dynamo.Models;

namespace Dynamo.Nodes
{
    [NodeName("Code Block")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Allows for code to be wriiten")] //<--Change the descp :|
    public partial class DynamoCodeBlockNode : NodeModel
    {
        private string code;
        private List<Statement> codeStatements;
        private List<Variable> referencedVariables;

        public DynamoCodeBlockNode()
        {
            codeStatements = new List<Statement>();
            referencedVariables = new List<Variable>();
            code = "Your Code Goes Here";
        }

        #region Properties
        public string Code
        {
            get
            {
                return code;
            }

            set
            {
                if (code == null || !code.Equals(value))
                {
                    code = value;
                    if (value != null)
                    {
                        DisableReporting();
                        ProcessCode();
                        RaisePropertyChanged("Code");
                        RequiresRecalc = true;
                        EnableReporting();
                        if (WorkSpace != null)
                            WorkSpace.Modified();
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private void ProcessCode()
        {
            //New code : Revamp everything
            codeStatements.Clear(); 
            referencedVariables.Clear();

            if (Code.Equals("")) //If its null then remove all the ports
            {
                SetPorts();
                return;
            }

            //Parse the text and assign each AST node to a statement instance
            Dictionary<int, List<GraphToDSCompiler.VariableLine>> unboundIdentifiers;
            unboundIdentifiers = new Dictionary<int, List<GraphToDSCompiler.VariableLine>>();
            List<ProtoCore.AST.Node> resultNode;
            if (!GraphToDSCompiler.GraphUtilities.ParseCodeBlockNodeStatements(Code, unboundIdentifiers, out resultNode))
                throw new Exception();
            //int statementNumber = 1;
            foreach (Node node in resultNode)
            {
                Statement tempStatement;
                //if (node is BinaryExpressionNode)
                //{
                //    tempStatement = Statement.CreateInstance(node, unboundIdentifiers[statementNumber]);
                //    statementNumber++;
                //}
                //else
                {
                    tempStatement = Statement.CreateInstance(node);
                }
                codeStatements.Add(tempStatement);
            }

            foreach (var entry in unboundIdentifiers)
            {
                foreach (GraphToDSCompiler.VariableLine varLine in entry.Value)
                {
                    referencedVariables.Add(new Variable(varLine));
                }
            }

            SetPorts(); //Set the input and output ports based on the 
        }

        private void SetPorts()
        {
            InPortData.Clear();
            OutPortData.Clear();
            if (codeStatements.Count == 0 || codeStatements == null)
            {
                return;
            }

            SetInputPorts();
            SetOutputPorts();

            RegisterAllPorts();
        }

        private void SetOutputPorts()
        {
            foreach (Statement s in codeStatements)
            {
                if (s.AssignedVariable != null)
                    OutPortData.Add(new PortData(s.AssignedVariable.Name, "Output", typeof(object)));
            }
        }

        private void SetInputPorts()
        {
            foreach(var refVariable in referencedVariables)
            {
                InPortData.Add(new PortData(refVariable.Name, "Input", typeof(object)));
            }
        }
        #endregion

    }

    //NOT TESTED
    public class Statement
    {
        #region Enums
        public enum State 
        { 
            Normal, 
            Warning, 
            Error 
        }
        public enum StatementType 
        { 
            Expression, 
            Literal, 
            Collection, 
            AssignmentVar, 
            FuncDeclaration 
        }
        #endregion

        private List<Variable> referencedVariables = new List<Variable>();
        private List<Statement> subStatements = new List<Statement>();

        #region Public Methods
        public static Statement CreateInstance(Node astNode)
        {
            if (astNode == null)
                throw new ArgumentNullException();

            return new Statement(astNode);
        }

#if false
        public static Statement CreateInstance(Node astNode, List<GraphToDSCompiler.VariableLine> varLine)
        {
            if (astNode == null)
                throw new ArgumentNullException();

            return new Statement(astNode,varLine);
        }
#endif

        //NOT TESTED WITH THIS COMMIT --- WILL BE CHNAGED TO USE EXISTING APIs
        public static List<Variable> GetReferencedVariables(Node astNode)
        {
            //DFS Search to find all identifier nodes
            if (astNode == null)
                return new List<Variable>();
            List<Variable> resultList = new List<Variable>();
            if (astNode is BinaryExpressionNode) // As of now only implemented for BEN, Will now look into other APIs and change it to universal
            {
                List<Variable> resultLeft;
                resultLeft = GetReferencedVariables((astNode as BinaryExpressionNode).LeftNode);
                resultList = GetReferencedVariables((astNode as BinaryExpressionNode).RightNode);
                foreach (Variable var in resultLeft)
                {
                    if (resultList.Where(x => x.Name.Equals(var.Name)).Count() == 0)
                    {
                        resultList.Add(var);
                    }
                }
            }
            if (astNode is IdentifierNode)
            {
                Variable resultVariable = new Variable(astNode as IdentifierNode);
                resultList.Add(resultVariable);
            }
            return resultList;
        }
        #endregion

        #region Properties
        public int StartLine { get; private set; }
        public int EndLine { get; private set; }
        
        public Variable AssignedVariable { get; private set; }
        
        public State CurrentState { get; private set; }
        public StatementType CurrentType { get; private set; }
        
        #endregion

        #region Private Methods
        private Statement(Node astNode)
        {
            StartLine = astNode.line;
            EndLine = astNode.endLine;

            if (astNode is BinaryExpressionNode)
            {
                BinaryExpressionNode binExprNode = astNode as BinaryExpressionNode;
            }
            else if (astNode is IdentifierNode)
            {

            }
        }
#if false
        private Statement(Node astNode, List<GraphToDSCompiler.VariableLine> refVarList)
        {
            ReferencedVariables = new List<Variable>();

            Variable tempVar;
            foreach (var varLine in refVarList)
            {
                tempVar = new Variable(varLine);
                ReferencedVariables.Add(tempVar);
            }

            StartLine = astNode.line;
            EndLine = astNode.endLine;

            if (astNode is ProtoCore.AST.AssociativeAST.BinaryExpressionNode)
            {
                BinaryExpressionNode binExprNode = astNode as BinaryExpressionNode;
                if (binExprNode.Optr == ProtoCore.DSASM.Operator.assign)
                {
                    if (binExprNode.LeftNode != null && binExprNode.LeftNode is IdentifierNode)
                    {
                        if (!binExprNode.LeftNode.Name.Equals("return"))
                            AssignedVariable = new Variable(binExprNode.LeftNode as IdentifierNode);
                    }
                    else
                    {
                        throw new ArgumentException();
                    }

                    //if (binExprNode.RightNode == null)
                    //{
                    //    throw new ArgumentNullException();
                    //}
                    //else
                    //{
                    //    CurrentType = StatementType.Expression;
                    //    ReferencedVariables = GetReferencedVariables(binExprNode.RightNode);
                    //}

                }
            }
            else if (astNode is IdentifierNode)
            {
                CurrentType = StatementType.AssignmentVar;
            }
            else if (astNode is ExprListNode)
            {
                CurrentType = StatementType.Collection;
            }
            else if (astNode is FunctionDefinitionNode)
            {
                FunctionDefinitionNode funcNode = astNode as FunctionDefinitionNode;
                CodeBlockNode funcBody = funcNode.FunctionBody;
                foreach (Node node in funcBody.Body)
                {
                    Statement tempStatement = new Statement(node);
                    SubStatements.Add(tempStatement);
                }
            }

        }
#endif
        #endregion
    }

    public class Variable
    {
        public int Row {get; private set;}
        public int StartColumn { get; private set; }
        public int EndColumn { get; private set; }
        public string Name { get; private set; }

        #region Public Methods
        public Variable(IdentifierNode identNode)
        {
            if (identNode == null)
                throw new ArgumentNullException();
            Name = identNode.Value;
            Row = identNode.line;
            StartColumn = identNode.col;
            EndColumn = identNode.endCol;
        }

        public Variable(GraphToDSCompiler.VariableLine varLine)
        {
            Name = varLine.variable;
            Row = varLine.line;
            StartColumn = varLine.column;
            EndColumn = StartColumn + Name.Length;
        }
        #endregion
    }
}
