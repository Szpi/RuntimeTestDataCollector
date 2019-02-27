﻿using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using RuntimeTestDataCollector.ObjectInitializationGeneration.CodeGeneration;
using RuntimeTestDataCollector.ObjectInitializationGeneration.CodeGeneration.Factory;

namespace DumpStackToCSharpCodeTests.ObjectInitializationGeneration
{
    [TestFixture]
    public class GenerateComplexObjectType
    {
        private CodeGeneratorManager _codeGeneratorManager;

        [SetUp]
        public void Setup()
        {
            var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);
            _codeGeneratorManager = CodeGeneratorManagerFactory.Create();
        }

        [Test]
        public void ShouldGenerate_ComplexObjectAssignment()
        {
            var firstElement = new ExpressionData("string", "test1", "TestString", new ExpressionData[] { }, "string");
            var secondElement = new ExpressionData("int", "10", "TestInt", new ExpressionData[] { }, "int");
            var stackObject = new List<ExpressionData>()
            {
                new ExpressionData("Test", "Test", "testComplexObject", new [] { firstElement, secondElement }, "Test")
            };

            var generated = _codeGeneratorManager.GenerateStackDump(stackObject);

            generated.Should().Be("var testComplexObject = new Test()\n{\r\n    TestString = \"test1\",\r\n    TestInt = 10\r\n};\n");
        }


        [Test]
        public void ShouldGenerate_ComplexObjectAssignment_WithStringArray()
        {
            var firstElement = new ExpressionData("string", "test1", "[0]", new ExpressionData[] { }, "string");
            var secondElement = new ExpressionData("string", "test2", "[1]", new ExpressionData[] { }, "string");
            var array = new ExpressionData("string[]",
                                   "{string[2]}",
                                   "testStringArray",
                                   new []
                                   {
                                       firstElement,
                                       secondElement
                                   },
                                   "string[]");

            var stackObject = new List<ExpressionData>()
            {
                new ExpressionData("Test", "Test", "testComplexObject", new [] { array }, "Test")
            };
            var generated = _codeGeneratorManager.GenerateStackDump(stackObject);

            generated.Should().Be("var testComplexObject = new Test()\n{\r\n    testStringArray = new string[]\r\n    {\r\n        \"test1\",\r\n        \"test2\"\r\n    }\r\n};\n");
        }
    }
}