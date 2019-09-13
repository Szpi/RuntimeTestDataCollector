﻿using EnvDTE;
using EnvDTE80;
using FluentAssertions;
using RuntimeTestDataCollector.ObjectInitializationGeneration.CodeGeneration;
using RuntimeTestDataCollector.ObjectInitializationGeneration.Type;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RuntimeTestDataCollector.StackFrameAnalyzer
{
    public class DebuggerStackFrameAnalyzer
    {
        private readonly int _maxObjectsToAnalyze;
        private readonly int _maxObjectDepth;
        private readonly ConcreteTypeAnalyzer _concreteTypeAnalyzer;
        private readonly bool _generateTypeWithNamespace;
        private readonly TimeSpan _maxGenerationTime = TimeSpan.FromSeconds(5);

        public DebuggerStackFrameAnalyzer(int maxObjectDepth,
                                          ConcreteTypeAnalyzer concreteTypeAnalyzer,
                                          bool generateTypeWithNamespace,
                                          int maxObjectsToAnalyze)
        {
            _maxObjectDepth = maxObjectDepth;
            _concreteTypeAnalyzer = concreteTypeAnalyzer;
            _generateTypeWithNamespace = generateTypeWithNamespace;
            _maxObjectsToAnalyze = maxObjectsToAnalyze;
        }

        public async Task<IReadOnlyList<ExpressionData>> AnalyzeCurrentStackAsync(DTE2 dte, CancellationToken token)
        {
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
            var currentStackExpressionsData = new List<ExpressionData>();
            if (dte?.Debugger?.CurrentStackFrame == null)
            {
                return currentStackExpressionsData;
            }

            var generationTime = Stopwatch.StartNew();
            int currentAnalyzedObject = 0;
            foreach (Expression expression in dte.Debugger.CurrentStackFrame.Locals)
            {
                if (currentAnalyzedObject > _maxObjectsToAnalyze)
                {
                    break;
                }

                var expressionData = GenerateExpressionData(expression, ref currentAnalyzedObject, generationTime);

                currentStackExpressionsData.Add(expressionData);
                if (HasExceedMaxGenerationTime(generationTime))
                {
                    Trace.WriteLine($">>>>>>>>>>>> seconds {generationTime.Elapsed.TotalSeconds} breaking !!");
                    break;
                }
            }

            Trace.WriteLine($">>>>>>>>>>>> |||||||||||||||| total time seconds {generationTime.Elapsed.TotalSeconds}");
            return currentStackExpressionsData;
        }

        private ExpressionData GenerateExpressionData(Expression expression, ref int currentAnalyzedObjects, Stopwatch generationTime)
        {
            if (expression == null)
            {
                return null;
            }
            var queue = new Queue<(Expression expression, ExpressionData parentExpressionData)>(_maxObjectsToAnalyze * 2);

            queue.Enqueue((expression, null));
            ExpressionData mainObject = null;
            var currentObjectDepth = 1;

            var nextDepthAfterAnalyzedObjects = 1;
            var depthEndsAfterAnalyzingObjects = expression.DataMembers.Count;

            while (queue.Any())
            {
                var stackObject = queue.Dequeue();
                var dataMember = stackObject.expression;

                currentAnalyzedObjects++;

                if (dataMember == null || string.IsNullOrEmpty(dataMember.Type))
                {
                    continue;
                }

                if (IsDictionaryDuplicatedValue(dataMember.Name))
                {
                    continue;
                }

                if (HasExceedMaxGenerationTime(generationTime))
                {
                    Trace.WriteLine($">>>>>>>>>>>> seconds {generationTime.Elapsed.TotalSeconds} breaking");
                    return mainObject;
                }

                if (currentAnalyzedObjects > _maxObjectsToAnalyze)
                {
                    return mainObject;
                }

                if (currentObjectDepth > _maxObjectDepth)
                {
                    return mainObject;
                }

                var expressionData = GetExpressionData(dataMember, new List<ExpressionData>());
                if (HasParentExpressionData(stackObject))
                {
                    stackObject.parentExpressionData.UnderlyingExpressionData.Add(expressionData);
                }
                else
                {
                    mainObject = expressionData;
                }

                foreach (Expression child in dataMember.DataMembers)
                {
                    nextDepthAfterAnalyzedObjects += dataMember.DataMembers.Count;
                    queue.Enqueue((child, expressionData));
                }

                if (WasCurrentDepthReached(currentAnalyzedObjects, depthEndsAfterAnalyzingObjects))
                {
                    depthEndsAfterAnalyzingObjects = nextDepthAfterAnalyzedObjects;
                    currentObjectDepth++;
                }
            }

            return mainObject;            
        }

        private static bool WasCurrentDepthReached(int currentAnalyzedObjects, int depthEndsAfterAnalyzingObjects)
        {
            return currentAnalyzedObjects == depthEndsAfterAnalyzingObjects;
        }

        private static bool HasParentExpressionData((Expression expression, ExpressionData parentExpressionData) stackObject)
        {
            return stackObject.parentExpressionData != null;
        }

        private bool IsDictionaryDuplicatedValue(string dataMemberType)
        {
            return dataMemberType == "type" || dataMemberType == "value";
        }

        private string GetTypeToGenerate(string type)
        {
            var concreteType = _concreteTypeAnalyzer.ParseConcreteType(type);
            return _generateTypeWithNamespace
                ? concreteType
                : _concreteTypeAnalyzer.GetTypeWithoutNamespace(concreteType);
        }

        private ExpressionData GetExpressionData(Expression expression, List<ExpressionData> expressionData)
        {
            var value = CorrectCharValue(expression.Type, expression.Value);
            return new ExpressionData(GetTypeToGenerate(expression.Type), value, expression.Name,
                                      expressionData, expression.Type);
        }

        private bool HasExceedMaxGenerationTime(Stopwatch generationTime)
        {
            return generationTime.Elapsed > _maxGenerationTime;
        }

        private string CorrectCharValue(string type, string value)
        {
            if (type != "char")
            {
                return value;
            }

            var charStartIndex = value.IndexOf("\'");

            return value.Substring(charStartIndex);
        }
    }
}