using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using ProtoCore;
using ProtoCore.AST.AssociativeAST;
using Dynamo;
using Dynamo.Nodes;


namespace Dynamo.Tests
{
    class CodeBlockNodeTests : DynamoUnitTest
    {
        [Test]
        public void TestVariableClass()
        {
            string code = "a;";
            CodeBlockNode commentNode;
            ProtoCore.AST.Node resultNode;
            resultNode = GraphToDSCompiler.GraphUtilities.Parse(code, out commentNode);
            IdentifierNode iNode = (resultNode as CodeBlockNode).Body[0] as IdentifierNode;
            Variable var1 = new Variable(iNode);
            Assert.AreEqual("a", var1.Name);
            Assert.AreEqual(1, var1.Row);
            Assert.AreEqual(1, var1.StartColumn);
            Assert.AreEqual(2, var1.EndColumn);
            iNode = null;
            Assert.Catch<ArgumentNullException>(delegate { Variable var2 = new Variable(iNode); });
        }

        [Test]
        public void TestStatement_FunctionDef()
        {
            string code = @"
def foo()
{
a = 5;
return = a;
}";
            Guid tempGuid = new Guid();
            CodeBlockNode commentNode;
            ProtoCore.AST.Node resultNode;
            List<string> topLevelRefVar = new List<string>();
            List<string> allRefVar = new List<string>();
            resultNode = GraphToDSCompiler.GraphUtilities.Parse(code, out commentNode);
            BinaryExpressionNode ben = (resultNode as CodeBlockNode).Body[0] as BinaryExpressionNode;
            Statement s1 = Statement.CreateInstance((resultNode as CodeBlockNode).Body[0], tempGuid);
            topLevelRefVar = Statement.GetReferencedVariableNames(s1,true);
            allRefVar = Statement.GetReferencedVariableNames(s1, false);
            Assert.AreEqual(2, s1.StartLine);
            Assert.AreEqual(6, s1.EndLine);
            Assert.AreEqual(null, s1.AssignedVariable);
            Assert.AreEqual(Statement.StatementType.FuncDeclaration, s1.CurrentType);
            Assert.AreEqual(0,topLevelRefVar.Count);
            Assert.AreEqual(1, allRefVar.Count);
            Assert.AreEqual("a", allRefVar[0]);

            code = @"def foo() = 10;";
            resultNode = GraphToDSCompiler.GraphUtilities.Parse(code, out commentNode);
            ben = (resultNode as CodeBlockNode).Body[0] as BinaryExpressionNode;
            s1 = Statement.CreateInstance((resultNode as CodeBlockNode).Body[0], tempGuid);
            topLevelRefVar = Statement.GetReferencedVariableNames(s1,true);
            allRefVar = Statement.GetReferencedVariableNames(s1, false);
            Assert.AreEqual(1, s1.StartLine);
            Assert.AreEqual(1, s1.EndLine);
            Assert.AreEqual(null, s1.AssignedVariable);
            Assert.AreEqual(Statement.StatementType.FuncDeclaration, s1.CurrentType);
            Assert.AreEqual(0, topLevelRefVar.Count);
            Assert.AreEqual(0, allRefVar.Count);
        }

        [Test]
        public void TestStatement_InlineExpression()
        {
            string code = @"
a = 
b>c+5 ? 
d-2 : 
e+f ;";
            Guid tempGuid = new Guid();
            CodeBlockNode commentNode;
            ProtoCore.AST.Node resultNode;
            List<string> refVarNames = new List<string>();
            resultNode = GraphToDSCompiler.GraphUtilities.Parse(code, out commentNode);
            BinaryExpressionNode ben = (resultNode as CodeBlockNode).Body[0] as BinaryExpressionNode;
            Statement s1 = Statement.CreateInstance((resultNode as CodeBlockNode).Body[0], tempGuid);
            refVarNames = Statement.GetReferencedVariableNames(s1, true);
            Assert.AreEqual(2, s1.StartLine);
            Assert.AreEqual(5, s1.EndLine);
            Assert.AreEqual("a",s1.AssignedVariable.Name);
            Assert.AreEqual(Statement.StatementType.Expression, s1.CurrentType);
            Assert.AreEqual(5, refVarNames.Count);
            Assert.AreEqual(true, refVarNames.Contains("d"));
        }
    }
}
