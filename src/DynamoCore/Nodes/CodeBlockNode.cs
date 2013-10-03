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
    public class DynamoCodeBlockNode : NodeModel
    {

    }


    //NOT TESTED
    public class Statement
    {
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

        #region Public Methods
        public static Statement CreateInstance(Node astNode)
        {
            if (astNode == null)
                throw new ArgumentNullException();

            return new Statement(astNode);
        }

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
        public List<Variable> ReferencedVariables { get; private set; }
        
        public State CurrentState { get; private set; }
        public StatementType CurrentType { get; private set; }
        
        public List<Statement> SubStatements { get; private set; }
        #endregion

        #region Private Methods
        private Statement(Node astNode)
        {
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

                    if (binExprNode.RightNode == null)
                    {
                        throw new ArgumentNullException();
                    }
                    else
                    {
                        CurrentType = StatementType.Expression;
                        ReferencedVariables = GetReferencedVariables(binExprNode.RightNode);
                    }

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
        #endregion
    }

    public class Variable
    {
        public int Row {get; private set;}
        public int StartColumn { get; private set; }
        public int EndColumn { get; private set; }
        public string Name { get; private set; }

        public Variable(IdentifierNode identNode)
        {
            if (identNode == null)
                throw new ArgumentNullException();
            Name = identNode.Value;
            Row = identNode.line;
            StartColumn = identNode.col;
            EndColumn = identNode.endCol;
        }
    }
}
