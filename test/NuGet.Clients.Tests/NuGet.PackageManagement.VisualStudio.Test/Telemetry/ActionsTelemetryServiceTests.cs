// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Moq;
using NuGet.Common;
using NuGet.VisualStudio;
using Xunit;

namespace NuGet.PackageManagement.VisualStudio.Test
{
    public class ActionsTelemetryServiceTests
    {
        [Theory]
        [InlineData(NuGetOperationType.Install, OperationSource.UI)]
        [InlineData(NuGetOperationType.Update, OperationSource.UI)]
        [InlineData(NuGetOperationType.Uninstall, OperationSource.UI)]
        [InlineData(NuGetOperationType.Install, OperationSource.PMC)]
        [InlineData(NuGetOperationType.Update, OperationSource.PMC)]
        [InlineData(NuGetOperationType.Uninstall, OperationSource.PMC)]
        public void ActionsTelemetryService_EmitActionEvent_OperationSucceed(NuGetOperationType operationType, OperationSource source)
        {
            // Arrange
            var telemetrySession = new Mock<ITelemetrySession>();
            TelemetryEvent lastTelemetryEvent = null;
            telemetrySession
                .Setup(x => x.PostEvent(It.IsAny<TelemetryEvent>()))
                .Callback<TelemetryEvent>(x => lastTelemetryEvent = x);

            var actionTelemetryData = new VSActionsTelemetryEvent(
                projectIds: new[] { Guid.NewGuid().ToString() },
                operationType: operationType,
                source: source,
                startTime: DateTimeOffset.Now.AddSeconds(-3),
                status: NuGetOperationStatus.Succeeded,
                packageCount: 1,
                endTime: DateTimeOffset.Now,
                duration: 2.10);
            var service = new NuGetVSActionTelemetryService(telemetrySession.Object);

            // Act
            service.EmitTelemetryEvent(actionTelemetryData);

            // Assert
            VerifyTelemetryEventData(service.OperationId, actionTelemetryData, lastTelemetryEvent);
        }

        [Theory]
        [InlineData(NuGetOperationType.Install, OperationSource.UI)]
        [InlineData(NuGetOperationType.Update, OperationSource.UI)]
        [InlineData(NuGetOperationType.Uninstall, OperationSource.UI)]
        [InlineData(NuGetOperationType.Install, OperationSource.PMC)]
        [InlineData(NuGetOperationType.Update, OperationSource.PMC)]
        [InlineData(NuGetOperationType.Uninstall, OperationSource.PMC)]
        public void ActionsTelemetryService_EmitActionEvent_OperationFailed(NuGetOperationType operationType, OperationSource source)
        {
            // Arrange
            var telemetrySession = new Mock<ITelemetrySession>();
            TelemetryEvent lastTelemetryEvent = null;
            telemetrySession
                .Setup(x => x.PostEvent(It.IsAny<TelemetryEvent>()))
                .Callback<TelemetryEvent>(x => lastTelemetryEvent = x);

            var actionTelemetryData = new VSActionsTelemetryEvent(
                projectIds: new[] { Guid.NewGuid().ToString() },
                operationType: operationType,
                source: source,
                startTime: DateTimeOffset.Now.AddSeconds(-2),
                status: NuGetOperationStatus.Failed,
                packageCount: 1,
                endTime: DateTimeOffset.Now,
                duration: 1.30);
            var service = new NuGetVSActionTelemetryService(telemetrySession.Object);

            // Act
            service.EmitTelemetryEvent(actionTelemetryData);

            // Assert
            VerifyTelemetryEventData(service.OperationId, actionTelemetryData, lastTelemetryEvent);
        }

        [Theory]
        [InlineData(NuGetOperationType.Install)]
        [InlineData(NuGetOperationType.Update)]
        [InlineData(NuGetOperationType.Uninstall)]
        public void ActionsTelemetryService_EmitActionEvent_OperationNoOp(NuGetOperationType operationType)
        {
            // Arrange
            var telemetrySession = new Mock<ITelemetrySession>();
            TelemetryEvent lastTelemetryEvent = null;
            telemetrySession
                .Setup(x => x.PostEvent(It.IsAny<TelemetryEvent>()))
                .Callback<TelemetryEvent>(x => lastTelemetryEvent = x);

            var actionTelemetryData = new VSActionsTelemetryEvent(
                projectIds: new[] { Guid.NewGuid().ToString() },
                operationType: operationType,
                source: OperationSource.PMC,
                startTime: DateTimeOffset.Now.AddSeconds(-1),
                status: NuGetOperationStatus.NoOp,
                packageCount: 1,
                endTime: DateTimeOffset.Now,
                duration: .40);
            var service = new NuGetVSActionTelemetryService(telemetrySession.Object);

            // Act
            service.EmitTelemetryEvent(actionTelemetryData);

            // Assert
            VerifyTelemetryEventData(service.OperationId, actionTelemetryData, lastTelemetryEvent);
        }

        [Theory]
        [MemberData(nameof(GetStepNames))]
        public void ActionsTelemetryService_EmitActionStepsEvent(string stepName)
        {
            // Arrange
            var telemetrySession = new Mock<ITelemetrySession>();
            TelemetryEvent lastTelemetryEvent = null;
            telemetrySession
                .Setup(x => x.PostEvent(It.IsAny<TelemetryEvent>()))
                .Callback<TelemetryEvent>(x => lastTelemetryEvent = x);

            var duration = 1.12;
            var stepNameWithProject = string.Format(stepName, "testProject");
            var service = new NuGetVSActionTelemetryService(telemetrySession.Object);

            // Act
            service.EmitTelemetryEvent(new ActionTelemetryStepEvent(stepNameWithProject, duration));

            // Assert
            Assert.NotNull(lastTelemetryEvent);
            Assert.Equal(ActionTelemetryStepEvent.NugetActionStepsEventName, lastTelemetryEvent.Name);
            Assert.Equal(3, lastTelemetryEvent.Properties.Count);

            Assert.Equal(service.OperationId, lastTelemetryEvent.Properties["OperationId"].ToString());
            Assert.Equal(stepNameWithProject, lastTelemetryEvent.Properties["StepName"].ToString());
            Assert.Equal(duration, (double)lastTelemetryEvent.Properties["Duration"]);
        }

        public static IEnumerable<object[]> GetStepNames()
        {
            yield return new[] { TelemetryConstants.PreviewBuildIntegratedStepName };
            yield return new[] { TelemetryConstants.GatherDependencyStepName };
            yield return new[] { TelemetryConstants.ResolveDependencyStepName };
            yield return new[] { TelemetryConstants.ResolvedActionsStepName };
            yield return new[] { TelemetryConstants.ExecuteActionStepName };
        }

        private void VerifyTelemetryEventData(string operationId, VSActionsTelemetryEvent expected, TelemetryEvent actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(ActionEventBase.NugetActionEventName, actual.Name);
            Assert.Equal(10, actual.Properties.Count);

            Assert.Equal(expected.OperationType.ToString(), actual.Properties["OperationType"].ToString());
            Assert.Equal(expected.Source.ToString(), actual.Properties["Source"].ToString());

            TestTelemetryUtility.VerifyTelemetryEventData(operationId, expected, actual);
        }
    }
}
