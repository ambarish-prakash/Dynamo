using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoCore.AST;
using ProtoCore.AST.AssociativeAST;

namespace Dynamo.Nodes
{
    //NOT TESTED
    class Statement
    {
        public int StartLine { get; private set; }
        public int EndLine { get; private set; }
        public Variable AssignedVariable { get; private set; }
        public List<Variable> ReferencedVariables { get; private set; }

        public enum State { Normal, Warning, Error }
        public State CurrentState { get; private set; }

        public enum StatementType { Expression, Literal, Collection, AssignmentVar, FuncDeclaration }
        public StatementType CurrentType { get; private set; }

        public List<Statement> SubStatements { get; private set; }

        public Statement(Node astNode)
        {
            if (astNode == null)
                return;

            StartLine = astNode.line;
            EndLine = astNode.endLine;
            
            if (astNode is ProtoCore.AST.AssociativeAST.BinaryExpressionNode )
            {
                BinaryExpressionNode bENode = astNode as BinaryExpressionNode;
                if (bENode.Optr == ProtoCore.DSASM.Operator.assign)
                {
                    if (bENode.LeftNode != null && bENode.LeftNode is IdentifierNode)
                        AssignedVariable = new Variable(bENode.LeftNode as IdentifierNode);
                    if (bENode.RightNode == null)
                    {
                        ReferencedVariables = null;
                        CurrentType = StatementType.AssignmentVar;
                    }
                    else
                    {
                        CurrentType = StatementType.Expression;
                        ReferencedVariables = GetReferencedVariables(bENode.RightNode);
                    }

                }
            }
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
                resultLeft= GetReferencedVariables((astNode as BinaryExpressionNode).LeftNode);
                resultList = GetReferencedVariables((astNode as BinaryExpressionNode).RightNode);
                foreach (Variable var in resultLeft)
                {
                    if (resultList.Where(x => x.Name.Equals(var.Name)).Count() == 0)
                    { resultList.Add(var);
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
    }

    //TESTED - INITIALIZES FINE
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
