using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Rebus.Correlate
{
	public class DependencyResolverAdapterTests
	{
		private readonly DependencyResolverAdapter _sut;
		private Func<Type, object> _optionalResolve;

		public DependencyResolverAdapterTests()
		{
			_sut = new DependencyResolverAdapter(type => _optionalResolve(type));
		}

		[Fact]
		public void When_creating_instance_without_func_it_should_throw()
		{
			Func<Type, object> optionalResolve = null;
			// ReSharper disable once ExpressionIsAlwaysNull
			// ReSharper disable once ObjectCreationAsStatement
			Action act = () => new DependencyResolverAdapter(optionalResolve);

			// Assert
			act.Should()
				.Throw<ArgumentNullException>()
				.Where(exception => exception.ParamName == nameof(optionalResolve));
		}

		[Fact]
		public void Given_dependency_is_not_registered_when_resolving_optional_should_return_null()
		{
			_optionalResolve = type => null;

			_sut.GetOrNull<object>().Should().BeNull();
		}

		[Fact]
		public void Given_dependency_is_registered_when_resolving_optional_should_return_null()
		{
			var instance = new object();
			_optionalResolve = type => instance;

			Func<object> act = () => _sut.GetOrNull<object>();

			act.Should().NotThrow().Which.Should().Be(instance);
		}

		[Fact]
		public void Given_dependency_is_not_registered_when_resolving_should_return_throw()
		{
			_optionalResolve = type => null;

			Func<object> act = () => _sut.Get<object>();

			act.Should().Throw<InvalidOperationException>();
		}

		[Fact]
		public void Given_dependency_is_registered_when_resolving_should_return_instance()
		{
			var instance = new object();
			_optionalResolve = type => instance;

			_sut.Get<object>().Should().Be(instance);
		}
	}
}
