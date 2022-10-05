using CoreEx.Json;
using CoreEx.TestFunction.Models;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Nsj = Newtonsoft.Json;

namespace CoreEx.Test.Framework.Json
{
    [TestFixture]
    public class JsonEmployeeTest
    {
        public class Employee
        {
            /// <summary>
            /// Gets or sets the 'EmployeeId' column value.
            /// </summary>
            public Guid EmployeeId { get; set; }

            /// <summary>
            /// Gets or sets the 'Email' column value.
            /// </summary>
            public string? Email { get; set; }

            /// <summary>
            /// Gets or sets the 'FirstName' column value.
            /// </summary>
            public string? FirstName { get; set; }

            /// <summary>
            /// Gets or sets the 'LastName' column value.
            /// </summary>
            public string? LastName { get; set; }

            /// <summary>
            /// Gets or sets the 'GenderCode' column value.
            /// </summary>
            public string? GenderCode { get; set; }

            /// <summary>
            /// Gets or sets the 'Birthday' column value.
            /// </summary>
            public DateTime? Birthday { get; set; }

            /// <summary>
            /// Gets or sets the 'StartDate' column value.
            /// </summary>
            public DateTime? StartDate { get; set; }

            /// <summary>
            /// Gets or sets the 'TerminationDate' column value.
            /// </summary>
            public DateTime? TerminationDate { get; set; }

            /// <summary>
            /// Gets or sets the 'TerminationReasonCode' column value.
            /// </summary>
            public string? TerminationReasonCode { get; set; }

            /// <summary>
            /// Gets or sets the 'PhoneNo' column value.
            /// </summary>
            public string? PhoneNo { get; set; }
        }

        [Test]
        public void SystemTextJson_Serialize_Deserialize()
        {
            // Arrange
            var json = "{\n  \"email\": \"john.doe@avanade.com\",\n  \"FirstName\": \"John\",\n  \"lastName\": \"Doe\",\n  \"genderCode\": \"male\",\n  \"birthday\": \"1990-03-24T13:49:11.813Z\",\n  \"startDate\": \"2022-03-24T13:49:11.813Z\",\n  \"phoneNo\": \"985 657 9455\"\n}";
            var js = new CoreEx.Text.Json.JsonSerializer() as IJsonSerializer;

            // Act
            var employee = js.Deserialize<Employee>(json);

            // Assert
            employee.Should().NotBeNull();
            employee!.FirstName.Should().Be("John");
        }
    }
}