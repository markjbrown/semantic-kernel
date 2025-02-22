﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Services;
using Moq;
using Xunit;

namespace SemanticKernel.UnitTests.Planning;

public sealed class PlanTests
{
    [Fact]
    public Task CanCreatePlanAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        // Act
        var plan = new Plan(goal);

        // Assert
        Assert.Equal(goal, plan.Description);
        Assert.Empty(plan.Steps);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CanExecutePlanAsync()
    {
        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var plan = new Plan(goal);

        // Act
        var result = await plan.InvokeAsync("Some input", kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Some input", result.Context.Result);
        Assert.Null(result.GetValue<string>());
    }

    [Fact]
    public async Task CanExecutePlanWithContextAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var plan = new Plan(goal);
        var functionRunner = new Mock<IFunctionRunner>();
        var serviceProvider = new Mock<IAIServiceProvider>();

        var context = new SKContext(functionRunner.Object, serviceProvider.Object, new ContextVariables("Some input"));

        // Act
        var result = await plan.InvokeAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Some input", result.Context.Result);
        Assert.Null(result.GetValue<string>());

        plan = new Plan(goal);
        // Act
        context.Variables.Update("other input");
        result = await plan.InvokeAsync(context);
        // Assert
        Assert.NotNull(result);
        Assert.Equal("other input", result.Context.Result);
        Assert.Null(result.GetValue<string>());
    }

