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
        public void TestStatement1()
        {
            string code = @"a=b+10;";
            Guid tempGuid = new Guid();
            CodeBlockNode commentNode;
            ProtoCore.AST.Node resultNode;
            resultNode = GraphToDSCompiler.GraphUtilities.Parse(code, out commentNode);
            BinaryExpressionNode ben = (resultNode as CodeBlockNode).Body[0] as BinaryExpressionNode;
            Statement s1 = Statement.CreateInstance((resultNode as CodeBlockNode).Body[0], tempGuid);
            Assert.AreEqual(1, s1.StartLine);
            Assert.AreEqual(1, s1.EndLine);
            Assert.AreEqual("a", s1.AssignedVariable.Name);
            //Assert.AreEqual("b", s1.ReferencedVariables[0].Name);
            //Assert.AreEqual(1, s1.ReferencedVariables.Count);
        }

    }
}
