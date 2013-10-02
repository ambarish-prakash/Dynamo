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
    class CodeNodeTests : DynamoUnitTest
    {
        [Test]
        public void TestVariableStruct()
        {
            string code = @"a;";
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
    }
}