    [Fact]
    public async Task CanExecutePlanWithPlanStepAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var planInput = "Some input";
        var stepOutput = "Output: The input was: ";
        var plan = new Plan(goal);

        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object, new ContextVariables(stepOutput));

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update(returnContext.Variables.Input + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        plan.AddSteps(new Plan(mockFunction.Object));

        // Act
        var result = await plan.InvokeAsync(planInput, kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal($"{stepOutput}{planInput}", result.Context.Result);
        Assert.Equal($"{stepOutput}{planInput}", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanExecutePlanWithFunctionStepAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var planInput = "Some input";
        var stepOutput = "Output: The input was: ";
        var plan = new Plan(goal);

        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object, new ContextVariables(stepOutput));

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update(returnContext.Variables.Input + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        plan.AddSteps(mockFunction.Object);

        // Act
        var result = await plan.InvokeAsync(planInput, kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal($"{stepOutput}{planInput}", result.Context.Result);
        Assert.Equal($"{stepOutput}{planInput}", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanExecutePlanWithFunctionStepsAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var planInput = "Some input";
        var stepOutput = "Output: The input was: ";
        var plan = new Plan(goal);

        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object, new ContextVariables(stepOutput));

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update(returnContext.Variables.Input + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        plan.AddSteps(mockFunction.Object, mockFunction.Object);

        // Act
        var result = await plan.InvokeAsync(planInput, kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal($"{stepOutput}{planInput}{stepOutput}{planInput}", result.Context.Result);
        Assert.Equal($"{stepOutput}{planInput}{stepOutput}{planInput}", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CanExecutePlanWithStepsAndFunctionAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var planInput = "Some input";
        var stepOutput = "Output: The input was: ";
        var plan = new Plan(goal);

        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object, new ContextVariables(stepOutput));

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update(returnContext.Variables.Input + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        plan.AddSteps(new Plan(mockFunction.Object), mockFunction.Object);

        // Act
        var result = await plan.InvokeAsync(planInput, kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal($"{stepOutput}{planInput}{stepOutput}{planInput}", result.Context.Result);
        Assert.Equal($"{stepOutput}{planInput}{stepOutput}{planInput}", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CanExecutePlanWithStepsAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var planInput = "Some input";
        var stepOutput = "Output: The input was: ";
        var plan = new Plan(goal);

        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object, new ContextVariables(stepOutput));

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update(returnContext.Variables.Input + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        plan.AddSteps(new Plan(mockFunction.Object), new Plan(mockFunction.Object));

        // Act
        var result = await plan.InvokeAsync(planInput, kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal($"{stepOutput}{planInput}{stepOutput}{planInput}", result.Context.Result);
        Assert.Equal($"{stepOutput}{planInput}{stepOutput}{planInput}", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CanStepPlanWithStepsAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var planInput = "Some input";
        var stepOutput = "Output: The input was: ";
        var plan = new Plan(goal);

        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object, new ContextVariables(stepOutput)
        );

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update(returnContext.Variables.Input + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        plan.AddSteps(mockFunction.Object, mockFunction.Object);

        // Act
        var result = await kernel.Object.StepAsync(planInput, plan);

        // Assert
        Assert.NotNull(result);
        Assert.Equal($"{stepOutput}{planInput}", result.State.ToString());

        // Act
        result = await kernel.Object.StepAsync(result);

        // Assert
        Assert.NotNull(result);
        Assert.Equal($"{stepOutput}{planInput}{stepOutput}{planInput}", result.State.ToString());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CanStepPlanWithStepsAndContextAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var planInput = "Some input";
        var stepOutput = "Output: The input was: ";
        var plan = new Plan(goal);

        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object, new ContextVariables(stepOutput));

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
            {
                c.Variables.TryGetValue("variables", out string? v);
                returnContext.Variables.Update(returnContext.Variables.Input + c.Variables.Input + v);
            })
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext)));
        mockFunction.Setup(x => x.Describe()).Returns(new FunctionView("functionName", "pluginName", "description")
        {
            Parameters = new ParameterView[] { new("variables") }
        });

        plan.AddSteps(mockFunction.Object, mockFunction.Object);

        // Act
        var cv = new ContextVariables(planInput);
        cv.Set("variables", "foo");
        plan = await kernel.Object.StepAsync(cv, plan);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal($"{stepOutput}{planInput}foo", plan.State.ToString());

        // Act
        cv.Set("variables", "bar");
        cv.Update(string.Empty);
        plan = await kernel.Object.StepAsync(cv, plan);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal($"{stepOutput}{planInput}foo{stepOutput}{planInput}foobar", plan.State.ToString());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task StepExceptionIsThrownAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var planInput = "Some input";
        var stepOutput = "Output: The input was: ";
        var plan = new Plan(goal);

        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object, new ContextVariables(stepOutput));

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Throws(new ArgumentException("Error message"));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        plan.AddSteps(mockFunction.Object, mockFunction.Object);

        // Act
        var cv = new ContextVariables(planInput);
        await Assert.ThrowsAsync<ArgumentException>(async () => await kernel.Object.StepAsync(cv, plan));
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanStepExceptionIsThrownAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var planInput = "Some input";
        var plan = new Plan(goal);

        // Arrange
        var logger = new Mock<ILogger>();
        var functions = new Mock<IFunctionCollection>();
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object);

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Throws(new ArgumentException("Error message"));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        plan.AddSteps(new Plan(mockFunction.Object), new Plan(mockFunction.Object));

        // Act
        var cv = new ContextVariables(planInput);
        await Assert.ThrowsAsync<ArgumentException>(async () => await kernel.Object.StepAsync(cv, plan));
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanExecutePlanWithTreeStepsAsync()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var plan = new Plan(goal);
        var subPlan = new Plan("Write a poem or joke");

        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object);

        var childFunction1 = new Mock<ISKFunction>();
        childFunction1.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update("Child 1 output!" + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("child1", "pluginName", returnContext, returnContext.Result)));
        childFunction1.Setup(x => x.Describe()).Returns(() => new FunctionView("child1", "pluginName"));

        var childFunction2 = new Mock<ISKFunction>();
        childFunction2.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update("Child 2 is happy about " + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("child2", "pluginName", returnContext, returnContext.Result)));
        childFunction2.Setup(x => x.Describe()).Returns(() => new FunctionView("child2", "pluginName"));

        var childFunction3 = new Mock<ISKFunction>();
        childFunction3.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update("Child 3 heard " + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("child3", "pluginName", returnContext, returnContext.Result)));
        childFunction3.Setup(x => x.Describe()).Returns(() => new FunctionView("child3", "pluginName"));

        var nodeFunction1 = new Mock<ISKFunction>();
        nodeFunction1.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update(c.Variables.Input + " - this just happened."))
            .Returns(() => Task.FromResult(new FunctionResult("node1", "pluginName", returnContext, returnContext.Result)));
        nodeFunction1.Setup(x => x.Describe()).Returns(() => new FunctionView("node1", "pluginName"));

        subPlan.AddSteps(childFunction1.Object, childFunction2.Object, childFunction3.Object);
        plan.AddSteps(subPlan);
        plan.AddSteps(nodeFunction1.Object);

        // Act
        while (plan.HasNextStep)
        {
            plan = await kernel.Object.StepAsync(plan);
        }

        // Assert
        Assert.NotNull(plan);
        Assert.Equal("Child 3 heard Child 2 is happy about Child 1 output!Write a poem or joke - this just happened.", plan.State.ToString());
        nodeFunction1.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
        childFunction1.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
        childFunction2.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
        childFunction3.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CanCreatePlanWithGoalAndSteps()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var plan = new Plan(goal, new Mock<ISKFunction>().Object, new Mock<ISKFunction>().Object);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(goal, plan.Description);
        Assert.Equal(2, plan.Steps.Count);
    }

    [Fact]
    public void CanCreatePlanWithGoalAndSubPlans()
    {
        // Arrange
        var goal = "Write a poem or joke and send it in an e-mail to Kai.";
        var plan = new Plan(goal, new Plan("Write a poem or joke"), new Plan("Send it in an e-mail to Kai"));

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(goal, plan.Description);
        Assert.Equal(2, plan.Steps.Count);
    }

    [Fact]
    public async Task CanExecutePlanWithOneStepAndStateAsync()
    {
        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object);

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update("Here is a poem about " + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        var plan = new Plan(mockFunction.Object);
        plan.State.Set("input", "Cleopatra");

        // Act
        var result = await plan.InvokeAsync(kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Here is a poem about Cleopatra", result.Context.Result);
        Assert.Equal("Here is a poem about Cleopatra", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanExecutePlanWithStateAsync()
    {
        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        functionRunner.Setup(k => k.RunAsync(It.IsAny<ISKFunction>(), It.IsAny<ContextVariables>(), It.IsAny<AIRequestSettings?>(), It.IsAny<CancellationToken>()))
        .Returns<ISKFunction, ContextVariables, AIRequestSettings, CancellationToken>(async (function, variables, settings, ct) =>
        {
            var c = new SKContext(functionRunner.Object, serviceProvider.Object, variables);
            var functionResult = await function.InvokeAsync(c, cancellationToken: ct);
            return functionResult;
        });

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object);

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
            {
                c.Variables.TryGetValue("type", out string? t);
                returnContext.Variables.Update($"Here is a {t} about " + c.Variables.Input);
            })
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        var planStep = new Plan(mockFunction.Object);
        planStep.Parameters.Set("type", string.Empty);
        var plan = new Plan(string.Empty);
        plan.AddSteps(planStep);
        plan.State.Set("input", "Cleopatra");
        plan.State.Set("type", "poem");

        // Act
        var result = await plan.InvokeAsync(kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Here is a poem about Cleopatra", result.Context.Result);
        Assert.Equal("Here is a poem about Cleopatra", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanExecutePlanWithCustomContextAsync()
    {
        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object);

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings?, CancellationToken>((c, s, ct) =>
            {
                c.Variables.TryGetValue("type", out string? t);
                returnContext.Variables.Update($"Here is a {t} about " + c.Variables.Input);
            })
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        var plan = new Plan(mockFunction.Object);
        plan.State.Set("input", "Cleopatra");
        plan.State.Set("type", "poem");

        // Act
        var result = await plan.InvokeAsync(kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Here is a poem about Cleopatra", result.Context.Result);
        Assert.Equal("Here is a poem about Cleopatra", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);

        plan = new Plan(mockFunction.Object);
        plan.State.Set("input", "Cleopatra");
        plan.State.Set("type", "poem");

        var contextOverride = new SKContext(functionRunner.Object, serviceProvider.Object);
        contextOverride.Variables.Set("type", "joke");
        contextOverride.Variables.Update("Medusa");

        // Act
        result = await plan.InvokeAsync(contextOverride);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Here is a joke about Medusa", result.Context.Result);
        Assert.Equal("Here is a joke about Medusa", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CanExecutePlanWithCustomStateAsync()
    {
        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object);

        var mockFunction = new Mock<ISKFunction>();
        mockFunction.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
            {
                c.Variables.TryGetValue("type", out string? t);
                returnContext.Variables.Update($"Here is a {t} about " + c.Variables.Input);
            })
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        mockFunction.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        var planStep = new Plan(mockFunction.Object);
        planStep.Parameters.Set("type", string.Empty);
        var plan = new Plan("A plan");
        plan.State.Set("input", "Medusa");
        plan.State.Set("type", "joke");
        plan.AddSteps(planStep);

        // Act
        var result = await plan.InvokeAsync(kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Here is a joke about Medusa", result.Context.Result);
        Assert.Equal("Here is a joke about Medusa", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Once);

        planStep = new Plan(mockFunction.Object);
        plan = new Plan("A plan");
        planStep.Parameters.Set("input", "Medusa");
        planStep.Parameters.Set("type", "joke");
        plan.State.Set("input", "Cleopatra"); // state input will not override parameter
        plan.State.Set("type", "poem");
        plan.AddSteps(planStep);

        // Act
        result = await plan.InvokeAsync(kernel.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Here is a poem about Medusa", result.Context.Result);
        Assert.Equal("Here is a poem about Medusa", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Exactly(2));

        planStep = new Plan(mockFunction.Object);
        plan = new Plan("A plan");
        planStep.Parameters.Set("input", "Cleopatra");
        planStep.Parameters.Set("type", "poem");
        plan.AddSteps(planStep);
        var contextOverride = new SKContext(functionRunner.Object, serviceProvider.Object);
        contextOverride.Variables.Set("type", "joke");
        contextOverride.Variables.Update("Medusa"); // context input will not override parameters

        // Act
        result = await plan.InvokeAsync(contextOverride);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Here is a joke about Cleopatra", result.Context.Result);
        Assert.Equal("Here is a joke about Cleopatra", result.GetValue<string>());
        mockFunction.Verify(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CanExecutePlanWithJoinedResultAsync()
    {
        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        functionRunner.Setup(k => k.RunAsync(It.IsAny<ISKFunction>(), It.IsAny<ContextVariables>(), It.IsAny<AIRequestSettings?>(), It.IsAny<CancellationToken>()))
        .Returns<ISKFunction, ContextVariables, AIRequestSettings, CancellationToken>(async (function, variables, settings, ct) =>
        {
            var c = new SKContext(functionRunner.Object, serviceProvider.Object, variables);
            var functionResult = await function.InvokeAsync(c, cancellationToken: ct);
            return functionResult;
        });

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object);

        var outlineMock = new Mock<ISKFunction>();
        outlineMock.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update($"Here is a {c.Variables["chapterCount"]} chapter outline about " + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("outline", "pluginName", returnContext, returnContext.Result)));
        outlineMock.Setup(x => x.Describe()).Returns(() => new FunctionView("outline", "pluginName"));

        var elementAtIndexMock = new Mock<ISKFunction>();
        elementAtIndexMock.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
            {
                returnContext.Variables.Update($"Outline section #{c.Variables["index"]} of {c.Variables["count"]}: " + c.Variables.Input);
            })
            .Returns(() => Task.FromResult(new FunctionResult("elementAt", "pluginName", returnContext, returnContext.Result)));
        elementAtIndexMock.Setup(x => x.Describe()).Returns(() => new FunctionView("elementAt", "pluginName"));

        var novelChapterMock = new Mock<ISKFunction>();
        novelChapterMock.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
            {
                returnContext.Variables.Update(
                    $"Chapter #{c.Variables["chapterIndex"]}: {c.Variables.Input}\nTheme:{c.Variables["theme"]}\nPreviously:{c.Variables["previousChapter"]}");
            })
            .Returns(() => Task.FromResult(new FunctionResult("novelChapter", "pluginName", returnContext, returnContext.Result)));
        novelChapterMock.Setup(x => x.Describe()).Returns(() => new FunctionView("novelChapter", "pluginName"));

        var plan = new Plan("A plan with steps that alternate appending to the plan result.");

        // Steps:
        // - WriterPlugin.NovelOutline chapterCount='3' INPUT='A group of kids in a club called 'The Thinking Caps' that solve mysteries and puzzles using their creativity and logic.' endMarker='<!--===ENDPART===-->' => OUTLINE
        // - MiscPlugin.ElementAtIndex count='3' INPUT='$OUTLINE' index='0' => CHAPTER_1_SYNOPSIS
        // - WriterPlugin.NovelChapter chapterIndex='1' previousChapter='' INPUT='$CHAPTER_1_SYNOPSIS' theme='Children's mystery' => RESULT__CHAPTER_1
        // - MiscPlugin.ElementAtIndex count='3' INPUT='$OUTLINE' index='1' => CHAPTER_2_SYNOPSIS
        // - WriterPlugin.NovelChapter chapterIndex='2' previousChapter='$CHAPTER_1_SYNOPSIS' INPUT='$CHAPTER_2_SYNOPSIS' theme='Children's mystery' => RESULT__CHAPTER_2
        // - MiscPlugin.ElementAtIndex count='3' INPUT='$OUTLINE' index='2' => CHAPTER_3_SYNOPSIS
        // - WriterPlugin.NovelChapter chapterIndex='3' previousChapter='$CHAPTER_2_SYNOPSIS' INPUT='$CHAPTER_3_SYNOPSIS' theme='Children's mystery' => RESULT__CHAPTER_3
        var planStep = new Plan(outlineMock.Object);
        planStep.Parameters.Set("input",
            "NovelOutline function input.");
        planStep.Parameters.Set("chapterCount", "3");
        planStep.Outputs.Add("OUTLINE");
        plan.AddSteps(planStep);

        planStep = new Plan(elementAtIndexMock.Object);
        planStep.Parameters.Set("count", "3");
        planStep.Parameters.Set("INPUT", "$OUTLINE");
        planStep.Parameters.Set("index", "0");
        planStep.Outputs.Add("CHAPTER_1_SYNOPSIS");
        plan.AddSteps(planStep);

        planStep = new Plan(novelChapterMock.Object);
        planStep.Parameters.Set("chapterIndex", "1");
        planStep.Parameters.Set("previousChapter", " ");
        planStep.Parameters.Set("INPUT", "$CHAPTER_1_SYNOPSIS");
        planStep.Parameters.Set("theme", "Children's mystery");
        planStep.Outputs.Add("RESULT__CHAPTER_1");
        plan.Outputs.Add("RESULT__CHAPTER_1");
        plan.AddSteps(planStep);

        planStep = new Plan(elementAtIndexMock.Object);
        planStep.Parameters.Set("count", "3");
        planStep.Parameters.Set("INPUT", "$OUTLINE");
        planStep.Parameters.Set("index", "1");
        planStep.Outputs.Add("CHAPTER_2_SYNOPSIS");
        plan.AddSteps(planStep);

        planStep = new Plan(novelChapterMock.Object);
        planStep.Parameters.Set("chapterIndex", "2");
        planStep.Parameters.Set("previousChapter", "$CHAPTER_1_SYNOPSIS");
        planStep.Parameters.Set("INPUT", "$CHAPTER_2_SYNOPSIS");
        planStep.Parameters.Set("theme", "Children's mystery");
        planStep.Outputs.Add("RESULT__CHAPTER_2");
        plan.Outputs.Add("RESULT__CHAPTER_2");
        plan.AddSteps(planStep);

        planStep = new Plan(elementAtIndexMock.Object);
        planStep.Parameters.Set("count", "3");
        planStep.Parameters.Set("INPUT", "$OUTLINE");
        planStep.Parameters.Set("index", "2");
        planStep.Outputs.Add("CHAPTER_3_SYNOPSIS");
        plan.AddSteps(planStep);

        planStep = new Plan(novelChapterMock.Object);
        planStep.Parameters.Set("chapterIndex", "3");
        planStep.Parameters.Set("previousChapter", "$CHAPTER_2_SYNOPSIS");
        planStep.Parameters.Set("INPUT", "$CHAPTER_3_SYNOPSIS");
        planStep.Parameters.Set("theme", "Children's mystery");
        planStep.Outputs.Add("CHAPTER_3");
        plan.Outputs.Add("CHAPTER_3");
        plan.AddSteps(planStep);

        // Act
        var result = await plan.InvokeAsync(kernel.Object);

        var expected =
            @"Chapter #1: Outline section #0 of 3: Here is a 3 chapter outline about NovelOutline function input.
Theme:Children's mystery
Previously:
Chapter #2: Outline section #1 of 3: Here is a 3 chapter outline about NovelOutline function input.
Theme:Children's mystery
Previously:Outline section #0 of 3: Here is a 3 chapter outline about NovelOutline function input.
Chapter #3: Outline section #2 of 3: Here is a 3 chapter outline about NovelOutline function input.
Theme:Children's mystery
Previously:Outline section #1 of 3: Here is a 3 chapter outline about NovelOutline function input.";

        // Assert
        Assert.Equal(expected, result.GetValue<string>());
        Assert.Equal(expected, result.Context.Result);
        Assert.True(result.TryGetMetadataValue<string>("RESULT__CHAPTER_1", out var chapter1));
        Assert.True(result.TryGetMetadataValue<string>("RESULT__CHAPTER_2", out var chapter2));
        Assert.True(result.TryGetMetadataValue<string>("CHAPTER_3", out var chapter3));
        Assert.False(result.TryGetMetadataValue<string>("CHAPTER_3_SYNOPSIS", out var chapter3Synopsis));
    }

    [Fact]
    public async Task CanExecutePlanWithExpandedAsync()
    {
        // Arrange
        var (kernel, functionRunner, serviceProvider) = this.SetupKernelMock();

        var returnContext = new SKContext(functionRunner.Object, serviceProvider.Object);

        var functionMock = new Mock<ISKFunction>();
        functionMock.Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), null, It.IsAny<CancellationToken>()))
            .Callback<SKContext, AIRequestSettings, CancellationToken>((c, s, ct) =>
                returnContext.Variables.Update($"Here is a payload '{c.Variables["payload"]}' for " + c.Variables.Input))
            .Returns(() => Task.FromResult(new FunctionResult("functionName", "pluginName", returnContext, returnContext.Result)));
        functionMock.Setup(x => x.Describe()).Returns(() => new FunctionView("functionName", "pluginName"));

        var plan = new Plan("A plan with steps that have variables with a $ in them but not associated with an output");

        var planStep = new Plan(functionMock.Object);
        planStep.Parameters.Set("input",
            "Function input.");
        planStep.Parameters.Set("payload", @"{""prop"":""value"", ""$prop"": 3, ""prop2"": ""my name is $pop and $var""}");
        plan.AddSteps(planStep);
        plan.State.Set("var", "foobar");

        // Act
        var result = await plan.InvokeAsync(kernel.Object);

        var expected =
            @"Here is a payload '{""prop"":""value"", ""$prop"": 3, ""prop2"": ""my name is $pop and foobar""}' for Function input.";

        // Assert
        Assert.Equal(expected, result.Context.Result);
        Assert.Equal(expected, result.GetValue<string>());
    }

    private (Mock<IKernel> kernelMock, Mock<IFunctionRunner> functionRunnerMock, Mock<IAIServiceProvider> serviceProviderMock) SetupKernelMock(IFunctionCollection? functions = null)
    {
        functions ??= new Mock<IFunctionCollection>().Object;

        var kernel = new Mock<IKernel>();
        var functionRunner = new Mock<IFunctionRunner>();
        var serviceProvider = new Mock<IAIServiceProvider>();

        kernel.SetupGet(x => x.Functions).Returns(functions);
        kernel.Setup(k => k.CreateNewContext(It.IsAny<ContextVariables>(), It.IsAny<IReadOnlyFunctionCollection>(), It.IsAny<ILoggerFactory>(), It.IsAny<CultureInfo>()))
            .Returns<ContextVariables, IReadOnlyFunctionCollection, ILoggerFactory, CultureInfo>((contextVariables, skills, loggerFactory, culture) =>
        {
            return new SKContext(functionRunner.Object, serviceProvider.Object, contextVariables, functions);
        });

        functionRunner.Setup(k => k.RunAsync(It.IsAny<ISKFunction>(), It.IsAny<ContextVariables>(), It.IsAny<AIRequestSettings?>(), It.IsAny<CancellationToken>()))
        .Returns<ISKFunction, ContextVariables, AIRequestSettings, CancellationToken>(async (function, variables, settings, ct) =>
        {
            var c = new SKContext(functionRunner.Object, serviceProvider.Object, variables);
            var functionResult = await function.InvokeAsync(c, cancellationToken: ct);
            return functionResult;
        });

        return (kernel, functionRunner, serviceProvider);
    }
}
