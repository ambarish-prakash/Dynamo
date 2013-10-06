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
        private string code = "Your Code Goes Here";
        private List<Statement> codeStatements = new List<Statement>();


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

            if (Code.Equals("")) //If its null then remove all the ports
            {
                SetPorts();
                return;
            }

            //Parse the text and assign each AST node to a statement instance
            Dictionary<int, List<GraphToDSCompiler.VariableLine>> unboundIdentifiers;
            unboundIdentifiers = new Dictionary<int, List<GraphToDSCompiler.VariableLine>>();
            List<ProtoCore.AST.Node> resultNode = new List<Node>();
            List<string> compiledCode;
            GraphToDSCompiler.GraphUtilities.CompileExpression(Code, out compiledCode);
            for(int i=0;i<compiledCode.Count;i++)
            {
                string singleExpression = compiledCode[i];
                string fakeVariableName = "temp" + this.GUID.ToString().Remove(7);
                singleExpression = singleExpression.Replace("%t", fakeVariableName);
                singleExpression = singleExpression.Replace("\r", "");
                singleExpression = singleExpression.Replace("\n", "");
                List<ProtoCore.AST.Node> singleNode;
                if (!GraphToDSCompiler.GraphUtilities.ParseCodeBlockNodeStatements(singleExpression, unboundIdentifiers, out singleNode))
                    throw new Exception();
                resultNode.Add(singleNode[0]);
            }

            foreach (Node node in resultNode)
            {
                Statement tempStatement;
                {
                    tempStatement = Statement.CreateInstance(node, this.GUID);
                }
                codeStatements.Add(tempStatement);
            }

            SetPorts(); //Set the input and output ports based on the 
        }

        private void SetPorts()
        {
            InPortData.Clear();
            OutPortData.Clear();
            if (codeStatements.Count == 0 || codeStatements == null)
            {
                RegisterAllPorts();
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
            List<string> uniqueInputs = new List<string>();
            foreach (var singleStatement in codeStatements)
            {
                List<string> inputNames = singleStatement.GetReferencedVariableNames();
                foreach (string name in inputNames)
                {
                    if (!uniqueInputs.Contains(name))
                        uniqueInputs.Add(name);
                }
            }
            foreach(string name in uniqueInputs)
                InPortData.Add(new PortData(name, "Output", typeof(object)));
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
            None,
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
        public static Statement CreateInstance(Node astNode, Guid nodeGuid)
        {
            if (astNode == null)
                throw new ArgumentNullException();

            return new Statement(astNode,nodeGuid);
        }

        //As of now only works with functionalcall nodes
        //NOT TESTED WITH THIS COMMIT
        public static void GetReferencedVariables(Node astNode , List<Variable> refVariableList)
        {
            //DFS Search to find all identifier nodes
            if (astNode == null)
                return ;
            if (astNode is FunctionCallNode) 
            {
                FunctionCallNode currentNode = astNode as FunctionCallNode;
                foreach (var node in currentNode.FormalArguments)
                {
                    GetReferencedVariables(node, refVariableList);
                }
                
            }
            else if (astNode is IdentifierNode)
            {
                Variable resultVariable = new Variable(astNode as IdentifierNode);
                refVariableList.Add(new Variable(astNode as IdentifierNode));
            }
            else if (astNode is ExprListNode)
            {
                ExprListNode currentNode = astNode as ExprListNode;
                foreach (var node in currentNode.list)
                {
                    GetReferencedVariables(node, refVariableList);
                }
            }
            else
            {
                //Its could be something like a literal
                //Or node not completely implemented YET
                return;
            }
        }

        public List<string> GetReferencedVariableNames()
        {
            List<string> names = new List<string>();
            foreach (Variable refVar in referencedVariables)
                names.Add(refVar.Name);
            return names;
        }

        public static StatementType GetStatementType(Node astNode,Guid nodeGuid)
        {
            if (astNode is FunctionDefinitionNode)
                return StatementType.FuncDeclaration;
            if (astNode is BinaryExpressionNode)
            {
                BinaryExpressionNode currentNode = astNode as BinaryExpressionNode;
                string fakeVariableName = "temp" + nodeGuid.ToString().Remove(7);
                if (!(currentNode.LeftNode is IdentifierNode) || currentNode.Optr != ProtoCore.DSASM.Operator.assign)
                    throw new ArgumentException();
                if (!currentNode.LeftNode.Name.Equals(fakeVariableName))
                    return StatementType.Expression;
                if (currentNode.RightNode is IdentifierNode)
                    return StatementType.AssignmentVar;
                if (currentNode.RightNode is ExprListNode)
                    return StatementType.Collection;
                if (currentNode.RightNode is DoubleNode || currentNode.RightNode is IntNode)
                    return StatementType.Literal;
                if (currentNode.RightNode is StringNode)
                    return StatementType.Literal;
            }
            return StatementType.None;
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
        private Statement(Node astNode, Guid nodeGuid)
        {
            StartLine = astNode.line;
            EndLine = astNode.endLine;
            CurrentType = GetStatementType(astNode,nodeGuid);

            if (astNode is BinaryExpressionNode)
            {
                BinaryExpressionNode binExprNode = astNode as BinaryExpressionNode;
                if (binExprNode.Optr != ProtoCore.DSASM.Operator.assign)
                    throw new ArgumentException("Binary Expr Node is not an assignment!");
                if (!(binExprNode.LeftNode is IdentifierNode))
                    throw new ArgumentException("LHS invalid");

                IdentifierNode assignedVar = binExprNode.LeftNode as IdentifierNode;
                string fakeVariableName = "temp" + nodeGuid.ToString().Remove(7);
                if (assignedVar.Name.Equals(fakeVariableName)) 
                {
                    AssignedVariable = new Variable(">",assignedVar.line);
                }
                else
                {
                    AssignedVariable = new Variable(assignedVar);
                }

                List<Variable> refVariableList = new List<Variable>();
                GetReferencedVariables(binExprNode.RightNode, refVariableList);
                referencedVariables = refVariableList;
            }
            else if (astNode is FunctionDefinitionNode)
            {
            }
            else
                throw new ArgumentException("Must be func def or assignment");

            Variable.SetCorrectColumn(referencedVariables, this.CurrentType);
        }
        #endregion
    }

    public class Variable
    {
        public int Row {get; private set;}
        public int StartColumn { get; private set; }
        public int EndColumn { get; private set; }
        public string Name { get; private set; }

        #region Private Methods
        private void MoveColumnBack()
        {
            StartColumn -= 13;
            EndColumn -= 13;
        }
        #endregion


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

        public Variable(string name, int line)
        {
            Name = name;
            Row = line;
            StartColumn = EndColumn = -1;
        }

        public static void SetCorrectColumn(List<Variable> refVar, Statement.StatementType type)
        {
            if (refVar == null)
                return;
            if (type != Statement.StatementType.Expression)
            {
                foreach (var singleVar in refVar)
                    singleVar.MoveColumnBack();
            }
        }
        #endregion
    }
}
