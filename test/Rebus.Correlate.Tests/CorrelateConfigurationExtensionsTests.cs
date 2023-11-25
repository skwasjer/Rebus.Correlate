using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Activation;
using Rebus.Config;

namespace Rebus.Correlate;

public class CorrelateConfigurationExtensionsTests
{
    public class WithServiceProvider : CorrelateConfigurationExtensionsTests
    {
        [Fact]
        public void When_configuring_instance_without_configurer_it_should_throw()
        {
            OptionsConfigurer configurer = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            Action act = () => configurer.EnableCorrelate(new ServiceCollection().BuildServiceProvider());

            // Assert
            act.Should()
                .Throw<ArgumentNullException>()
                .Where(exception => exception.ParamName == nameof(configurer));
        }

        [Fact]
        public void When_configuring_instance_without_serviceProvider_it_should_throw()
        {
            IServiceProvider serviceProvider = null;
            Action act = () =>
                Configure.With(new BuiltinHandlerActivator())
                    .Options(opts =>
                        // ReSharper disable once ExpressionIsAlwaysNull
                        opts.EnableCorrelate(serviceProvider)
                    );

            // Assert
            act.Should()
                .Throw<ArgumentNullException>()
                .Where(exception => exception.ParamName == nameof(serviceProvider));
        }
    }

    public class WithBuiltIn : CorrelateConfigurationExtensionsTests
    {
        [Fact]
        public void When_configuring_instance_without_configurer_it_should_throw()
        {
            OptionsConfigurer configurer = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            Action act = () => configurer.EnableCorrelate(new LoggerFactory());

            // Assert
            act.Should()
                .Throw<ArgumentNullException>()
                .Where(exception => exception.ParamName == nameof(configurer));
        }

        [Fact]
        public void When_configuring_instance_without_serviceProvider_it_should_throw()
        {
            ILoggerFactory loggerFactory = null;
            Action act = () =>
                Configure.With(new BuiltinHandlerActivator())
                    .Options(opts =>
                        // ReSharper disable once ExpressionIsAlwaysNull
                        opts.EnableCorrelate(loggerFactory)
                    );

            // Assert
            act.Should()
                .Throw<ArgumentNullException>()
                .Where(exception => exception.ParamName == nameof(loggerFactory));
        }
    }

    public class WithDependencyResolverAdapter : CorrelateConfigurationExtensionsTests
    {
        [Fact]
        public void When_configuring_instance_without_configurer_it_should_throw()
        {
            OptionsConfigurer configurer = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            Action act = () => configurer.EnableCorrelate(new DependencyResolverAdapter(_ => null));

            // Assert
            act.Should()
                .Throw<ArgumentNullException>()
                .Where(exception => exception.ParamName == nameof(configurer));
        }

        [Fact]
        public void When_configuring_instance_without_dependencyResolverAdapter_it_should_throw()
        {
            DependencyResolverAdapter dependencyResolverAdapter = null;
            Action act = () =>
                Configure.With(new BuiltinHandlerActivator())
                    .Options(opts =>
                        // ReSharper disable once ExpressionIsAlwaysNull
                        opts.EnableCorrelate(dependencyResolverAdapter)
                    );

            // Assert
            act.Should()
                .Throw<ArgumentNullException>()
                .Where(exception => exception.ParamName == nameof(dependencyResolverAdapter));
        }
    }
}
