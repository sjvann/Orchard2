﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orchard.DeferredTasks;
using Orchard.Environment.Extensions;
using Orchard.Environment.Shell.Descriptor.Models;
using Orchard.Environment.Shell.State;
using Orchard.Events;

namespace Orchard.Environment.Shell
{
    public class ShellStateCoordinator : IShellDescriptorManagerEventHandler
    {
        private readonly ShellSettings _settings;
        private readonly IShellStateManager _stateManager;
        private readonly IExtensionManager _extensionManager;
        private readonly IEventBus _eventBus;
        private readonly IDeferredTaskEngine _deferredTaskEngine;

        public ShellStateCoordinator(
            ShellSettings settings,
            IShellStateManager stateManager,
            IExtensionManager extensionManager,
            IEventBus eventBus,
            IDeferredTaskEngine deferredTaskEngine,
            ILogger<ShellStateCoordinator> logger)
        {
            _deferredTaskEngine = deferredTaskEngine;
            _settings = settings;
            _stateManager = stateManager;
            _extensionManager = extensionManager;
            _eventBus = eventBus;
            Logger = logger;
        }

        public ILogger Logger { get; set; }

        void IShellDescriptorManagerEventHandler.Changed(ShellDescriptor descriptor, string tenant)
        {
            // deduce and apply state changes involved
            var shellState = _stateManager.GetShellStateAsync().Result;
            foreach (var feature in descriptor.Features)
            {
                var featureName = feature.Name;
                var featureState = shellState.Features.SingleOrDefault(f => f.Name == featureName);
                if (featureState == null)
                {
                    featureState = new ShellFeatureState
                    {
                        Name = featureName
                    };
                }
                if (!featureState.IsInstalled)
                {
                    _stateManager.UpdateInstalledState(featureState, ShellFeatureState.State.Rising);
                }
                if (!featureState.IsEnabled)
                {
                    _stateManager.UpdateEnabledState(featureState, ShellFeatureState.State.Rising);
                }
            }
            foreach (var featureState in shellState.Features)
            {
                var featureName = featureState.Name;
                if (descriptor.Features.Any(f => f.Name == featureName))
                {
                    continue;
                }
                if (!featureState.IsDisabled)
                {
                    _stateManager.UpdateEnabledState(featureState, ShellFeatureState.State.Falling);
                }
            }

            FireApplyChangesIfNeeded();
        }

        private void FireApplyChangesIfNeeded()
        {
            _deferredTaskEngine.AddTask(context =>
            {
                var stateManager = context.ServiceProvider.GetRequiredService<IShellStateManager>();
                var shellStateCoordinator = context.ServiceProvider.GetRequiredService<IShellStateUpdater>();
                var shellState = stateManager.GetShellStateAsync().Result;

                while (shellState.Features.Any(FeatureIsChanging))
                {
                    var descriptor = new ShellDescriptor
                    {
                        Features = shellState.Features
                            .Where(FeatureShouldBeLoadedForStateChangeNotifications)
                            .Select(x => new ShellFeature
                            {
                                Name = x.Name
                            })
                            .ToArray()
                    };

                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation("Adding pending task 'ApplyChanges' for shell '{0}'", _settings.Name);
                    }

                    shellStateCoordinator.ApplyChanges();
                }

                return Task.CompletedTask;
            });
        }

        private static bool FeatureIsChanging(ShellFeatureState shellFeatureState)
        {
            if (shellFeatureState.EnableState == ShellFeatureState.State.Rising ||
                shellFeatureState.EnableState == ShellFeatureState.State.Falling)
            {
                return true;
            }
            if (shellFeatureState.InstallState == ShellFeatureState.State.Rising ||
                shellFeatureState.InstallState == ShellFeatureState.State.Falling)
            {
                return true;
            }
            return false;
        }

        private static bool FeatureShouldBeLoadedForStateChangeNotifications(ShellFeatureState shellFeatureState)
        {
            return FeatureIsChanging(shellFeatureState) || shellFeatureState.EnableState == ShellFeatureState.State.Up;
        }
    }
}
