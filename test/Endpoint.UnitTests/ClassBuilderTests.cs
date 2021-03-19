using Endpoint.Application.Builders;
using Endpoint.Application.Enums;
using Endpoint.Application.Services;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Endpoint.UnitTests
{
    public class ClassBuilderTests
    {


        [Fact]
        public void Constructor()
        {

            var sut = new ClassBuilder("CustomerController", null, null);
        }


        [Fact]
        public void Controller()
        {
            var context = new Context();

            var expected = new List<string> {
                "using System.Net;",
                "using System.Threading.Tasks;",
                "",
                "namespace CustomerService.Api.Controllers",
                "{",
                "    [ApiController]",
                "    [Route(\"api/[controller]\")]",
                "    public class CustomerController",
                "    {",
                "        private readonly IMediator _mediator;",
                "",
                "        public CustomerController(IMediator mediator)",
                "            => _mediator = mediator;",
                "",
                "        [HttpGet(\"{customerId}\", Name = \"GetCustomerByIdRoute\")]",
                "        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]",
                "        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]",
                "        [ProducesResponseType(typeof(GetCustomerById.Response), (int)HttpStatusCode.OK)]",
                "        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]",
                "        public async Task<ActionResult<GetCustomerById.Response>> GetById([FromRoute]GetCustomerById.Request request)",
                "        {",
                "            var response = await _mediator.Send(request);",
                "        ",
                "            if (response.Customer == null)",
                "            {",
                "                return new NotFoundObjectResult(request.CustomerId);",
                "            }",
                "        ",
                "            return response;",
                "        }",
                "        ",
                "    }",
                "}"
            }.ToArray();

            new ClassBuilder("CustomerController", context, Mock.Of<IFileSystem>())
                .WithUsing("System.Net")
                .WithUsing("System.Threading.Tasks")
                .WithNamespace("CustomerService.Api.Controllers")
                .WithAttribute(new AttributeBuilder().WithName("ApiController").Build())
                .WithAttribute(new AttributeBuilder().WithName("Route").WithParam("\"api/[controller]\"").Build())
                .WithDependency("IMediator", "mediator")
                .WithMethod(new MethodBuilder().WithEndpointType(EndpointType.GetById).WithResource("Customer").WithAuthorize(false).Build())
                .Build();

            var actual = context.ElementAt(0).Value;

            for(var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(actual[i], expected[i]);
            }
        }

        [Fact]
        public void Model()
        {
            var context = new Context();

            var expected = new List<string> {
                "using System;",
                "",
                "namespace CustomerService.Api.Models",
                "{",
                "    public class Customer",
                "    {",
                "        public Guid CustomerId { get; set; }",
                "    }",
                "}"
            }.ToArray();

            new ClassBuilder("Customer", context, Mock.Of<IFileSystem>())
                .WithDirectory("")
                .WithUsing("System")
                .WithNamespace("CustomerService.Api.Models")
                .WithProperty(new PropertyBuilder().WithName("CustomerId").WithType("Guid").WithAccessors(new AccessorsBuilder().Build()).Build())
                .Build();

            var actual = context.First().Value;

            for(var i = 0; i <expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void Extensions()
        {
            var context = new Context();

            var expected = new List<string> {
                "using System;",
                "using CustomerService.Domain.Models;",
                "",
                "namespace CustomerService.Application.Features",
                "{",
                "    public static class CustomerExtensions",
                "    {",
                "        public static CustomerDto ToDto(this Customer customer)",
                "        {",
                "            return new CustomerDto",
                "            {",
                "                CustomerId = customer.CustomerId",
                "            };",
                "        }",
                "    }",
                "}"
            }.ToArray();

            new ClassBuilder("CustomerExtensions", context, Mock.Of<IFileSystem>())
                .WithDirectory("")
                .WithUsing("System")
                .WithUsing("CustomerService.Domain.Models")
                .WithNamespace("CustomerService.Application.Features")
                .WithMethod(new MethodBuilder()
                .IsStatic()
                .WithName("ToDto")
                .WithReturnType("CustomerDto")
                .WithPropertyName("CustomerId")
                .WithParameter(new ParameterBuilder("Customer", "customer", true).Build())
                .WithBody(new()
                {
                    "    return new ()",
                    "    {",
                    "        CustomerId = customer.CustomerId",
                    "    };"
                })
                .Build())
                .Build();

            var actual = context.First().Value;

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void DbContext()
        {
            var context = new Context();

            var expected = new List<string> {
                "using CustomerService.Api.Models;",
                "using Microsoft.EntityFrameworkCore;",
                "",
                "namespace CustomerService.Api.Data",
                "{",
                "    public class CustomerServiceDbContext: DbContext, ICustomerServiceDbContext",
                "    {",
                "        public DbSet<Customer> Customers { get; private set; }",
                "        public CustomerServiceDbContext(DbContextOptions options)",
                "            :base(options) { }",
                "    }",
                "}"
            }.ToArray();

            new ClassBuilder("CustomerServiceDbContext", context, Mock.Of<IFileSystem>())
                .WithDirectory("")
                .WithUsing("CustomerService.Api.Models")
                .WithUsing("Microsoft.EntityFrameworkCore")
                .WithNamespace("CustomerService.Api.Data")
                .WithInterface("ICustomerServiceDbContext")
                .WithBase("DbContext")
                .WithBaseDependency("DbContextOptions","options")
                .WithProperty(new PropertyBuilder().WithName("Customers").WithType(new TypeBuilder().WithGenericType("DbSet","Customer").Build()).WithAccessors(new AccessorsBuilder().WithSetAccessModifuer("private").Build()).Build())
                .Build();

            var actual = context.First().Value;

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void Interface()
        {
            var context = new Context();

            var expected = new List<string> {
                "using CustomerService.Api.Models;",
                "using Microsoft.EntityFrameworkCore;",
                "",
                "namespace CustomerService.Application.Interfaces",
                "{",
                "    public interface ICustomerServiceDbContext",
                "    {",
                "        DbSet<Customer> Customers { get; }",
                "        Task<int> SaveChangesAsync(CancellationToken cancellationToken);",
                "    }",
                "}"
            }.ToArray();

            var sut = new ClassBuilder("CustomerServiceDbContext", context, Mock.Of<IFileSystem>(), "interface")
                .WithUsing("CustomerService.Api.Models")
                .WithUsing("Microsoft.EntityFrameworkCore")
                .WithNamespace("CustomerService.Application.Interfaces")
                .WithProperty(new PropertyBuilder().WithName("Customers").WithAccessModifier(AccessModifier.Inherited).WithType(new TypeBuilder().WithGenericType("DbSet","Customer").Build()).WithAccessors(new AccessorsBuilder().WithGetterOnly().Build()).Build())
                .WithMethodSignature(new MethodSignatureBuilder()
                .WithAsync(false)
                .WithAccessModifier(AccessModifier.Inherited)
                .WithName("SaveChangesAsync")
                .WithReturnType(new TypeBuilder().WithGenericType("Task","int").Build())
                .WithParameter(new ParameterBuilder("CancellationToken", "cancellationToken").Build()).Build());

            sut.Build();

            var actual = context.First().Value;

            Assert.Equal(expected, actual);
        }


        [Fact]
        public void InnerRequest()
        {
            var context = new Context();

            var expected = new List<string> {
                "namespace CustomerService.Application.Features",
                "{",
                "    public class Request: IRequest<Response> { }",
                "}"
            }.ToArray();

            new ClassBuilder("Request", context, Mock.Of<IFileSystem>())
                .WithDirectory("")
                .WithInterface(new TypeBuilder().WithGenericType("IRequest","Response").Build())
                .WithNamespace("CustomerService.Application.Features")
                .Build();

            var actual = context.First().Value;

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void Response()
        {
            var context = new Context();

            var expected = new List<string> {
                "namespace CustomerService.Application.Features",
                "{",
                "    public class Response: ResponseBase",
                "    {",
                "        public CustomerDto Customer { get; set; }",
                "    }",
                "}"
            }.ToArray();

            new ClassBuilder("Response", context, Mock.Of<IFileSystem>())
                .WithDirectory("")
                .WithBase("ResponseBase")
                .WithNamespace("CustomerService.Application.Features")
                .WithProperty(new PropertyBuilder().WithType("CustomerDto").WithName("Customer").WithAccessors(new AccessorsBuilder().Build()).Build())
                .Build();

            var actual = context.First().Value;

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void Validator()
        {
            var context = new Context();

            var expected = new List<string> {
                "namespace CustomerService.Application.Features",
                "{",
                "    public class CustomerValidator: AbstractValidator<CustomerDto> { }",
                "}"
            }.ToArray();

            new ClassBuilder("CustomerValidator", context, Mock.Of<IFileSystem>())
                .WithDirectory("")
                .WithBase(new TypeBuilder().WithGenericType("AbstractValidator","CustomerDto").Build())
                .WithNamespace("CustomerService.Application.Features")
                .Build();

            var actual = context.First().Value;

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void SubClasses()
        {
            var context = new Context();

            var expected = new List<string> {
                "namespace CustomerService.Application.Features",
                "{",
                "    public class Query",
                "    {",
                "        public class Request: IRequest<Response> { }",
                "    ",
                "        public class Response { }",
                "    ",
                "        public class Handler: IHandler<Request, Response> { }",
                "    }",
                "}"
            }.ToArray();

            new ClassBuilder("Response", context, Mock.Of<IFileSystem>())
                .WithDirectory("")
                .WithBase("ResponseBase")
                .WithNamespace("CustomerService.Application.Features")
                .WithProperty(new PropertyBuilder().WithType("CustomerDto").WithName("Customer").WithAccessors(new AccessorsBuilder().Build()).Build())
                .Build();

            var actual = context.First().Value;

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }
    }
}
